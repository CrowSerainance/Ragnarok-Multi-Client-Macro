namespace PersonalRagnarokTool.Core.Services;

public enum ClientWindowMatchKind
{
    NotBound = 0,
    ExactHandle = 1,
    ProcessAndTitle = 2,
    ProcessOnly = 3,
    TitleAndProcessName = 4,
    Missing = 5,
}
