using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public static class ClickExecutionPlanner
{
    public static int ResolveClickCount(MacroBinding binding, TraceSequence? trace)
    {
        if (binding.ClickCountOverride is > 0)
        {
            return binding.ClickCountOverride.Value;
        }

        if (trace is not null && trace.Points.Count > 0)
        {
            return trace.Points.Count;
        }

        return 1;
    }
}
