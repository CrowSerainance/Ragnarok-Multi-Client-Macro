using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Tests.Services;

public sealed class ConfigAndRoutingTests
{
    [Fact]
    public void AppConfigStore_RoundTripsProfilesAndBindings()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"prt-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppConfigStore();
            var config = new AppConfig();
            var profile = new ClientProfile { DisplayName = "Client 1" };
            profile.Bindings.Add(new MacroBinding
            {
                Name = "Primary",
                TriggerKey = "f1",
                IntervalMs = 80,
                Steps =
                {
                    new MacroStep { Key = "f2", DelayMs = 90 },
                    new MacroStep { Key = "q", DelayMs = 140 },
                },
            });
            config.ClientProfiles.Add(profile);

            store.Save(tempPath, config);
            AppConfig loaded = store.Load(tempPath);

            Assert.Single(loaded.ClientProfiles);
            Assert.Equal("Client 1", loaded.ClientProfiles[0].DisplayName);
            Assert.Single(loaded.ClientProfiles[0].Bindings);
            Assert.Equal("f1", loaded.ClientProfiles[0].Bindings[0].TriggerKey);
            Assert.Equal(80, loaded.ClientProfiles[0].Bindings[0].IntervalMs);
            Assert.Collection(
                loaded.ClientProfiles[0].Bindings[0].Steps,
                step =>
                {
                    Assert.Equal("f2", step.Key);
                    Assert.Equal(90, step.DelayMs);
                },
                step =>
                {
                    Assert.Equal("q", step.Key);
                    Assert.Equal(140, step.DelayMs);
                });
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
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

    [Theory]
    [InlineData("F1", 0x70)]
    [InlineData("spacebar", 0x20)]
    [InlineData("num1", 0x61)]
    [InlineData("+", 0xBB)]
    [InlineData("None", 0x00)]
    public void VirtualKeyMap_MapsExpectedKeys(string input, ushort expected)
    {
        ushort vk = VirtualKeyMap.GetVk(input);

        Assert.Equal(expected, vk);
    }

    [Fact]
    public void AgentPipeClient_BuildAutopotPayload_UsesBinaryLayoutExpectedByNativeAgent()
    {
        var autopot = new AutopotConfig
        {
            Enabled = true,
            HpKey = "F1",
            HpThreshold = 60,
            SpKey = "F2",
            SpThreshold = 35,
            DelayMs = 75,
        };
        var ygg = new YggAutopotConfig
        {
            Enabled = true,
            HpKey = "F3",
            SpKey = "F3",
            HpThreshold = 20,
            SpThreshold = 25,
        };

        byte[] payload = AgentPipeClient.BuildAutopotPayload(autopot, ygg);

        Assert.Equal(23, payload.Length);
        Assert.Equal(1, payload[0]);
        Assert.Equal((ushort)0x70, BitConverter.ToUInt16(payload, 1));
        Assert.Equal(60, BitConverter.ToInt32(payload, 3));
        Assert.Equal((ushort)0x71, BitConverter.ToUInt16(payload, 7));
        Assert.Equal(35, BitConverter.ToInt32(payload, 9));
        Assert.Equal((ushort)0x72, BitConverter.ToUInt16(payload, 13));
        Assert.Equal(20, BitConverter.ToInt32(payload, 15));
        Assert.Equal(75, BitConverter.ToInt32(payload, 19));
    }
}
