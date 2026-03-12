namespace PersonalRagnarokTool.Core.Services;

public static class HotkeyText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.Trim();
        string upper = value.ToUpperInvariant();
        return upper switch
        {
            "RETURN" => "Enter",
            "ESCAPE" => "Esc",
            "SPACEBAR" => "Space",
            "PRIOR" => "PgUp",
            "NEXT" => "PgDown",
            "OEMPLUS" or "OEM_PLUS" or "PLUS" or "+" => "OemPlus",
            "OEMMINUS" or "OEM_MINUS" or "MINUS" or "-" => "OemMinus",
            var x when x.StartsWith("NUMPAD", StringComparison.Ordinal) => $"Num{x[6..]}",
            var x when x.StartsWith("NUM", StringComparison.Ordinal) && x.Length > 3 => $"Num{x[3..]}",
            var x when x.StartsWith("F", StringComparison.Ordinal) && x.Length > 1 && int.TryParse(x[1..], out _) => x,
            var x when x.Length == 1 => x,
            "PGUP" => "PgUp",
            "PGDOWN" => "PgDown",
            var x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant(),
        };
    }
}
