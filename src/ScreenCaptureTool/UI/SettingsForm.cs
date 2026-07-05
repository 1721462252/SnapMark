using ScreenCaptureTool.Core;

namespace ScreenCaptureTool.UI;

public sealed class SettingsForm : Form
{
    private readonly Func<HotkeyGesture, bool> tryApplyHotkey;
    private readonly TextBox hotkeyTextBox = new();
    private readonly TextBox directoryTextBox = new();
    private readonly Label activeHotkeyLabel = new();
    private HotkeyGesture candidateHotkey;

    public SettingsForm(AppSettings currentSettings, Func<HotkeyGesture, bool> tryApplyHotkey)
    {
        this.tryApplyHotkey = tryApplyHotkey;
        candidateHotkey = currentSettings.Hotkey;
        ResultSettings = new AppSettings
        {
            Hotkey = currentSettings.Hotkey,
            SaveDirectory = currentSettings.SaveDirectory,
            ImageFormat = currentSettings.ImageFormat
        };

        InitializeComponent();
        activeHotkeyLabel.Text = $"当前生效快捷键：{currentSettings.Hotkey}";
        hotkeyTextBox.Text = currentSettings.Hotkey.ToString();
        directoryTextBox.Text = currentSettings.SaveDirectory ?? string.Empty;
    }

    public AppSettings ResultSettings { get; private set; }

    private void InitializeComponent()
    {
        Text = "设置";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 230);
        Font = new Font("Segoe UI", 9f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 3,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        activeHotkeyLabel.AutoSize = true;
        activeHotkeyLabel.Dock = DockStyle.Fill;
        layout.Controls.Add(activeHotkeyLabel, 0, 0);
        layout.SetColumnSpan(activeHotkeyLabel, 3);

        layout.Controls.Add(new Label
        {
            Text = "截图快捷键",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);

        hotkeyTextBox.ReadOnly = true;
        hotkeyTextBox.Dock = DockStyle.Fill;
        hotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;
        layout.Controls.Add(hotkeyTextBox, 1, 1);
        layout.SetColumnSpan(hotkeyTextBox, 2);

        layout.Controls.Add(new Label
        {
            Text = "保存目录",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 2);

        directoryTextBox.Dock = DockStyle.Fill;
        layout.Controls.Add(directoryTextBox, 1, 2);

        var browseButton = new Button
        {
            Text = "选择...",
            Dock = DockStyle.Fill
        };
        browseButton.Click += (_, _) => BrowseDirectory();
        layout.Controls.Add(browseButton, 2, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        var saveButton = new Button
        {
            Text = "保存",
            Width = 88,
            DialogResult = DialogResult.None
        };
        saveButton.Click += (_, _) => SaveSettings();

        var cancelButton = new Button
        {
            Text = "取消",
            Width = 88,
            DialogResult = DialogResult.Cancel
        };
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 4);
        layout.SetColumnSpan(buttonPanel, 3);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void HotkeyTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        if (!HotkeyGesture.TryFromKeyEvent(e, out HotkeyGesture gesture))
        {
            hotkeyTextBox.Text = "请按一个非修饰键";
            return;
        }

        candidateHotkey = gesture;
        hotkeyTextBox.Text = gesture.ToString();
    }

    private void BrowseDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择截图默认保存目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(directoryTextBox.Text) ? directoryTextBox.Text : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            directoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveSettings()
    {
        if (!tryApplyHotkey(candidateHotkey))
        {
            MessageBox.Show(this, "快捷键已被其他程序占用，已保留旧的有效快捷键。", "快捷键冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ResultSettings = new AppSettings
        {
            Hotkey = candidateHotkey,
            SaveDirectory = string.IsNullOrWhiteSpace(directoryTextBox.Text) ? null : directoryTextBox.Text.Trim(),
            ImageFormat = "png"
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
