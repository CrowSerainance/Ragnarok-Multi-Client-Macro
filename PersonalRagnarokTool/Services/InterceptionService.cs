using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Services;

public sealed class InterceptionService : IDisposable
{
    private IntPtr _context = IntPtr.Zero;
    private int _keyboardId = -1;
    private int _mouseId = -1;
    private bool _initialized;

    public bool IsAvailable => _initialized;

    public InterceptionService()
    {
        try
        {
            _context = InterceptionInterop.interception_create_context();
            if (_context != IntPtr.Zero)
            {
                // Find first valid keyboard and mouse
                for (int i = 1; i <= 10; i++)
                {
                    if (_keyboardId == -1 && InterceptionInterop.interception_is_keyboard(i) > 0)
                        _keyboardId = i;
                }

                for (int i = 11; i <= 20; i++)
                {
                    if (_mouseId == -1 && InterceptionInterop.interception_is_mouse(i) > 0)
                        _mouseId = i;
                }

                if (_keyboardId != -1) // At least a keyboard found
                {
                    _initialized = true;
                }
            }
        }
        catch
        {
            // Interception DLL or driver not present
            _initialized = false;
        }
    }

    public void SendKey(int vk)
    {
        if (!_initialized || _keyboardId == -1) return;

        ushort scanCode = (ushort)NativeMethods.MapVirtualKeyW((uint)vk, 0);

        var strokeDown = new InterceptionInterop.Stroke
        {
            Key = new InterceptionInterop.KeyStroke
            {
                Code = scanCode,
                State = (ushort)InterceptionInterop.KeyState.Down
            }
        };

        var strokeUp = new InterceptionInterop.Stroke
        {
            Key = new InterceptionInterop.KeyStroke
            {
                Code = scanCode,
                State = (ushort)InterceptionInterop.KeyState.Up
            }
        };

        InterceptionInterop.interception_send(_context, _keyboardId, ref strokeDown, 1);
        Thread.Sleep(new Random().Next(25, 55));
        InterceptionInterop.interception_send(_context, _keyboardId, ref strokeUp, 1);
    }

    public void SendClick(int x, int y)
    {
        if (!_initialized || _mouseId == -1) return;

        // Note: Interception mouse sending usually requires absolute coordinates (0-65535) 
        // if we use absolute flags, or we just rely on standard SendInput for mouse if Gepard only blocks keys.
        // For full hardware mouse clicks, we use Interception.
        var strokeDown = new InterceptionInterop.Stroke
        {
            Mouse = new InterceptionInterop.MouseStroke
            {
                State = (ushort)InterceptionInterop.MouseState.LeftDown
            }
        };

        var strokeUp = new InterceptionInterop.Stroke
        {
            Mouse = new InterceptionInterop.MouseStroke
            {
                State = (ushort)InterceptionInterop.MouseState.LeftUp
            }
        };

        // Move cursor via SetCursorPos first, then send hardware click
        NativeMethods.SetCursorPos(x, y);
        Thread.Sleep(new Random().Next(5, 12));

        InterceptionInterop.interception_send(_context, _mouseId, ref strokeDown, 1);
        Thread.Sleep(new Random().Next(25, 55));
        InterceptionInterop.interception_send(_context, _mouseId, ref strokeUp, 1);
    }

    public void Dispose()
    {
        if (_context != IntPtr.Zero)
        {
            InterceptionInterop.interception_destroy_context(_context);
            _context = IntPtr.Zero;
        }
    }
}
