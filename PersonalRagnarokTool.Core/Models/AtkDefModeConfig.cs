using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class AtkDefKeyEntry : ObservableObject
{
    private string _slotName = "Slot";
    private string _key = "None";

    public string SlotName
    {
        get => _slotName;
        set => SetProperty(ref _slotName, value);
    }

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }
}

public sealed class AtkDefModeConfig : ObservableObject
{
    private bool _enabled;
    private string _spammerKey = "None";
    private bool _spammerWithClick;
    private int _spammerDelay = 10;
    private int _switchDelay = 50;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public string SpammerKey
    {
        get => _spammerKey;
        set => SetProperty(ref _spammerKey, value);
    }

    public bool SpammerWithClick
    {
        get => _spammerWithClick;
        set => SetProperty(ref _spammerWithClick, value);
    }

    public int SpammerDelay
    {
        get => _spammerDelay;
        set => SetProperty(ref _spammerDelay, Math.Max(1, value));
    }

    public int SwitchDelay
    {
        get => _switchDelay;
        set => SetProperty(ref _switchDelay, Math.Max(1, value));
    }

    public ObservableCollection<AtkDefKeyEntry> AtkKeys { get; set; } = new();
    public ObservableCollection<AtkDefKeyEntry> DefKeys { get; set; } = new();
}
