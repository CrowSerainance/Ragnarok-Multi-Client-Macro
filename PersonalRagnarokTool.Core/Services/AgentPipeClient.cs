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
        public ushort YggKey;
        public int YggThreshold;
        public int DelayMs;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SyncBuffEntry
    {
        public uint StatusId;
        public ushort Key;
        public bool Enabled;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SyncSpammerEntry
    {
        public ushort Key;
        public int IntervalMs;
        public bool Enabled;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SyncRecoveryEntry
    {
        public uint StatusId;
        public ushort Key;
        public bool Enabled;
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
            YggKey = VirtualKeyMap.GetVk(config.YggKey),
            YggThreshold = config.YggThreshold,
            DelayMs = config.DelayMs
        };
        Send(pid, CMD_SYNC_AUTOPOT, payload);
    }

    public static void SyncAutobuff(int pid, AutobuffConfig config)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        
        bw.Write(config.Enabled);
        bw.Write((uint)config.Buffs.Count);
        
        foreach (var buff in config.Buffs)
        {
            bw.Write(buff.StatusId);
            bw.Write(VirtualKeyMap.GetVk(buff.Key));
            bw.Write(buff.Enabled);
        }
        
        SendRaw(pid, CMD_SYNC_AUTOBUFF, ms.ToArray());
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
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        
        bw.Write(config.Enabled);
        bw.Write((uint)config.Recoveries.Count);
        
        foreach (var rec in config.Recoveries)
        {
            bw.Write(rec.StatusId);
            bw.Write(VirtualKeyMap.GetVk(rec.Key));
            bw.Write(rec.Enabled);
        }
        
        SendRaw(pid, CMD_SYNC_RECOVERY, ms.ToArray());
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
            pipe.Connect(50); // Short timeout
            
            var header = new Header { CommandId = cmdId, PayloadLength = (uint)payload.Length };
            byte[] headerArr = StructToBytes(header);
            
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

// Dummy VirtualKeyMap if it doesn't exist in Core
// This should ideally use the one in PersonalRagnarokTool.Services
public static class VirtualKeyMap
{
    public static ushort GetVk(string keyName)
    {
        // Placeholder: in a real scenario, this would map "F1" -> 0x70
        return 0; 
    }
}
