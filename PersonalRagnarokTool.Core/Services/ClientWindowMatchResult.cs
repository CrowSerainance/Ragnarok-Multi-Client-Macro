using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public sealed record ClientWindowMatchResult(
    ClientWindowMatchKind Kind,
    ClientWindowRef? ResolvedWindow,
    bool IsLive,
    bool WasRebound,
    string Label,
    string Detail);
