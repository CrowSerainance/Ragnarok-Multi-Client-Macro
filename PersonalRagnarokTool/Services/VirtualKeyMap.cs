using System.Windows.Input;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

internal static class VirtualKeyMap
{
    public static bool TryGetVirtualKey(string? keyName, out int virtualKey)
    {
        virtualKey = 0;
        var normalized = HotkeyText.Normalize(keyName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        string upper = normalized.ToUpperInvariant();

        if (normalized.Length == 1)
        {
            var ch = normalized[0];
            if (char.IsLetterOrDigit(ch))
            {
                short vk = NativeMethods.VkKeyScanW(ch);
                if (vk != -1)
                {
                    virtualKey = vk & 0xFF;
                    return true;
                }
            }
        }

        if (upper.StartsWith("F", StringComparison.Ordinal)
            && int.TryParse(upper[1..], out int functionKey)
            && functionKey is >= 1 and <= 24)
        {
            virtualKey = 0x6F + functionKey;
            return true;
        }

        if (upper.StartsWith("NUM", StringComparison.Ordinal)
            && int.TryParse(upper[3..], out int numpadDigit)
            && numpadDigit is >= 0 and <= 9)
        {
            virtualKey = 0x60 + numpadDigit;
            return true;
        }

        virtualKey = upper switch
        {
            "ENTER" => 0x0D,
            "SPACE" => 0x20,
            "ESC" => 0x1B,
            "TAB" => 0x09,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PGUP" => 0x21,
            "PGDOWN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "OEMPLUS" => 0xBB,
            "OEMMINUS" => 0xBD,
            _ => 0,
        };

        if (virtualKey == 0 && Enum.TryParse<Key>(normalized, true, out var parsedKey))
        {
            virtualKey = KeyInterop.VirtualKeyFromKey(parsedKey);
        }

        return virtualKey != 0;
    }

    public static string NormalizeKeyName(Key key)
    {
        if (key is >= Key.F1 and <= Key.F24)
        {
            return key.ToString().ToUpperInvariant();
        }

        if (key is >= Key.A and <= Key.Z)
        {
            return key.ToString().ToUpperInvariant();
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return key.ToString()[1..];
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return $"Num{(int)(key - Key.NumPad0)}";
        }

        return key switch
        {
            Key.Return => "Enter",
            Key.Space => "Space",
            Key.Escape => "Esc",
            Key.Tab => "Tab",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PgUp",
            Key.PageDown => "PgDown",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.OemPlus => "OemPlus",
            Key.OemMinus => "OemMinus",
            _ => string.Empty,
        };
    }
}
