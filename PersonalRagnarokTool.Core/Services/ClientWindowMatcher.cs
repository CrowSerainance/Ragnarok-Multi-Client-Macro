using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public enum ClientWindowMatchKind
{
    NotBound,
    ExactHandle,
    ProcessAndTitle,
    ProcessOnly,
    Missing,
}

public sealed record ClientWindowMatchResult(
    ClientWindowMatchKind Kind,
    bool IsLive,
    bool WasRebound,
    ClientWindowRef? ResolvedWindow);

public static class ClientWindowMatcher
{
    public static ClientWindowMatchResult Match(
        ClientWindowRef? boundWindow,
        IEnumerable<ClientWindowRef> availableWindows)
    {
        if (boundWindow is null)
            return new(ClientWindowMatchKind.NotBound, false, false, null);

        ClientWindowRef[] candidates = availableWindows.ToArray();

        ClientWindowRef? exact = candidates.FirstOrDefault(window => window.WindowHandle == boundWindow.WindowHandle);
        if (exact is not null)
            return new(ClientWindowMatchKind.ExactHandle, true, false, exact);

        ClientWindowRef? byProcessAndTitle = candidates.FirstOrDefault(window =>
            window.ProcessId == boundWindow.ProcessId &&
            string.Equals(window.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase));
        if (byProcessAndTitle is not null)
            return new(ClientWindowMatchKind.ProcessAndTitle, true, true, byProcessAndTitle);

        ClientWindowRef? byProcess = candidates.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId);
        if (byProcess is not null)
            return new(ClientWindowMatchKind.ProcessOnly, true, true, byProcess);

        return new(ClientWindowMatchKind.Missing, false, false, null);
    }
}
