using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PersonalRagnarokTool.Services;

public sealed class HotkeyDispatcher
{
    public enum ActionType { Macro, Lua }

    public struct ActionStep
    {
        public ActionType Type;
        public string Data; // MacroID or Lua Script
        public IpcClient.TargetDirection Direction;
        public byte Distance;
        public int DelayMs;
    }

    private struct TargetBinding
    {
        public int ProcessId;
        public List<ActionStep> Steps;
    }

    private readonly ConcurrentDictionary<Key, TargetBinding> _registry = new();
    private readonly IpcClient _ipcClient;

    public HotkeyDispatcher(IpcClient ipcClient)
    {
        _ipcClient = ipcClient ?? throw new ArgumentNullException(nameof(ipcClient));
    }

    public void RegisterChain(Key key, int targetProcessId, List<ActionStep> steps)
    {
        _registry[key] = new TargetBinding
        {
            ProcessId = targetProcessId,
            Steps = steps
        };
    }

    public void UnregisterKey(Key key)
    {
        _registry.TryRemove(key, out _);
    }

    public bool IsKeyRegistered(Key key)
    {
        return _registry.ContainsKey(key);
    }

    public void DispatchKey(Key key)
    {
        if (_registry.TryGetValue(key, out var binding))
        {
            ushort virtualKey = (ushort)KeyInterop.VirtualKeyFromKey(key);
            
            // Dispatch asynchronously to execute the chain
            Task.Run(async () =>
            {
                foreach (var step in binding.Steps)
                {
                    if (step.Type == ActionType.Macro)
                    {
                        uint macroId = uint.Parse(step.Data);
                        _ipcClient.SendExecuteMacroCommand(binding.ProcessId, macroId, virtualKey, step.Direction, step.Distance);
                    }
                    else if (step.Type == ActionType.Lua)
                    {
                        _ipcClient.SendExecuteLuaCommand(binding.ProcessId, step.Data);
                    }

                    if (step.DelayMs > 0)
                    {
                        await Task.Delay(step.DelayMs);
                    }
                }
            });
        }
    }
}
