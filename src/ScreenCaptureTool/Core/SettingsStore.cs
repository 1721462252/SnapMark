using System.Text.Json;

namespace ScreenCaptureTool.Core;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string appDataDirectory;

    public SettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenCaptureTool"))
    {
    }

    public SettingsStore(string appDataDirectory)
    {
        this.appDataDirectory = appDataDirectory;
    }

    public string SettingsPath => Path.Combine(appDataDirectory, "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return AppSettings.Default;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            StoredSettings? stored = JsonSerializer.Deserialize<StoredSettings>(json, JsonOptions);
            if (stored is null)
            {
                return AppSettings.Default;
            }

            return ToAppSettings(stored);
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
        catch (IOException)
        {
            return AppSettings.Default;
        }
        catch (UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(appDataDirectory);
        var stored = new StoredSettings
        {
            Hotkey = settings.Hotkey.ToString(),
            SaveDirectory = string.IsNullOrWhiteSpace(settings.SaveDirectory) ? null : settings.SaveDirectory,
            ImageFormat = string.IsNullOrWhiteSpace(settings.ImageFormat) ? AppSettings.Default.ImageFormat : settings.ImageFormat
        };

        string json = JsonSerializer.Serialize(stored, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static AppSettings ToAppSettings(StoredSettings stored)
    {
        HotkeyGesture hotkey = HotkeyGesture.TryParse(stored.Hotkey, out HotkeyGesture parsed)
            ? parsed
            : HotkeyGesture.Default;

        return new AppSettings
        {
            Hotkey = hotkey,
            SaveDirectory = string.IsNullOrWhiteSpace(stored.SaveDirectory) ? null : stored.SaveDirectory,
            ImageFormat = string.IsNullOrWhiteSpace(stored.ImageFormat) ? AppSettings.Default.ImageFormat : stored.ImageFormat
        };
    }

    private sealed class StoredSettings
    {
        public string? Hotkey { get; set; }

        public string? SaveDirectory { get; set; }

        public string? ImageFormat { get; set; }
    }
}
