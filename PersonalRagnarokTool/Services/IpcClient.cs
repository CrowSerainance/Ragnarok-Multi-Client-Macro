using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PersonalRagnarokTool.Services;

public sealed class IpcClient
{
    private const uint CMD_GET_GAME_STATE = 0x05;
    private const uint CMD_EXECUTE_MACRO = 0x06;
    private const uint CMD_EXECUTE_LUA = 0x07;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Header
    {
        public uint CommandId;
        public uint PayloadLength;
    }

    public enum TargetDirection : byte
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ExecuteMacroHeader
    {
        public uint MacroId;
        public ushort VirtualKey;
        public byte TargetDirection;
        public byte TargetDistance;
    }

    public void SendExecuteLuaCommand(int targetPid, string luaScript)
    {
        string pipeName = $"PRT_Agent_{targetPid}";
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            pipeClient.Connect(500);

            byte[] scriptBytes = System.Text.Encoding.UTF8.GetBytes(luaScript);
            var header = new Header
            {
                CommandId = CMD_EXECUTE_LUA,
                PayloadLength = (uint)scriptBytes.Length
            };

            int headerSize = Marshal.SizeOf<Header>();
            byte[] buffer = new byte[headerSize + scriptBytes.Length];

            IntPtr ptrHeader = Marshal.AllocHGlobal(headerSize);
            try
            {
                Marshal.StructureToPtr(header, ptrHeader, false);
                Marshal.Copy(ptrHeader, buffer, 0, headerSize);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrHeader);
            }

            Buffer.BlockCopy(scriptBytes, 0, buffer, headerSize, scriptBytes.Length);
            pipeClient.Write(buffer, 0, buffer.Length);
            pipeClient.Flush();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IpcClient] Lua Dispatch Failed: {ex.Message}");
        }
    }

    public void SendExecuteMacroCommand(int targetPid, uint macroId, ushort virtualKey, TargetDirection direction, byte distance)
    {
        string pipeName = $"PRT_Agent_{targetPid}";
        
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            pipeClient.Connect(500); // 500ms timeout

            var header = new Header
            {
                CommandId = CMD_EXECUTE_MACRO,
                PayloadLength = (uint)Marshal.SizeOf<ExecuteMacroHeader>()
            };

            var payload = new ExecuteMacroHeader
            {
                MacroId = macroId,
                VirtualKey = virtualKey,
                TargetDirection = (byte)direction,
                TargetDistance = distance
            };

            int headerSize = Marshal.SizeOf<Header>();
            int payloadSize = Marshal.SizeOf<ExecuteMacroHeader>();
            byte[] buffer = new byte[headerSize + payloadSize];

            IntPtr ptrHeader = Marshal.AllocHGlobal(headerSize);
            IntPtr ptrPayload = Marshal.AllocHGlobal(payloadSize);

            try
            {
                Marshal.StructureToPtr(header, ptrHeader, false);
                Marshal.Copy(ptrHeader, buffer, 0, headerSize);

                Marshal.StructureToPtr(payload, ptrPayload, false);
                Marshal.Copy(ptrPayload, buffer, headerSize, payloadSize);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrHeader);
                Marshal.FreeHGlobal(ptrPayload);
            }

            pipeClient.Write(buffer, 0, buffer.Length);
            pipeClient.Flush();
        }
        catch (TimeoutException)
        {
            // Log or handle the case where the client DLL is not ready
        }
        catch (Exception ex)
        {
            // Log IPC exceptions
            System.Diagnostics.Debug.WriteLine($"[IpcClient] Failed to send macro execution command: {ex.Message}");
        }
    }
}
