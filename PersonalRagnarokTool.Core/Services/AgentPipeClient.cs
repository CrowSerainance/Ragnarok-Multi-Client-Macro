using System.IO.Pipes;
using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public sealed class AgentPipeClient
{
    private const uint CMD_SYNC_AUTOPOT = 0x10;
    private const uint CMD_SYNC_AUTOBUFF = 0x11;
    private const uint CMD_SYNC_SPAMMER = 0x12;
    private const uint CMD_SYNC_RECOVERY = 0x13;
    private const uint CMD_SYNC_YGG_AUTOPOT = 0x14;
    private const uint CMD_SYNC_SKILL_TIMER = 0x15;
    private const uint CMD_SYNC_ATK_DEF = 0x16;
    private const uint CMD_SYNC_MACRO_SONG = 0x17;
    private const uint CMD_SYNC_MACRO_SWITCH = 0x18;
    private const uint CMD_SYNC_AUTOBUFF_SKILLS = 0x19;
    private const uint CMD_SYNC_AUTOBUFF_ITEMS = 0x1A;
    private const uint CMD_SYNC_DEBUFF_RECOVERY = 0x1B;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Header
    {
        public uint CommandId;
        public uint PayloadLength;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SyncAutopotPayload
    {
        public bool Enabled;
        public ushort HpKey;
        public int HpThreshold;
        public ushort SpKey;
        public int SpThreshold;
        public int DelayMs;
    }

    public static void SyncAutopot(int pid, AutopotConfig config)
    {
        var payload = new SyncAutopotPayload
        {
            Enabled = config.Enabled,
            HpKey = VirtualKeyMap.GetVk(config.HpKey),
            HpThreshold = config.HpThreshold,
            SpKey = VirtualKeyMap.GetVk(config.SpKey),
            SpThreshold = config.SpThreshold,
            DelayMs = config.DelayMs
        };
        Send(pid, CMD_SYNC_AUTOPOT, payload);
    }

    public static void SyncYggAutopot(int pid, YggAutopotConfig config)
    {
        var payload = new SyncAutopotPayload
        {
            Enabled = config.Enabled,
            HpKey = VirtualKeyMap.GetVk(config.HpKey),
            HpThreshold = config.HpThreshold,
            SpKey = VirtualKeyMap.GetVk(config.SpKey),
            SpThreshold = config.SpThreshold,
            DelayMs = config.DelayMs
        };
        Send(pid, CMD_SYNC_YGG_AUTOPOT, payload);
    }

    public static void SyncAutobuff(int pid, AutobuffConfig config)
    {
        SendBuffList(pid, CMD_SYNC_AUTOBUFF, config.Enabled, config.Buffs);
    }

    public static void SyncAutobuffSkills(int pid, AutobuffSkillsConfig config)
    {
        SendBuffList(pid, CMD_SYNC_AUTOBUFF_SKILLS, config.Enabled, config.Buffs);
    }

    public static void SyncAutobuffItems(int pid, AutobuffItemsConfig config)
    {
        SendBuffList(pid, CMD_SYNC_AUTOBUFF_ITEMS, config.Enabled, config.Buffs);
    }

    private static void SendBuffList(int pid, uint cmdId, bool enabled, IReadOnlyCollection<BuffConfig> buffs)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(enabled);
        bw.Write((uint)buffs.Count);
        foreach (var buff in buffs)
        {
            bw.Write(buff.StatusId);
            bw.Write(VirtualKeyMap.GetVk(buff.Key));
            bw.Write(buff.Enabled);
        }
        SendRaw(pid, cmdId, ms.ToArray());
    }

    public static void SyncSpammer(int pid, SpammerConfig config)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(config.Enabled);
        bw.Write((uint)config.Keys.Count);
        foreach (var key in config.Keys)
        {
            bw.Write(VirtualKeyMap.GetVk(key.Key));
            bw.Write(key.IntervalMs);
            bw.Write(key.Enabled);
        }
        SendRaw(pid, CMD_SYNC_SPAMMER, ms.ToArray());
    }

    public static void SyncRecovery(int pid, StatusRecoveryConfig config)
    {
        SendRecoveryList(pid, CMD_SYNC_RECOVERY, config.Enabled, false, "None", "None", config.Recoveries);
    }

    public static void SyncDebuffRecovery(int pid, DebuffRecoveryConfig config)
    {
        SendRecoveryList(pid, CMD_SYNC_DEBUFF_RECOVERY, config.Enabled, config.AutoStand,
            config.GroupStatusKey, config.GroupNewStatusKey, config.Recoveries);
    }

    private static void SendRecoveryList(int pid, uint cmdId, bool enabled, bool autoStand,
        string groupStatusKey, string groupNewStatusKey, IReadOnlyCollection<RecoveryConfig> recoveries)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(enabled);
        bw.Write(autoStand);
        bw.Write(VirtualKeyMap.GetVk(groupStatusKey));
        bw.Write(VirtualKeyMap.GetVk(groupNewStatusKey));
        bw.Write((uint)recoveries.Count);
        foreach (var rec in recoveries)
        {
            bw.Write(rec.StatusId);
            bw.Write(VirtualKeyMap.GetVk(rec.Key));
            bw.Write(rec.Enabled);
        }
        SendRaw(pid, cmdId, ms.ToArray());
    }

    public static void SyncSkillTimers(int pid, SkillTimerConfig config)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(config.Enabled);
        WriteTimerEntry(bw, config.Timer1);
        WriteTimerEntry(bw, config.Timer2);
        WriteTimerEntry(bw, config.Timer3);
        SendRaw(pid, CMD_SYNC_SKILL_TIMER, ms.ToArray());
    }

    private static void WriteTimerEntry(BinaryWriter bw, SkillTimerEntry entry)
    {
        bw.Write(VirtualKeyMap.GetVk(entry.Key));
        bw.Write(entry.DelaySeconds);
        bw.Write(entry.Enabled);
    }

    public static void SyncAtkDefMode(int pid, AtkDefModeConfig config)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(config.Enabled);
        bw.Write(VirtualKeyMap.GetVk(config.SpammerKey));
        bw.Write(config.SpammerWithClick);
        bw.Write(config.SpammerDelay);
        bw.Write(config.SwitchDelay);
        bw.Write((uint)config.AtkKeys.Count);
        foreach (var k in config.AtkKeys) bw.Write(VirtualKeyMap.GetVk(k.Key));
        bw.Write((uint)config.DefKeys.Count);
        foreach (var k in config.DefKeys) bw.Write(VirtualKeyMap.GetVk(k.Key));
        SendRaw(pid, CMD_SYNC_ATK_DEF, ms.ToArray());
    }

    public static void SyncMacroSongs(int pid, MacroSongConfig config)
    {
        SendMacroLanes(pid, CMD_SYNC_MACRO_SONG, config.Enabled, config.Lanes);
    }

    public static void SyncMacroSwitch(int pid, MacroSwitchConfig config)
    {
        SendMacroLanes(pid, CMD_SYNC_MACRO_SWITCH, config.Enabled, config.Lanes);
    }

    private static void SendMacroLanes(int pid, uint cmdId, bool enabled, IReadOnlyCollection<MacroChainLane> lanes)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(enabled);
        bw.Write((uint)lanes.Count);
        foreach (var lane in lanes)
        {
            bw.Write(VirtualKeyMap.GetVk(lane.TriggerKey));
            bw.Write(VirtualKeyMap.GetVk(lane.DaggerKey));
            bw.Write(VirtualKeyMap.GetVk(lane.InstrumentKey));
            bw.Write(lane.DelayMs);
            bw.Write(lane.InfinityLoop);
            bw.Write((uint)lane.Entries.Count);
            foreach (var entry in lane.Entries)
            {
                bw.Write(VirtualKeyMap.GetVk(entry.Key));
                bw.Write(entry.DelayMs);
                bw.Write(entry.HasClick);
            }
        }
        SendRaw(pid, cmdId, ms.ToArray());
    }

    private static void Send<T>(int pid, uint cmdId, T payload) where T : struct
    {
        int size = Marshal.SizeOf(payload);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(payload, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        SendRaw(pid, cmdId, arr);
    }

    private static void SendRaw(int pid, uint cmdId, byte[] payload)
    {
        string pipeName = $"PRT_Agent_{pid}";
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            pipe.Connect(50);
            byte[] headerArr = StructToBytes(new Header { CommandId = cmdId, PayloadLength = (uint)payload.Length });
            pipe.Write(headerArr, 0, headerArr.Length);
            if (payload.Length > 0)
                pipe.Write(payload, 0, payload.Length);
        }
        catch { /* Agent not injected or busy */ }
    }

    private static byte[] StructToBytes<T>(T str) where T : struct
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
}

public static class VirtualKeyMap
{
    public static ushort GetVk(string keyName)
    {
        // Placeholder: in a real scenario, this would map "F1" -> 0x70
        return 0;
    }
}
