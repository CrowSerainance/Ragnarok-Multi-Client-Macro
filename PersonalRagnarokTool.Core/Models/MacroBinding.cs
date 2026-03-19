using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class MacroBinding : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _clientProfileId = string.Empty;
    private string _name = "Binding";
    private bool _isEnabled = true;
    private string _triggerKey = "F1";
    private int _intervalMs = 50;

    public string Id
    {
        get => _id;
        set
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(v)) return;
            SetProperty(ref _id, v);
        }
    }

    public string ClientProfileId
    {
        get => _clientProfileId;
        set => SetProperty(ref _clientProfileId, (value ?? string.Empty).Trim());
    }

    public string Name
    {
        get => _name;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) trimmed = "Binding";
            SetProperty(ref _name, trimmed);
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>The key on your keyboard that triggers this macro.</summary>
    public string TriggerKey
    {
        get => _triggerKey;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) trimmed = "F1";
            SetProperty(ref _triggerKey, trimmed);
        }
    }

    /// <summary>Repeat interval (ms) — delay between full sequence cycles. Min 25.</summary>
    public int IntervalMs
    {
        get => _intervalMs;
        set => SetProperty(ref _intervalMs, Math.Max(25, value));
    }

    /// <summary>The macro step sequence to execute when triggered.</summary>
    public ObservableCollection<MacroStep> Steps { get; init; } = new();
}
