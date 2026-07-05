using ScreenCaptureTool.Core;
using ScreenCaptureTool.Services;
using ScreenCaptureTool.UI;

namespace ScreenCaptureTool.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly SettingsStore settingsStore;
    private readonly HotkeyService hotkeyService;
    private readonly CaptureService captureService = new();
    private readonly ExportService exportService = new();
    private AppSettings settings;
    private OverlayForm? overlayForm;

    public TrayApplicationContext()
    {
        settingsStore = new SettingsStore();
        settings = settingsStore.Load();
        hotkeyService = new HotkeyService();
        hotkeyService.HotkeyPressed += (_, _) => StartCapture();

        notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Screen Capture Tool",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        notifyIcon.DoubleClick += (_, _) => StartCapture();

        if (!TryRegisterHotkey(settings.Hotkey))
        {
            notifyIcon.ShowBalloonTip(
                3000,
                "Screen Capture Tool",
                $"快捷键 {settings.Hotkey} 已被占用，请在设置中重新选择。",
                ToolTipIcon.Warning);
        }
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("开始截图", null, (_, _) => StartCapture());
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private void StartCapture()
    {
        if (overlayForm is not null)
        {
            overlayForm.Activate();
            return;
        }

        try
        {
            CaptureResult capture = captureService.CaptureVirtualDesktop();
            overlayForm = new OverlayForm(capture, settings, settingsStore, exportService);
            overlayForm.FormClosed += (_, _) => overlayForm = null;
            overlayForm.Show();
            overlayForm.Activate();
        }
        catch (Exception ex)
        {
            notifyIcon.ShowBalloonTip(3000, "截图失败", ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(settings, TryRegisterHotkey);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        settings = form.ResultSettings;
        settingsStore.Save(settings);
    }

    private bool TryRegisterHotkey(HotkeyGesture gesture)
    {
        HotkeyGesture? oldGesture = hotkeyService.Current;
        if (hotkeyService.Register(gesture))
        {
            return true;
        }

        if (oldGesture is not null)
        {
            hotkeyService.Register(oldGesture.Value);
        }

        return false;
    }

    protected override void ExitThreadCore()
    {
        overlayForm?.Close();
        hotkeyService.Dispose();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
