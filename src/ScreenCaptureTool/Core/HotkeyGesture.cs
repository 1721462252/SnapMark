using System.Windows.Forms;

namespace ScreenCaptureTool.Core;

public readonly record struct HotkeyGesture
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    public static HotkeyGesture Default { get; } = new(Keys.S, control: true, shift: true, alt: false, windows: false);

    public HotkeyGesture(Keys key, bool control, bool shift, bool alt, bool windows)
    {
        Key = key;
        Control = control;
        Shift = shift;
        Alt = alt;
        Windows = windows;
    }

    public Keys Key { get; }

    public bool Control { get; }

    public bool Shift { get; }

    public bool Alt { get; }

    public bool Windows { get; }

    public static HotkeyGesture Parse(string value)
    {
        if (TryParse(value, out HotkeyGesture gesture))
        {
            return gesture;
        }

        throw new FormatException($"Invalid hotkey gesture: {value}");
    }

    public static bool TryParse(string? value, out HotkeyGesture gesture)
    {
        gesture = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        bool control = false;
        bool shift = false;
        bool alt = false;
        bool windows = false;
        Keys? key = null;

        foreach (string rawPart in value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawPart.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                rawPart.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                control = true;
                continue;
            }

            if (rawPart.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                shift = true;
                continue;
            }

            if (rawPart.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
                continue;
            }

            if (rawPart.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                rawPart.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                windows = true;
                continue;
            }

            if (key is not null || !Enum.TryParse(rawPart, ignoreCase: true, out Keys parsedKey))
            {
                return false;
            }

            key = NormalizeKey(parsedKey);
        }

        if (key is null || IsModifierOnlyKey(key.Value))
        {
            return false;
        }

        gesture = new HotkeyGesture(key.Value, control, shift, alt, windows);
        return true;
    }

    public static bool TryFromKeyEvent(KeyEventArgs e, out HotkeyGesture gesture)
    {
        Keys key = NormalizeKey(e.KeyCode);
        if (IsModifierOnlyKey(key))
        {
            gesture = default;
            return false;
        }

        gesture = new HotkeyGesture(
            key,
            e.Control,
            e.Shift,
            e.Alt,
            (e.Modifiers & Keys.LWin) == Keys.LWin || (e.Modifiers & Keys.RWin) == Keys.RWin);
        return true;
    }

    public uint ToWin32Modifiers()
    {
        uint modifiers = 0;
        if (Alt)
        {
            modifiers |= ModAlt;
        }

        if (Control)
        {
            modifiers |= ModControl;
        }

        if (Shift)
        {
            modifiers |= ModShift;
        }

        if (Windows)
        {
            modifiers |= ModWin;
        }

        return modifiers;
    }

    public override string ToString()
    {
        List<string> parts = [];
        if (Control)
        {
            parts.Add("Ctrl");
        }

        if (Shift)
        {
            parts.Add("Shift");
        }

        if (Alt)
        {
            parts.Add("Alt");
        }

        if (Windows)
        {
            parts.Add("Win");
        }

        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    private static Keys NormalizeKey(Keys key)
    {
        return key switch
        {
            Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => Keys.ControlKey,
            Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => Keys.ShiftKey,
            Keys.Menu or Keys.LMenu or Keys.RMenu => Keys.Menu,
            Keys.LWin or Keys.RWin => Keys.LWin,
            _ => key & Keys.KeyCode
        };
    }

    private static bool IsModifierOnlyKey(Keys key)
    {
        return key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin;
    }
}
