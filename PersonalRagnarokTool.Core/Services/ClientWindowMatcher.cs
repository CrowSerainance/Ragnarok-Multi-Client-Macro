using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public static class ClientWindowMatcher
{
    public static ClientWindowMatchResult Match(ClientWindowRef? boundWindow, IEnumerable<ClientWindowRef> availableWindows)
    {
        if (boundWindow is null || boundWindow.WindowHandle == 0)
        {
            return new ClientWindowMatchResult(
                ClientWindowMatchKind.NotBound,
                null,
                false,
                false,
                "Unbound",
                "Bind this profile to a live client window.");
        }

        var windows = availableWindows.ToArray();
        ClientWindowRef? match = windows.FirstOrDefault(window => window.WindowHandle == boundWindow.WindowHandle);
        if (match is not null)
        {
            return new ClientWindowMatchResult(
                ClientWindowMatchKind.ExactHandle,
                match,
                true,
                false,
                "Live",
                $"Exact handle match on {match.DisplayText}.");
        }

        match = windows.FirstOrDefault(window =>
            window.ProcessId == boundWindow.ProcessId &&
            string.Equals(window.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return new ClientWindowMatchResult(
                ClientWindowMatchKind.ProcessAndTitle,
                match,
                true,
                true,
                "Rebound",
                $"Rebound by process id and title to {match.DisplayText}.");
        }

        match = windows.FirstOrDefault(window =>
            string.Equals(window.ProcessName, boundWindow.ProcessName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(window.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return new ClientWindowMatchResult(
                ClientWindowMatchKind.TitleAndProcessName,
                match,
                true,
                true,
                "Rebound",
                $"Rebound by process name and title to {match.DisplayText}.");
        }

        match = windows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId);
        if (match is not null)
        {
            return new ClientWindowMatchResult(
                ClientWindowMatchKind.ProcessOnly,
                match,
                true,
                true,
                "Rebound",
                $"Rebound by process id to {match.DisplayText}.");
        }

        return new ClientWindowMatchResult(
            ClientWindowMatchKind.Missing,
            null,
            false,
            false,
            "Missing",
            $"No live window matches {boundWindow.DisplayText}.");
    }
}
