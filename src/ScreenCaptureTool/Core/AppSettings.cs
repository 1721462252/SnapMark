namespace ScreenCaptureTool.Core;

public sealed class AppSettings
{
    public HotkeyGesture Hotkey { get; set; } = HotkeyGesture.Default;

    public string? SaveDirectory { get; set; }

    public string ImageFormat { get; set; } = "png";

    public static AppSettings Default => new();
}
