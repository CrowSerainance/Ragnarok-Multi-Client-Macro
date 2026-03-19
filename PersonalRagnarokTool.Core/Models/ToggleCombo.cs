using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class ToggleCombo : ObservableObject
{
    private string _modifier = "Alt";
    private string _key = "Oem5";

    public string Modifier
    {
        get => _modifier;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) trimmed = "Alt";
            SetProperty(ref _modifier, trimmed);
        }
    }

    public string Key
    {
        get => _key;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) trimmed = "Oem5";
            SetProperty(ref _key, trimmed);
        }
    }

    public string DisplayText => $"{Modifier} + {Key}";
}
