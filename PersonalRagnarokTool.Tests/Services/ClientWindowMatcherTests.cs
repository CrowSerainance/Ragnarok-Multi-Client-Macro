using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Tests.Services;

public sealed class ClientWindowMatcherTests
{
    [Fact]
    public void Match_ReturnsNotBound_WhenProfileHasNoWindow()
    {
        ClientWindowMatchResult result = ClientWindowMatcher.Match(null, []);

        Assert.Equal(ClientWindowMatchKind.NotBound, result.Kind);
        Assert.False(result.IsLive);
        Assert.Null(result.ResolvedWindow);
    }

    [Fact]
    public void Match_ReturnsExactHandle_WhenHandleStillExists()
    {
        var bound = CreateWindow(101, 5001, "ragexe", "MuhRO");
        var live = CreateWindow(101, 5001, "ragexe", "MuhRO");

        ClientWindowMatchResult result = ClientWindowMatcher.Match(bound, [live]);

        Assert.Equal(ClientWindowMatchKind.ExactHandle, result.Kind);
        Assert.True(result.IsLive);
        Assert.False(result.WasRebound);
        Assert.Same(live, result.ResolvedWindow);
    }

    [Fact]
    public void Match_RebindsByProcessIdAndTitle_WhenHandleChanges()
    {
        var bound = CreateWindow(101, 5001, "ragexe", "MuhRO");
        var live = CreateWindow(202, 5001, "ragexe", "MuhRO");

        ClientWindowMatchResult result = ClientWindowMatcher.Match(bound, [live]);

        Assert.Equal(ClientWindowMatchKind.ProcessAndTitle, result.Kind);
        Assert.True(result.IsLive);
        Assert.True(result.WasRebound);
        Assert.Same(live, result.ResolvedWindow);
    }

    [Fact]
    public void Match_ReturnsMissing_WhenNoCandidateExists()
    {
        var bound = CreateWindow(101, 5001, "ragexe", "MuhRO");
        var other = CreateWindow(202, 9001, "notragexe", "Other Window");

        ClientWindowMatchResult result = ClientWindowMatcher.Match(bound, [other]);

        Assert.Equal(ClientWindowMatchKind.Missing, result.Kind);
        Assert.False(result.IsLive);
        Assert.Null(result.ResolvedWindow);
    }

    private static ClientWindowRef CreateWindow(long handle, int processId, string processName, string title)
    {
        return new ClientWindowRef
        {
            WindowHandle = handle,
            ProcessId = processId,
            ProcessName = processName,
            WindowTitle = title,
            ClientWidth = 1024,
            ClientHeight = 768,
        };
    }
}
