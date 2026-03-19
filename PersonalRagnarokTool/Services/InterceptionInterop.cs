using System.Runtime.InteropServices;

namespace PersonalRagnarokTool.Services;

/// <summary>
/// P/Invoke wrappers for the Interception API.
/// Requires interception.dll (x64) to be present in the application directory,
/// and the Interception driver to be installed on the system.
/// </summary>
public static class InterceptionInterop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InterceptionPredicate(int device);

    [Flags]
    public enum KeyState : ushort
    {
        Down = 0x00,
        Up = 0x01,
        E0 = 0x02,
        E1 = 0x04,
        TermsrvSetLED = 0x08,
        TermsrvShadow = 0x10,
        TermsrvVKPacket = 0x20
    }

    [Flags]
    public enum MouseState : ushort
    {
        LeftDown = 0x001,
        LeftUp = 0x002,
        RightDown = 0x004,
        RightUp = 0x008,
        MiddleDown = 0x010,
        MiddleUp = 0x020,
        Button4Down = 0x040,
        Button4Up = 0x080,
        Button5Down = 0x100,
        Button5Up = 0x200,
        Wheel = 0x400,
        HWheel = 0x800
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseStroke
    {
        public ushort State;
        public ushort Flags;
        public short Rolling;
        public int X;
        public int Y;
        public uint Information;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyStroke
    {
        public ushort Code;
        public ushort State;
        public uint Information;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Stroke
    {
        [FieldOffset(0)] public MouseStroke Mouse;
        [FieldOffset(0)] public KeyStroke Key;
    }

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr interception_create_context();

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void interception_destroy_context(IntPtr context);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void interception_set_filter(IntPtr context, InterceptionPredicate predicate, ushort filter);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_receive(IntPtr context, int device, ref Stroke stroke, uint nstroke);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_send(IntPtr context, int device, ref Stroke stroke, uint nstroke);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_is_keyboard(int device);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_is_mouse(int device);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_wait(IntPtr context);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_wait_with_timeout(IntPtr context, uint milliseconds);

    public const ushort INTERCEPTION_FILTER_KEY_DOWN = 0x01;
    public const ushort INTERCEPTION_FILTER_KEY_UP = 0x02;
    public const ushort INTERCEPTION_FILTER_KEY_E0 = 0x04;
    public const ushort INTERCEPTION_FILTER_KEY_E1 = 0x08;
    public const ushort INTERCEPTION_FILTER_KEY_TERMSRV_SET_LED = 0x10;
    public const ushort INTERCEPTION_FILTER_KEY_TERMSRV_SHADOW = 0x20;
    public const ushort INTERCEPTION_FILTER_KEY_TERMSRV_VKPACKET = 0x40;
    
    public const ushort INTERCEPTION_FILTER_KEY_ALL = 0xFFFF;
}
