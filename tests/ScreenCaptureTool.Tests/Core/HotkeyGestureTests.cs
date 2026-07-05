using System.Windows.Forms;
using ScreenCaptureTool.Core;

namespace ScreenCaptureTool.Tests.Core;

public sealed class HotkeyGestureTests
{
    [Fact]
    public void Default_uses_control_shift_s()
    {
        HotkeyGesture gesture = HotkeyGesture.Default;

        Assert.Equal(Keys.S, gesture.Key);
        Assert.True(gesture.Control);
        Assert.True(gesture.Shift);
        Assert.False(gesture.Alt);
        Assert.False(gesture.Windows);
        Assert.Equal("Ctrl+Shift+S", gesture.ToString());
    }

    [Theory]
    [InlineData("Ctrl+Shift+S", Keys.S, true, true, false, false)]
    [InlineData("Alt+PrintScreen", Keys.PrintScreen, false, false, true, false)]
    [InlineData("Win+Shift+A", Keys.A, false, true, false, true)]
    public void Parse_round_trips_display_text(
        string value,
        Keys expectedKey,
        bool expectedControl,
        bool expectedShift,
        bool expectedAlt,
        bool expectedWindows)
    {
        HotkeyGesture gesture = HotkeyGesture.Parse(value);

        Assert.Equal(expectedKey, gesture.Key);
        Assert.Equal(expectedControl, gesture.Control);
        Assert.Equal(expectedShift, gesture.Shift);
        Assert.Equal(expectedAlt, gesture.Alt);
        Assert.Equal(expectedWindows, gesture.Windows);
        Assert.Equal(gesture, HotkeyGesture.Parse(gesture.ToString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+Shift")]
    [InlineData("DefinitelyNotAKey")]
    public void TryParse_rejects_invalid_gestures(string value)
    {
        bool parsed = HotkeyGesture.TryParse(value, out HotkeyGesture gesture);

        Assert.False(parsed);
        Assert.Equal(default, gesture);
    }

    [Fact]
    public void ToWin32Modifiers_combines_selected_modifiers()
    {
        var gesture = new HotkeyGesture(Keys.F9, control: true, shift: false, alt: true, windows: true);

        Assert.Equal(0x0001u | 0x0002u | 0x0008u, gesture.ToWin32Modifiers());
    }

    [Fact]
    public void FromKeyEvent_ignores_modifier_key_as_primary_key()
    {
        var args = new KeyEventArgs(Keys.Control | Keys.Shift | Keys.ControlKey);

        Assert.False(HotkeyGesture.TryFromKeyEvent(args, out _));
    }
}
