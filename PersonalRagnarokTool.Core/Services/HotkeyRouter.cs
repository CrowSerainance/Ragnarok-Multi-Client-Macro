using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public sealed class HotkeyRouter
{
    public RoutedBinding? FindBinding(AppConfig config, string triggerHotkey)
    {
        var normalized = HotkeyText.Normalize(triggerHotkey);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        foreach (var profile in config.ClientProfiles.Where(p => p.IsEnabled))
        {
            foreach (var binding in profile.Bindings.Where(b => b.IsEnabled))
            {
                if (string.Equals(HotkeyText.Normalize(binding.TriggerHotkey), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return new RoutedBinding(profile, binding);
                }
            }
        }

        return null;
    }
}
