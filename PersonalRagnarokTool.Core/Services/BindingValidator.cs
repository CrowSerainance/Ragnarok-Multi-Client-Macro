using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public static class BindingValidator
{
    public static IReadOnlyList<string> GetDuplicateHotkeys(AppConfig config)
    {
        var keys = config.ClientProfiles
            .Where(profile => profile.IsEnabled)
            .SelectMany(profile => profile.Bindings)
            .Where(binding => binding.IsEnabled)
            .Select(binding => HotkeyText.Normalize(binding.TriggerHotkey))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return keys;
    }

    public static bool HasDuplicateHotkeys(AppConfig config) => GetDuplicateHotkeys(config).Count > 0;

    public static void NormalizeConfig(AppConfig config)
    {
        foreach (var profile in config.ClientProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                profile.Id = Guid.NewGuid().ToString("N");
            }

            foreach (var binding in profile.Bindings)
            {
                if (string.IsNullOrWhiteSpace(binding.Id))
                {
                    binding.Id = Guid.NewGuid().ToString("N");
                }

                binding.ClientProfileId = profile.Id;
                binding.TriggerHotkey = HotkeyText.Normalize(binding.TriggerHotkey);
                binding.InputKey = HotkeyText.Normalize(binding.InputKey);
                binding.ExecutionMode = ExecutionMode.TraceSequence;
                if (binding.TraceSequenceId is null && profile.TraceSequences.Count > 0)
                {
                    binding.TraceSequenceId = profile.TraceSequences[0].Id;
                }
            }

            foreach (var trace in profile.TraceSequences)
            {
                if (string.IsNullOrWhiteSpace(trace.Id))
                {
                    trace.Id = Guid.NewGuid().ToString("N");
                }
            }
        }
    }
}
