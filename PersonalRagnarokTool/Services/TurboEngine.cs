using System.Collections.Concurrent;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Services;

public sealed class TurboEngine : IDisposable
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTurbos = new();
    private readonly InputDispatcher _dispatcher;

    public TurboEngine(InputDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void StartTurbo(MacroBinding binding, ClientWindowRef window, InputMethod method)
    {
        if (_activeTurbos.ContainsKey(binding.Id))
            return;

        var cts = new CancellationTokenSource();
        if (!_activeTurbos.TryAdd(binding.Id, cts))
        {
            cts.Dispose();
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Execute each step in the macro sequence
                    foreach (var step in binding.Steps)
                    {
                        if (cts.Token.IsCancellationRequested) break;

                        _dispatcher.SendKey(window, step.Key, method, out _);

                        if (step.DelayMs > 0)
                            await Task.Delay(step.DelayMs, cts.Token);
                    }

                    // Wait the repeat interval before cycling again
                    await Task.Delay(Math.Max(25, binding.IntervalMs), cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _activeTurbos.TryRemove(binding.Id, out _);
            }
        }, cts.Token);
    }

    public void StopTurbo(string bindingId)
    {
        if (_activeTurbos.TryRemove(bindingId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void StopAll()
    {
        foreach (var kvp in _activeTurbos)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _activeTurbos.Clear();
    }

    public bool IsRunning(string bindingId) => _activeTurbos.ContainsKey(bindingId);

    public void Dispose() => StopAll();
}
