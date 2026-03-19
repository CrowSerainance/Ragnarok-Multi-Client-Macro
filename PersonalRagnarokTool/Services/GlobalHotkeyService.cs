using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

/// <summary>
/// Detects hotkeys via GetAsyncKeyState polling (like YXExt.dll) or Interception kernel driver.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int PollIntervalMs = 10; // ~100 Hz, matches YXExt

    // ── Monitored key sets (swapped atomically) ──────────────────
    private volatile KeyMap _keyMap = KeyMap.Empty;

    // ── Edge-detection state (owned by poll thread only) ─────────
    private readonly HashSet<int> _simpleKeysDown = new();
    private readonly HashSet<string> _comboTagsDown = new();

    // ── Lifecycle ────────────────────────────────────────────────
    private CancellationTokenSource? _cts;
    private Thread? _pollThread;
    
    private IntPtr _interceptionContext = IntPtr.Zero;
    private bool _useInterception = false;

    public event EventHandler<string>? HotkeyPressed;

    public bool IsReady => _pollThread?.IsAlive == true;

    public GlobalHotkeyService()
    {
        try
        {
            _interceptionContext = InterceptionInterop.interception_create_context();
            if (_interceptionContext != IntPtr.Zero)
            {
                _useInterception = true;
            }
        }
        catch
        {
            // Interception DLL or driver not found
            _useInterception = false;
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Start / Stop
    // ═════════════════════════════════════════════════════════════

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        
        ThreadStart loopMethod = _useInterception ? () => InterceptionPollLoop(_cts.Token) : () => PollLoop(_cts.Token);

        _pollThread = new Thread(loopMethod)
        {
            Name = "HotkeyPoll",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        _pollThread.Start();
    }

    public void Stop()
    {
        _cts?.Cancel();
        _pollThread?.Join(1000);
        _cts?.Dispose();
        _cts = null;
        _pollThread = null;
    }

    // ═════════════════════════════════════════════════════════════
    //  Update monitored keys (called from UI thread)
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Rebuilds the full set of monitored keys + combos.
    /// Thread-safe: the poll loop picks up the new map on its next cycle.
    /// </summary>
    public IReadOnlyList<string> RefreshMonitoredKeys(
        IEnumerable<MacroBinding> bindings,
        ToggleCombo globalToggle,
        IEnumerable<(ToggleCombo combo, string tag)> clientToggles)
    {
        var errors = new List<string>();
        var simpleKeys = new Dictionary<int, string>();
        var combos = new List<ComboEntry>();
        var seenVks = new HashSet<int>();

        // ── Global toggle combo ──────────────────────────────────
        if (!TryBuildCombo(globalToggle, "TOGGLE:global", combos, errors))
            errors.Add($"Unsupported global toggle key '{globalToggle.Key}'.");

        // ── Per-client toggle combos ─────────────────────────────
        foreach (var (combo, tag) in clientToggles)
        {
            if (!TryBuildCombo(combo, tag, combos, errors))
                errors.Add($"Unsupported client toggle key '{combo.Key}'.");
        }

        // ── Binding trigger keys (simple, no modifier) ───────────
        foreach (var binding in bindings.Where(b => b.IsEnabled))
        {
            string normalized = HotkeyText.Normalize(binding.TriggerKey);
            if (!VirtualKeyMap.TryGetVirtualKey(normalized, out int vk))
            {
                errors.Add($"Unsupported hotkey '{binding.TriggerKey}' for {binding.Name}.");
                continue;
            }

            if (!seenVks.Add(vk))
            {
                errors.Add($"Duplicate hotkey '{binding.TriggerKey}' for {binding.Name}.");
                continue;
            }

            simpleKeys[vk] = normalized;
        }

        // Swap atomically
        _keyMap = new KeyMap(simpleKeys, combos);

        // Reset edge-detection so stale state doesn't carry over
        _simpleKeysDown.Clear();
        _comboTagsDown.Clear();

        return errors;
    }

    // ═════════════════════════════════════════════════════════════
    //  Interception loop (runs on dedicated background thread)
    // ═════════════════════════════════════════════════════════════

    private void InterceptionPollLoop(CancellationToken ct)
    {
        InterceptionInterop.interception_set_filter(
            _interceptionContext,
            InterceptionInterop.interception_is_keyboard,
            InterceptionInterop.INTERCEPTION_FILTER_KEY_ALL);

        var stroke = new InterceptionInterop.Stroke();
        
        // Maintain a logical key state dictionary for modifiers based on scancodes
        var keyStates = new Dictionary<int, bool>();

        while (!ct.IsCancellationRequested)
        {
            int device = InterceptionInterop.interception_wait_with_timeout(_interceptionContext, 50);
            if (device > 0)
            {
                if (InterceptionInterop.interception_receive(_interceptionContext, device, ref stroke, 1) > 0)
                {
                    bool isDown = (stroke.Key.State & (ushort)InterceptionInterop.KeyState.Up) == 0;
                    
                    // Map scancode back to VK for our logic
                    uint vk = NativeMethods.MapVirtualKeyW(stroke.Key.Code, 1); // 1 = MAPVK_VSC_TO_VK
                    if (vk != 0)
                    {
                        keyStates[(int)vk] = isDown;
                        ProcessHotkeys((int)vk, isDown, keyStates);
                    }

                    // Forward stroke immediately to OS
                    InterceptionInterop.interception_send(_interceptionContext, device, ref stroke, 1);
                }
            }
        }
    }

    private void ProcessHotkeys(int changedVk, bool isDown, Dictionary<int, bool> keyStates)
    {
        KeyMap map = _keyMap;

        // Check toggle combos (modifier + key)
        foreach (var combo in map.Combos)
        {
            if (combo.KeyVk == changedVk && isDown)
            {
                bool allModsHeld = true;
                foreach (int modVk in combo.ModifierVks)
                {
                    if (!keyStates.TryGetValue(modVk, out bool modDown) || !modDown)
                    {
                        allModsHeld = false;
                        break;
                    }
                }

                if (allModsHeld)
                {
                    if (_comboTagsDown.Add(combo.Tag))
                        HotkeyPressed?.Invoke(this, combo.Tag);
                }
            }
            else if (combo.KeyVk == changedVk && !isDown)
            {
                _comboTagsDown.Remove(combo.Tag);
            }
        }

        // Check simple trigger keys
        if (map.SimpleKeys.TryGetValue(changedVk, out string? tag))
        {
            if (isDown)
            {
                if (_simpleKeysDown.Add(changedVk))
                    HotkeyPressed?.Invoke(this, tag);
            }
            else
            {
                _simpleKeysDown.Remove(changedVk);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Poll loop (runs on dedicated background thread)
    // ═════════════════════════════════════════════════════════════

    private void PollLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            KeyMap map = _keyMap; // snapshot

            // ── Check toggle combos (modifier + key) ─────────────
            foreach (var combo in map.Combos)
            {
                bool allModsHeld = true;
                foreach (int modVk in combo.ModifierVks)
                {
                    if ((NativeMethods.GetAsyncKeyState(modVk) & 0x8000) == 0)
                    {
                        allModsHeld = false;
                        break;
                    }
                }

                bool keyHeld = (NativeMethods.GetAsyncKeyState(combo.KeyVk) & 0x8000) != 0;

                if (allModsHeld && keyHeld)
                {
                    // Rising edge: fire only once per press
                    if (_comboTagsDown.Add(combo.Tag))
                        HotkeyPressed?.Invoke(this, combo.Tag);
                }
                else
                {
                    _comboTagsDown.Remove(combo.Tag);
                }
            }

            // ── Check simple trigger keys ────────────────────────
            foreach (var (vk, tag) in map.SimpleKeys)
            {
                bool isDown = (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

                if (isDown)
                {
                    if (_simpleKeysDown.Add(vk))
                        HotkeyPressed?.Invoke(this, tag);
                }
                else
                {
                    _simpleKeysDown.Remove(vk);
                }
            }

            Thread.Sleep(PollIntervalMs);
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════

    private static bool TryBuildCombo(
        ToggleCombo toggle, string tag,
        List<ComboEntry> combos, List<string> errors)
    {
        if (!VirtualKeyMap.TryGetVirtualKey(toggle.Key, out int keyVk))
            return false;

        var modVks = new List<int>();
        string mod = toggle.Modifier;
        if (mod.Contains("Alt")) modVks.Add(0xA4);     // VK_LMENU
        if (mod.Contains("Ctrl")) modVks.Add(0xA2);    // VK_LCONTROL
        if (mod.Contains("Shift")) modVks.Add(0xA0);   // VK_LSHIFT

        combos.Add(new ComboEntry(modVks.ToArray(), keyVk, tag));
        return true;
    }

    public void Dispose()
    {
        Stop();
        if (_interceptionContext != IntPtr.Zero)
        {
            InterceptionInterop.interception_destroy_context(_interceptionContext);
            _interceptionContext = IntPtr.Zero;
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Immutable snapshots for thread safety
    // ═════════════════════════════════════════════════════════════

    private sealed record ComboEntry(int[] ModifierVks, int KeyVk, string Tag);

    private sealed class KeyMap
    {
        public static readonly KeyMap Empty = new(
            new Dictionary<int, string>(), new List<ComboEntry>());

        public IReadOnlyDictionary<int, string> SimpleKeys { get; }
        public IReadOnlyList<ComboEntry> Combos { get; }

        public KeyMap(
            IReadOnlyDictionary<int, string> simpleKeys,
            IReadOnlyList<ComboEntry> combos)
        {
            SimpleKeys = simpleKeys;
            Combos = combos;
        }
    }
}
