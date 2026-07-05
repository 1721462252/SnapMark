using System.Drawing.Drawing2D;
using ScreenCaptureTool.Annotations;

namespace ScreenCaptureTool.UI;

public sealed class FloatingToolbar : Control
{
    private readonly Dictionary<AnnotationTool, ToolIconButton> toolButtons = [];
    private readonly ToolTip toolTip = new();

    public FloatingToolbar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(314, 48);
        Visible = false;

        AddToolButton(ToolbarButtonKind.Select, AnnotationTool.Select, "选择 / 移动");
        AddToolButton(ToolbarButtonKind.Rectangle, AnnotationTool.Rectangle, "矩形框");
        AddToolButton(ToolbarButtonKind.Ellipse, AnnotationTool.Ellipse, "圆形 / 椭圆框");
        AddToolButton(ToolbarButtonKind.Pen, AnnotationTool.Pen, "画笔");
        AddToolButton(ToolbarButtonKind.Note, AnnotationTool.Note, "备注");
        AddCommandButton(ToolbarButtonKind.Copy, "复制到剪贴板", () => CopyClicked?.Invoke(this, EventArgs.Empty));
        AddCommandButton(ToolbarButtonKind.Save, "保存到目录", () => SaveClicked?.Invoke(this, EventArgs.Empty));
        AddCommandButton(ToolbarButtonKind.Cancel, "取消截图", () => CancelClicked?.Invoke(this, EventArgs.Empty));
        SetActiveTool(AnnotationTool.Select);
    }

    public event EventHandler<AnnotationTool>? ToolSelected;

    public event EventHandler? CopyClicked;

    public event EventHandler? SaveClicked;

    public event EventHandler? CancelClicked;

    public void SetActiveTool(AnnotationTool tool)
    {
        foreach (KeyValuePair<AnnotationTool, ToolIconButton> pair in toolButtons)
        {
            pair.Value.Active = pair.Key == tool;
            pair.Value.Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;
        bounds.Inflate(-1, -1);
        using GraphicsPath path = Rounded(bounds, 12);
        using var brush = new SolidBrush(UiColors.ToolbarBack);
        using var pen = new Pen(UiColors.ToolbarBorder, 1f);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AddToolButton(ToolbarButtonKind kind, AnnotationTool tool, string tip)
    {
        ToolIconButton button = CreateButton(kind, tip);
        button.Click += (_, _) =>
        {
            SetActiveTool(tool);
            ToolSelected?.Invoke(this, tool);
        };
        toolButtons[tool] = button;
    }

    private void AddCommandButton(ToolbarButtonKind kind, string tip, Action onClick)
    {
        ToolIconButton button = CreateButton(kind, tip);
        button.Click += (_, _) => onClick();
    }

    private ToolIconButton CreateButton(ToolbarButtonKind kind, string tip)
    {
        int left = 10 + Controls.Count * 38;
        var button = new ToolIconButton(kind)
        {
            Location = new Point(left, 7)
        };
        toolTip.SetToolTip(button, tip);
        Controls.Add(button);
        return button;
    }

    private static GraphicsPath Rounded(Rectangle rectangle, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
