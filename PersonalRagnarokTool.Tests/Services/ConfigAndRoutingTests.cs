using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Tests.Services;

public sealed class ConfigAndRoutingTests
{
    [Fact]
    public void AppConfigStore_RoundTripsProfilesBindingsAndPolygon()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"prt-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppConfigStore();
            var config = new AppConfig();
            var profile = new ClientProfile { DisplayName = "Client 1" };
            profile.ActionPolygon.IsClosed = true;
            profile.ActionPolygon.Vertices.Add(new NormalizedPoint(0.1, 0.2));
            profile.ActionPolygon.Vertices.Add(new NormalizedPoint(0.9, 0.2));
            profile.ActionPolygon.Vertices.Add(new NormalizedPoint(0.6, 0.8));
            profile.TraceSequences.Add(new TraceSequence { Name = "Trace A" });
            profile.Bindings.Add(new MacroBinding
            {
                Name = "Primary",
                TriggerHotkey = "f1",
                InputKey = "1",
                ExecutionMode = ExecutionMode.RandomPolygon,
            });
            config.ClientProfiles.Add(profile);

            store.Save(tempPath, config);
            AppConfig loaded = store.Load(tempPath);

            Assert.Single(loaded.ClientProfiles);
            Assert.Equal("Client 1", loaded.ClientProfiles[0].DisplayName);
            Assert.True(loaded.ClientProfiles[0].ActionPolygon.IsClosed);
            Assert.Equal(3, loaded.ClientProfiles[0].ActionPolygon.Vertices.Count);
            Assert.Single(loaded.ClientProfiles[0].Bindings);
            Assert.Equal("F1", loaded.ClientProfiles[0].Bindings[0].TriggerHotkey);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void BindingValidator_FindsDuplicateEnabledHotkeys()
    {
        AppConfig config = CreateConfig();
        config.ClientProfiles[0].Bindings.Add(new MacroBinding { Name = "A", TriggerHotkey = "F1", InputKey = "1" });
        config.ClientProfiles[1].Bindings.Add(new MacroBinding { Name = "B", TriggerHotkey = "f1", InputKey = "2" });
        config.ClientProfiles[1].Bindings.Add(new MacroBinding { Name = "Disabled", TriggerHotkey = "F1", InputKey = "3", IsEnabled = false });

        IReadOnlyList<string> duplicates = BindingValidator.GetDuplicateHotkeys(config);

        Assert.Single(duplicates);
        Assert.Equal("F1", duplicates[0]);
    }

    [Fact]
    public void HotkeyRouter_ReturnsTheMatchingEnabledBinding()
    {
        AppConfig config = CreateConfig();
        var binding = new MacroBinding
        {
            Name = "Heal",
            TriggerHotkey = "F2",
            InputKey = "2",
        };
        config.ClientProfiles[0].Bindings.Add(binding);
        BindingValidator.NormalizeConfig(config);

        var router = new HotkeyRouter();
        RoutedBinding? routed = router.FindBinding(config, "f2");

        Assert.NotNull(routed);
        Assert.Equal(binding.Id, routed!.Binding.Id);
        Assert.Equal(config.ClientProfiles[0].Id, routed.Profile.Id);
    }

    [Fact]
    public void ClickExecutionPlanner_UsesTraceCountBeforeDefaultingToOne()
    {
        var binding = new MacroBinding { Name = "Random", TriggerHotkey = "F3", InputKey = "3" };
        var trace = new TraceSequence { Name = "Trace" };
        trace.Points.Add(new NormalizedPoint(0.1, 0.1));
        trace.Points.Add(new NormalizedPoint(0.2, 0.2));

        int count = ClickExecutionPlanner.ResolveClickCount(binding, trace);

        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData("PgUp", "PgUp")]
    [InlineData("pgdown", "PgDown")]
    [InlineData("OemPlus", "OemPlus")]
    [InlineData("+", "OemPlus")]
    [InlineData("minus", "OemMinus")]
    [InlineData("F12", "F12")]
    public void HotkeyText_NormalizesExtendedKeyNames(string input, string expected)
    {
        string normalized = HotkeyText.Normalize(input);

        Assert.Equal(expected, normalized);
    }

    private static AppConfig CreateConfig()
    {
        var config = new AppConfig();
        config.ClientProfiles.Add(new ClientProfile { DisplayName = "Client 1" });
        config.ClientProfiles.Add(new ClientProfile { DisplayName = "Client 2" });
        return config;
    }
}
