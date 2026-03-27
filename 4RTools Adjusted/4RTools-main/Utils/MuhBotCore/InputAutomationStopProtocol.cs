using System.Threading;

namespace _4RTools.Utils.MuhBotCore;

/// <summary>
/// Coordinates overlapping automation threads (Skill Spammer, macro chains, ATK/DEF) with
/// buff-style maintenance (Autobuff, status/debuff recovery) so they do not post conflicting
/// key/mouse messages in the same window of time.
/// </summary>
public static class InputAutomationStopProtocol
{
    private static int _exclusiveDepth;

    /// <summary>Call once when entering a high-priority automation pass (must pair with <see cref="LeaveExclusiveAutomation"/>).</summary>
    public static void EnterExclusiveAutomation()
    {
        Interlocked.Increment(ref _exclusiveDepth);
    }

    public static void LeaveExclusiveAutomation()
    {
        Interlocked.Decrement(ref _exclusiveDepth);
    }

    /// <summary>True while any exclusive lane (skill spam, macro fire, ATK/DEF) holds the protocol.</summary>
    public static bool ShouldYieldBuffStyleInput()
    {
        return Volatile.Read(ref _exclusiveDepth) > 0;
    }
}
