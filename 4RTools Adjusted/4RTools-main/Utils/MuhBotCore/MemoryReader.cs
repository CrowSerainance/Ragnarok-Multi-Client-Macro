#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace _4RTools.Utils.MuhBotCore;

/// <summary>
/// Typed read/write and pointer chain resolution for process memory.
/// Supports direct ReadProcessMemory and optional driver-backed reads.
/// </summary>
public class MemoryReader
{
    private readonly ProcessManager _processManager;

    public MemoryReader(ProcessManager processManager)
    {
        _processManager = processManager;
    }

    private IntPtr Handle => _processManager.Handle;
    private IntPtr Base => _processManager.BaseAddress;
    private IntPtr WHandle => _processManager.WriteHandle;

    /// <summary>True when we have a valid process handle (attached).</summary>
    public bool IsValid => _processManager.IsAttached;

    public void CloseWriteHandle() => _processManager.CloseWriteHandle();

    public bool ReadBytes(IntPtr address, byte[] buffer)
    {
        return InternalRead(address, buffer, (uint)buffer.Length);
    }

    public bool WriteBytes(IntPtr address, byte[] buffer)
    {
        return InternalWrite(address, buffer, (uint)buffer.Length);
    }

    public uint ReadUInt32(IntPtr address)
    {
        var buf = new byte[4];
        if (!InternalRead(address, buf, 4))
            return 0;
        return BitConverter.ToUInt32(buf, 0);
    }

    public int ReadInt32(IntPtr address)
    {
        var buf = new byte[4];
        if (!InternalRead(address, buf, 4))
            return 0;
        return BitConverter.ToInt32(buf, 0);
    }

    public float ReadFloat(IntPtr address)
    {
        var buf = new byte[4];
        if (!InternalRead(address, buf, 4))
            return 0f;
        return BitConverter.ToSingle(buf, 0);
    }

    public IntPtr ReadPointer(IntPtr address, int offset = 0)
    {
        var buf = new byte[4];
        if (!InternalRead(IntPtr.Add(address, offset), buf, 4))
            return IntPtr.Zero;

        uint raw = BitConverter.ToUInt32(buf, 0);
        if (raw == 0) return IntPtr.Zero;
        return IntPtr.Size == 8
            ? new IntPtr(unchecked((long)raw))
            : new IntPtr(unchecked((int)raw));
    }

    /// <summary>
    /// Resolve multi-level pointer chain: base + offsets[0] -> read ptr -> + offsets[1] -> ... -> final + lastOffset.
    /// </summary>
    public IntPtr ResolvePointerChain(IntPtr baseAddr, int[] offsets, int finalOffset = 0)
    {
        IntPtr addr = baseAddr;
        for (int i = 0; i < offsets.Length; i++)
        {
            addr = IntPtr.Add(addr, offsets[i]);
            addr = ReadPointer(addr);
            if (addr == IntPtr.Zero)
                return IntPtr.Zero;
        }
        return IntPtr.Add(addr, finalOffset);
    }

    /// <summary>
    /// Resolve from process base: baseAddress + firstOffset, then follow pointer chain.
    /// </summary>
    public IntPtr ResolvePointerChainFromBase(int[] offsets, int finalOffset = 0)
    {
        IntPtr addr = ReadPointer(IntPtr.Add(Base, offsets[0]));
        if (addr == IntPtr.Zero)
            return IntPtr.Zero;
        for (int i = 1; i < offsets.Length; i++)
        {
            addr = ReadPointer(IntPtr.Add(addr, offsets[i]));
            if (addr == IntPtr.Zero)
                return IntPtr.Zero;
        }
        return IntPtr.Add(addr, finalOffset);
    }

    public string ReadString(IntPtr address, int maxLength = 64, Encoding? encoding = null)
    {
        encoding ??= Encoding.ASCII;
        var buf = new byte[maxLength];
        if (!InternalRead(address, buf, (uint)maxLength))
            return string.Empty;
        int end = Array.IndexOf(buf, (byte)0);
        if (end < 0) end = maxLength;
        return encoding.GetString(buf, 0, end).TrimEnd('\0');
    }

    public bool WriteUInt32(IntPtr address, uint value)
    {
        var buf = BitConverter.GetBytes(value);
        return InternalWrite(address, buf, (uint)buf.Length);
    }

    public bool WriteInt32(IntPtr address, int value)
    {
        var buf = BitConverter.GetBytes(value);
        return InternalWrite(address, buf, (uint)buf.Length);
    }

    public bool WriteFloat(IntPtr address, float value)
    {
        var buf = BitConverter.GetBytes(value);
        return InternalWrite(address, buf, (uint)buf.Length);
    }

    public IntPtr BaseAddress => Base;

    private bool InternalRead(IntPtr address, byte[] buffer, uint size)
    {
        IntPtr driverHandle = _processManager.DriverHandle;
        if (driverHandle != IntPtr.Zero && driverHandle.ToInt64() != -1)
        {
            var req = new Native.READ_MEM_REQ
            {
                ProcessId = (uint)_processManager.ProcessId,
                TargetAddress = (ulong)address.ToInt64(),
                Size = size
            };

            return Native.DeviceIoControl(
                driverHandle,
                Native.IOCTL_READ_MEMORY,
                ref req,
                (uint)Marshal.SizeOf<Native.READ_MEM_REQ>(),
                buffer,
                size,
                out _,
                IntPtr.Zero);
        }

        return Native.ReadProcessMemory(Handle, address, buffer, size, out _);
    }

    private bool InternalWrite(IntPtr address, byte[] data, uint size)
    {
        IntPtr driverHandle = _processManager.DriverHandle;
        if (driverHandle != IntPtr.Zero && driverHandle.ToInt64() != -1)
        {
            // Build combined buffer: [WRITE_MEM_REQ header][data]
            int headerSize = Marshal.SizeOf<Native.WRITE_MEM_REQ>();
            var req = new Native.WRITE_MEM_REQ
            {
                ProcessId = (uint)_processManager.ProcessId,
                TargetAddress = (ulong)address.ToInt64(),
                Size = size
            };

            byte[] inputBuffer = new byte[headerSize + size];
            // Marshal the header struct into the buffer
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            try
            {
                Marshal.StructureToPtr(req, headerPtr, false);
                Marshal.Copy(headerPtr, inputBuffer, 0, headerSize);
            }
            finally
            {
                Marshal.FreeHGlobal(headerPtr);
            }
            // Copy data payload after the header
            Array.Copy(data, 0, inputBuffer, headerSize, size);

            return Native.DeviceIoControl(
                driverHandle,
                Native.IOCTL_WRITE_MEMORY,
                inputBuffer,
                (uint)inputBuffer.Length,
                Array.Empty<byte>(),
                0,
                out _,
                IntPtr.Zero);
        }

        return Native.WriteProcessMemory(WHandle, address, data, size, out _);
    }
}
