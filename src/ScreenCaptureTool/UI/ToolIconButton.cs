using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ScreenCaptureTool.UI;

public enum ToolbarButtonKind
{
    Select,
    Rectangle,
    Ellipse,
    Pen,
    Note,
    Copy,
    Save,
    Cancel
}

public sealed class ToolIconButton : Control
{
    private bool hovered;

    public ToolIconButton(ToolbarButtonKind kind)
    {
        Kind = kind;
        Size = new Size(36, 34);
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    public ToolbarButtonKind Kind { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Active { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;
        bounds.Inflate(-2, -2);

        using GraphicsPath background = Rounded(bounds, 8);
        using var fill = new SolidBrush(Active ? UiColors.ButtonActive : hovered ? UiColors.ButtonHover : Color.Transparent);
        e.Graphics.FillPath(fill, background);

        using var pen = new Pen(Kind is ToolbarButtonKind.Cancel ? Color.FromArgb(255, 255, 98, 98) : UiColors.Icon, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        DrawIcon(e.Graphics, pen, bounds);
    }

    private void DrawIcon(Graphics graphics, Pen pen, Rectangle bounds)
    {
        Rectangle icon = bounds;
        icon.Inflate(-8, -7);

        switch (Kind)
        {
            case ToolbarButtonKind.Select:
                graphics.DrawLine(pen, icon.Left, icon.Top, icon.Left + 4, icon.Bottom);
                graphics.DrawLine(pen, icon.Left, icon.Top, icon.Right, icon.Top + 9);
                graphics.DrawLine(pen, icon.Left + 4, icon.Bottom, icon.Left + 8, icon.Bottom - 7);
                graphics.DrawLine(pen, icon.Right, icon.Top + 9, icon.Left + 8, icon.Bottom - 7);
                break;
            case ToolbarButtonKind.Rectangle:
                graphics.DrawRectangle(pen, icon);
                break;
            case ToolbarButtonKind.Ellipse:
                graphics.DrawEllipse(pen, icon);
                break;
            case ToolbarButtonKind.Pen:
                graphics.DrawBezier(pen, icon.Left, icon.Bottom - 3, icon.Left + 5, icon.Top, icon.Right - 5, icon.Bottom, icon.Right, icon.Top + 3);
                break;
            case ToolbarButtonKind.Note:
                graphics.DrawRectangle(pen, icon.Left, icon.Top, icon.Width - 4, icon.Height - 8);
                graphics.DrawLine(pen, icon.Left + 6, icon.Bottom - 8, icon.Right, icon.Bottom);
                graphics.DrawLine(pen, icon.Right, icon.Bottom, icon.Right - 5, icon.Bottom - 8);
                break;
            case ToolbarButtonKind.Copy:
                graphics.DrawRectangle(pen, icon.Left + 5, icon.Top, icon.Width - 5, icon.Height - 5);
                graphics.DrawRectangle(pen, icon.Left, icon.Top + 5, icon.Width - 5, icon.Height - 5);
                break;
            case ToolbarButtonKind.Save:
                graphics.DrawRectangle(pen, icon);
                graphics.DrawLine(pen, icon.Left + 4, icon.Top, icon.Right - 4, icon.Top);
                graphics.DrawLine(pen, icon.Left + 5, icon.Bottom - 5, icon.Right - 5, icon.Bottom - 5);
                graphics.DrawLine(pen, icon.Right - 5, icon.Top, icon.Right - 5, icon.Top + 7);
                break;
            case ToolbarButtonKind.Cancel:
                graphics.DrawLine(pen, icon.Left, icon.Top, icon.Right, icon.Bottom);
                graphics.DrawLine(pen, icon.Right, icon.Top, icon.Left, icon.Bottom);
                break;
        }
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
