using System.Collections.Concurrent;

namespace PersonalRagnarokTool.Services;

public sealed class ToggleService
{
    private bool _globalActive = true;
    private readonly ConcurrentDictionary<string, bool> _clientOverrides = new();

    public bool IsGlobalActive => _globalActive;

    public bool ToggleGlobal()
    {
        _globalActive = !_globalActive;
        GlobalToggled?.Invoke(this, _globalActive);
        return _globalActive;
    }

    public bool ToggleClient(string profileId)
    {
        bool current = IsClientActive(profileId);
        bool newState = !current;
        _clientOverrides[profileId] = newState;
        ClientToggled?.Invoke(this, (profileId, newState));
        return newState;
    }

    public bool IsClientActive(string profileId)
    {
        return _clientOverrides.TryGetValue(profileId, out bool over) ? over : _globalActive;
    }

    public void ResetClient(string profileId) => _clientOverrides.TryRemove(profileId, out _);

    public event EventHandler<bool>? GlobalToggled;
    public event EventHandler<(string ProfileId, bool IsActive)>? ClientToggled;
}
