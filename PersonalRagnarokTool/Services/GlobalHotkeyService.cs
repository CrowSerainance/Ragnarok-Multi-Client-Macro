using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

public sealed class GlobalHotkeyService
{
    private readonly Dictionary<int, string> _hotkeyById = new();
    private IntPtr _windowHandle = IntPtr.Zero;
    private int _nextId = 10_000;

    public event EventHandler<string>? HotkeyPressed;

    public bool IsReady => _windowHandle != IntPtr.Zero;

    public void SetWindowHandle(IntPtr windowHandle)
    {
        UnregisterAll();
        _windowHandle = windowHandle;
    }

    public IReadOnlyList<string> RegisterBindings(IEnumerable<MacroBinding> bindings)
    {
        UnregisterAll();
        var errors = new List<string>();
        var seenHotkeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_windowHandle == IntPtr.Zero)
        {
            errors.Add("Window handle is not ready for hotkey registration.");
            return errors;
        }

        foreach (var binding in bindings.Where(x => x.IsEnabled))
        {
            string normalizedHotkey = HotkeyText.Normalize(binding.TriggerHotkey);
            if (!VirtualKeyMap.TryGetVirtualKey(normalizedHotkey, out int vk))
            {
                errors.Add($"Unsupported hotkey '{binding.TriggerHotkey}' for {binding.Name}.");
                continue;
            }

            if (!seenHotkeys.Add(normalizedHotkey))
            {
                errors.Add($"Duplicate hotkey '{binding.TriggerHotkey}' for {binding.Name}.");
                continue;
            }

            int id = _nextId++;
            if (!NativeMethods.RegisterHotKey(_windowHandle, id, NativeMethods.MOD_NOREPEAT, (uint)vk))
            {
                errors.Add($"OS rejected hotkey '{binding.TriggerHotkey}' for {binding.Name}.");
                continue;
            }

            _hotkeyById[id] = normalizedHotkey;
        }

        return errors;
    }

    public void UnregisterAll()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            foreach (var id in _hotkeyById.Keys.ToArray())
            {
                _ = NativeMethods.UnregisterHotKey(_windowHandle, id);
            }
        }

        _hotkeyById.Clear();
    }

    public void HandleWindowMessage(IntPtr wParam)
    {
        int id = wParam.ToInt32();
        if (_hotkeyById.TryGetValue(id, out string? hotkey))
        {
            HotkeyPressed?.Invoke(this, hotkey);
        }
    }
}
