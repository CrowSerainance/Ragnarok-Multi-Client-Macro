#nullable enable
using System.Collections.Generic;
using System;
using System.Windows.Forms;

namespace _4RTools.Utils.MuhBotCore;

/// <summary>
/// Map key names (e.g. "F1", "Z") to virtual key codes.
/// </summary>
public static class KeyMap
{
    private static readonly Dictionary<string, int> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Function keys
        ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73, ["F5"] = 0x74,
        ["F6"] = 0x75, ["F7"] = 0x76, ["F8"] = 0x77, ["F9"] = 0x78, ["F10"] = 0x79,
        ["F11"] = 0x7A, ["F12"] = 0x7B,
        // Letters
        ["A"] = 0x41, ["B"] = 0x42, ["C"] = 0x43, ["D"] = 0x44, ["E"] = 0x45,
        ["F"] = 0x46, ["G"] = 0x47, ["H"] = 0x48, ["I"] = 0x49, ["J"] = 0x4A,
        ["K"] = 0x4B, ["L"] = 0x4C, ["M"] = 0x4D, ["N"] = 0x4E, ["O"] = 0x4F,
        ["P"] = 0x50, ["Q"] = 0x51, ["R"] = 0x52, ["S"] = 0x53, ["T"] = 0x54,
        ["U"] = 0x55, ["V"] = 0x56, ["W"] = 0x57, ["X"] = 0x58, ["Y"] = 0x59,
        ["Z"] = 0x5A,
        // Digits
        ["0"] = 0x30, ["1"] = 0x31, ["2"] = 0x32, ["3"] = 0x33, ["4"] = 0x34,
        ["5"] = 0x35, ["6"] = 0x36, ["7"] = 0x37, ["8"] = 0x38, ["9"] = 0x39,
        // Navigation / special
        ["Enter"] = 0x0D, ["Return"] = 0x0D,
        ["Escape"] = 0x1B, ["Esc"] = 0x1B,
        ["Tab"] = 0x09,
        ["Space"] = 0x20, ["Spacebar"] = 0x20,
        ["Backspace"] = 0x08,
        ["Insert"] = 0x2D, ["Ins"] = 0x2D,
        ["Delete"] = 0x2E, ["Del"] = 0x2E,
        ["Home"] = 0x24, ["End"] = 0x23,
        ["PgUp"] = 0x21, ["PageUp"] = 0x21,
        ["PgDown"] = 0x22, ["PageDown"] = 0x22,
        // Arrow keys
        ["Up"] = 0x26, ["Down"] = 0x28, ["Left"] = 0x25, ["Right"] = 0x27,
        // Numpad
        ["Num0"] = 0x60, ["Num1"] = 0x61, ["Num2"] = 0x62, ["Num3"] = 0x63, ["Num4"] = 0x64,
        ["Num5"] = 0x65, ["Num6"] = 0x66, ["Num7"] = 0x67, ["Num8"] = 0x68, ["Num9"] = 0x69,
    };

    public static int GetVkCode(string? keyName)
    {
        if (string.IsNullOrEmpty(keyName)) return 0;
        string normalized = keyName!.Trim();
        if (Map.TryGetValue(normalized, out int vk))
            return vk;

        // Fall back to any WinForms Keys enum name
        if (Enum.TryParse<Keys>(normalized, true, out var parsedKey))
        {
            return (int)parsedKey;
        }

        return 0;
    }
}
