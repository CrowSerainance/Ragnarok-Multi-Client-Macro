using System.Diagnostics;
using System.Text;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Services;

public sealed class ClientDiscoveryService
{
    private readonly int _currentProcessId;

    public ClientDiscoveryService(int currentProcessId)
    {
        _currentProcessId = currentProcessId;
    }

    public IReadOnlyList<ClientWindowRef> DiscoverWindows()
    {
        var windows = new List<ClientWindowRef>();

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
            {
                return true;
            }

            int titleLength = NativeMethods.GetWindowTextLength(hWnd);
            if (titleLength <= 0)
            {
                return true;
            }

            var builder = new StringBuilder(titleLength + 1);
            _ = NativeMethods.GetWindowText(hWnd, builder, builder.Capacity);
            var title = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            if (processId == 0 || processId == _currentProcessId)
            {
                return true;
            }

            if (!NativeMethods.GetClientRect(hWnd, out var clientRect))
            {
                return true;
            }

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;
            if (width < 320 || height < 240)
            {
                return true;
            }

            string processName;
            try
            {
                processName = Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                return true;
            }

            windows.Add(new ClientWindowRef
            {
                WindowHandle = hWnd.ToInt64(),
                ProcessId = (int)processId,
                ProcessName = processName,
                WindowTitle = title,
                ClientWidth = width,
                ClientHeight = height,
            });

            return true;
        }, IntPtr.Zero);

        return windows
            .OrderByDescending(IsLikelyGameClient)
            .ThenBy(window => window.WindowTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsLikelyGameClient(ClientWindowRef window)
    {
        string sample = $"{window.ProcessName} {window.WindowTitle}";
        string[] hints = ["rag", "ragnarok", "ro", "muh", "gepard", "client"];
        return hints.Any(hint => sample.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }
}
