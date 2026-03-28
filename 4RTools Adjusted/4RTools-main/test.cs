using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static int ExtractMacroSlotIndex(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return int.MaxValue;

        int start = key.StartsWith("in", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        int end = key.IndexOf("mac", StringComparison.OrdinalIgnoreCase);
        if (end > start && int.TryParse(key.Substring(start, end - start), out int index))
            return index;

        return int.MaxValue;
    }

    static void Main()
    {
        var keys = new List<string> { "in3mac1", "in1mac1", "in4mac1", "in2mac1" };
        var ordered = keys.OrderBy(ExtractMacroSlotIndex).ToList();
        
        foreach (var k in ordered)
        {
            Console.WriteLine(k + " -> " + ExtractMacroSlotIndex(k));
        }
    }
}
