using System.Runtime.InteropServices;
using System.Text;

namespace PersonalRagnarokTool.Services;

public sealed class MemoryService
{
    private readonly ProcessAttachmentService _attachmentService;

    public MemoryService(ProcessAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    public bool IsValid => _attachmentService.IsAttached;
    public IntPtr BaseAddress => _attachmentService.BaseAddress;

    public bool ReadBytes(IntPtr address, byte[] buffer)
    {
        if (!IsValid) return false;

        if (_attachmentService.DriverHandle != IntPtr.Zero && _attachmentService.DriverHandle.ToInt64() != -1)
        {
            var req = new NativeMethods.READ_MEM_REQ
            {
                ProcessId = (uint)_attachmentService.ProcessId,
                TargetAddress = (ulong)address.ToInt64(),
                Size = (ulong)buffer.Length
            };
            return NativeMethods.DeviceIoControl(_attachmentService.DriverHandle, NativeMethods.IOCTL_READ_MEMORY, ref req, (uint)Marshal.SizeOf(req), buffer, (uint)buffer.Length, out _, IntPtr.Zero);
        }

        IntPtr hProcess = _attachmentService.Handle;
        if (hProcess == IntPtr.Zero || hProcess.ToInt64() == -1) return false;

        return NativeMethods.ReadProcessMemory(hProcess, address, buffer, (uint)buffer.Length, out _);
    }

    public bool WriteBytes(IntPtr address, byte[] buffer)
    {
        if (!IsValid) return false;

        if (_attachmentService.DriverHandle != IntPtr.Zero && _attachmentService.DriverHandle.ToInt64() != -1)
        {
            int reqSize = Marshal.SizeOf<NativeMethods.WRITE_MEM_REQ>();
            var ioBuffer = new byte[reqSize + buffer.Length];
            var req = new NativeMethods.WRITE_MEM_REQ
            {
                ProcessId = (uint)_attachmentService.ProcessId,
                TargetAddress = (ulong)address.ToInt64(),
                Size = (ulong)buffer.Length
            };

            IntPtr ptr = Marshal.AllocHGlobal(reqSize);
            Marshal.StructureToPtr(req, ptr, false);
            Marshal.Copy(ptr, ioBuffer, 0, reqSize);
            Marshal.FreeHGlobal(ptr);

            Buffer.BlockCopy(buffer, 0, ioBuffer, reqSize, buffer.Length);

            return NativeMethods.DeviceIoControl(_attachmentService.DriverHandle, NativeMethods.IOCTL_WRITE_MEMORY, ioBuffer, (uint)ioBuffer.Length, ioBuffer, (uint)ioBuffer.Length, out _, IntPtr.Zero);
        }

        IntPtr hProcess = _attachmentService.WriteHandle;
        if (hProcess == IntPtr.Zero || hProcess.ToInt64() == -1) return false;

        return NativeMethods.WriteProcessMemory(hProcess, address, buffer, (uint)buffer.Length, out _);
    }

    public uint ReadUInt32(IntPtr address)
    {
        var buf = new byte[4];
        if (ReadBytes(address, buf)) return BitConverter.ToUInt32(buf, 0);
        return 0;
    }

    public bool WriteUInt32(IntPtr address, uint value)
    {
        return WriteBytes(address, BitConverter.GetBytes(value));
    }
}
