using PersonalRagnarokTool.Core.Geometry;
using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

public sealed class MacroExecutor
{
    private readonly ClientBindingService _bindingService;
    private readonly BackgroundInputDispatcher _inputDispatcher;
    private readonly HotkeyRouter _hotkeyRouter;
    private readonly SemaphoreSlim _executionGate = new(1, 1);

    public MacroExecutor(ClientBindingService bindingService, BackgroundInputDispatcher inputDispatcher, HotkeyRouter hotkeyRouter)
    {
        _bindingService = bindingService;
        _inputDispatcher = inputDispatcher;
        _hotkeyRouter = hotkeyRouter;
    }

    public async Task<string> ExecuteHotkeyAsync(AppConfig config, string hotkey, CancellationToken cancellationToken = default)
    {
        var routedBinding = _hotkeyRouter.FindBinding(config, hotkey);
        if (routedBinding is null)
        {
            return $"No binding found for '{hotkey}'.";
        }

        return await ExecuteBindingAsync(routedBinding.Profile, routedBinding.Binding, config, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ExecuteBindingAsync(ClientProfile profile, MacroBinding binding, AppConfig config, CancellationToken cancellationToken = default)
    {
        await _executionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!profile.IsEnabled || !binding.IsEnabled)
            {
                return $"{profile.DisplayName}: binding '{binding.Name}' is disabled.";
            }

            var liveWindow = _bindingService.ResolveLiveWindow(profile);
            if (liveWindow is null)
            {
                return $"{profile.DisplayName}: no live client window found. Re-bind the client profile.";
            }

            string inputKeyToSend = string.IsNullOrWhiteSpace(binding.InputKey)
                ? binding.TriggerHotkey
                : binding.InputKey;

            if (!_inputDispatcher.SendInputKey(liveWindow, inputKeyToSend, config, out string inputStatus))
            {
                return $"{profile.DisplayName}: key send failed - {inputStatus}";
            }

            if (binding.PostInputDelayMs > 0)
            {
                await Task.Delay(binding.PostInputDelayMs, cancellationToken).ConfigureAwait(false);
            }

            var trace = profile.TraceSequences.FirstOrDefault(x => x.Id == binding.TraceSequenceId);
            var points = trace?.Points.Select(point => point.Clone()).ToArray() ?? Array.Empty<NormalizedPoint>();
            if (points.Length == 0)
            {
                return $"{profile.DisplayName}: {binding.Name} - key sent ({HotkeyText.Normalize(inputKeyToSend)}), no click executed because the selected sequence has no points.";
            }

            for (int i = 0; i < points.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pixel = CoordinateTranslator.ToPixel(points[i], liveWindow.ClientWidth, liveWindow.ClientHeight);
                _inputDispatcher.SendClick(liveWindow, pixel.X, pixel.Y, config);

                if (i < points.Length - 1 && binding.InterClickDelayMs > 0)
                {
                    await Task.Delay(binding.InterClickDelayMs, cancellationToken).ConfigureAwait(false);
                }
            }

            return $"{profile.DisplayName}: {binding.Name} executed ({points.Length} click{(points.Length == 1 ? string.Empty : "s")}).";
        }
        finally
        {
            _executionGate.Release();
        }
    }
}
