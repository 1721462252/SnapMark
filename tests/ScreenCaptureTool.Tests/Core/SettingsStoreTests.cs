using ScreenCaptureTool.Core;

namespace ScreenCaptureTool.Tests.Core;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_returns_defaults_when_file_is_missing()
    {
        string directory = CreateTempDirectory();
        var store = new SettingsStore(directory);

        AppSettings settings = store.Load();

        Assert.Equal(HotkeyGesture.Default, settings.Hotkey);
        Assert.Null(settings.SaveDirectory);
        Assert.Equal("png", settings.ImageFormat);
    }

    [Fact]
    public void Save_then_load_round_trips_settings()
    {
        string directory = CreateTempDirectory();
        var store = new SettingsStore(directory);
        var expected = new AppSettings
        {
            Hotkey = HotkeyGesture.Parse("Alt+PrintScreen"),
            SaveDirectory = Path.Combine(directory, "captures"),
            ImageFormat = "png"
        };

        store.Save(expected);
        AppSettings actual = store.Load();

        Assert.Equal(expected.Hotkey, actual.Hotkey);
        Assert.Equal(expected.SaveDirectory, actual.SaveDirectory);
        Assert.Equal(expected.ImageFormat, actual.ImageFormat);
    }

    [Fact]
    public void Save_uses_injected_app_data_directory()
    {
        string directory = CreateTempDirectory();
        var store = new SettingsStore(directory);

        store.Save(AppSettings.Default);

        Assert.True(File.Exists(Path.Combine(directory, "settings.json")));
    }

    [Fact]
    public void Load_returns_defaults_when_json_is_malformed()
    {
        string directory = CreateTempDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "settings.json"), "{not valid json");
        var store = new SettingsStore(directory);

        AppSettings settings = store.Load();

        Assert.Equal(AppSettings.Default.Hotkey, settings.Hotkey);
        Assert.Null(settings.SaveDirectory);
        Assert.Equal(AppSettings.Default.ImageFormat, settings.ImageFormat);
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ScreenCaptureTool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
