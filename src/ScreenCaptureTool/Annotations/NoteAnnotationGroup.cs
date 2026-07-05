using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScreenCaptureTool.Annotations;

public enum NotePart
{
    None,
    Outer,
    Target,
    Arrow,
    Text
}

public sealed class NoteAnnotationGroup : AnnotationElement
{
    private RectangleF bounds;
    private RectangleF targetBounds;
    private RectangleF textBounds;

    public NoteAnnotationGroup(RectangleF bounds)
    {
        this.bounds = AnnotationGeometry.Normalize(bounds);
        ResetPartsForBounds(this.bounds);
    }

    public override RectangleF Bounds => bounds;

    public RectangleF TargetBounds => targetBounds;

    public RectangleF TextBounds => textBounds;

    public string Text { get; set; } = "备注";

    public PointF ArrowStart => new(targetBounds.Right, targetBounds.Top + targetBounds.Height / 2f);

    public PointF ArrowEnd => new(textBounds.Left, textBounds.Top + textBounds.Height / 2f);

    public override void Draw(Graphics graphics)
    {
        using var targetPen = new Pen(StrokeColor, StrokeWidth);
        using var outerPen = new Pen(Color.FromArgb(180, Color.White), 1.5f)
        {
            DashStyle = DashStyle.Dash
        };
        using var arrowPen = new Pen(StrokeColor, StrokeWidth)
        {
            EndCap = LineCap.ArrowAnchor
        };
        using var textBrush = new SolidBrush(Color.FromArgb(230, 20, 20, 20));
        using var textPen = new Pen(StrokeColor, 2f);
        using var font = new Font("Segoe UI", 10f, FontStyle.Regular);
        using var fontBrush = new SolidBrush(Color.White);

        graphics.FillRectangle(textBrush, textBounds);
        graphics.DrawRectangle(outerPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        graphics.DrawRectangle(targetPen, targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height);
        graphics.DrawRectangle(textPen, textBounds.X, textBounds.Y, textBounds.Width, textBounds.Height);
        graphics.DrawLine(arrowPen, ArrowStart, ArrowEnd);
        graphics.DrawString(Text, font, fontBrush, textBounds);
    }

    public override bool HitTest(PointF point)
    {
        return HitTestPart(point) != NotePart.None;
    }

    public NotePart HitTestPart(PointF point)
    {
        if (targetBounds.Contains(point) || IsNearRectangleEdge(targetBounds, point, HitSlop))
        {
            return NotePart.Target;
        }

        if (textBounds.Contains(point) || IsNearRectangleEdge(textBounds, point, HitSlop))
        {
            return NotePart.Text;
        }

        if (DistanceToSegment(point, ArrowStart, ArrowEnd) <= HitSlop)
        {
            return NotePart.Arrow;
        }

        if (IsNearRectangleEdge(bounds, point, HitSlop))
        {
            return NotePart.Outer;
        }

        return NotePart.None;
    }

    public override void Move(SizeF delta)
    {
        bounds.Offset(delta.Width, delta.Height);
        targetBounds.Offset(delta.Width, delta.Height);
        textBounds.Offset(delta.Width, delta.Height);
    }

    public void MovePart(NotePart part, SizeF delta)
    {
        switch (part)
        {
            case NotePart.Target:
                targetBounds.Offset(delta.Width, delta.Height);
                break;
            case NotePart.Text:
                textBounds.Offset(delta.Width, delta.Height);
                break;
            case NotePart.Outer:
                Move(delta);
                break;
        }
    }

    public RectangleF GetPartBounds(NotePart part)
    {
        return part switch
        {
            NotePart.Target => targetBounds,
            NotePart.Text => textBounds,
            NotePart.Outer => bounds,
            _ => RectangleF.Empty
        };
    }

    public void ResizePart(NotePart part, RectangleF bounds)
    {
        RectangleF normalized = AnnotationGeometry.Normalize(bounds);
        switch (part)
        {
            case NotePart.Target:
                targetBounds = normalized;
                break;
            case NotePart.Text:
                textBounds = normalized;
                break;
            case NotePart.Outer:
                Resize(normalized);
                break;
        }
    }

    public override void Resize(RectangleF bounds)
    {
        RectangleF newBounds = AnnotationGeometry.Normalize(bounds);
        if (this.bounds.Width <= 0 || this.bounds.Height <= 0)
        {
            this.bounds = newBounds;
            ResetPartsForBounds(this.bounds);
            return;
        }

        targetBounds = ScaleChild(targetBounds, this.bounds, newBounds);
        textBounds = ScaleChild(textBounds, this.bounds, newBounds);
        this.bounds = newBounds;
    }

    private void ResetPartsForBounds(RectangleF outerBounds)
    {
        targetBounds = new RectangleF(
            outerBounds.Left,
            outerBounds.Top,
            outerBounds.Width * 0.45f,
            outerBounds.Height * 0.45f);
        textBounds = new RectangleF(
            outerBounds.Left + outerBounds.Width * 0.58f,
            outerBounds.Top + outerBounds.Height * 0.52f,
            outerBounds.Width * 0.40f,
            outerBounds.Height * 0.35f);
    }

    private static RectangleF ScaleChild(RectangleF child, RectangleF oldParent, RectangleF newParent)
    {
        float scaleX = newParent.Width / oldParent.Width;
        float scaleY = newParent.Height / oldParent.Height;
        return new RectangleF(
            newParent.Left + (child.Left - oldParent.Left) * scaleX,
            newParent.Top + (child.Top - oldParent.Top) * scaleY,
            child.Width * scaleX,
            child.Height * scaleY);
    }

    private static float DistanceToSegment(PointF point, PointF start, PointF end)
    {
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        if (dx == 0 && dy == 0)
        {
            return Distance(point, start);
        }

        float t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0f, 1f);
        var projection = new PointF(start.X + t * dx, start.Y + t * dy);
        return Distance(point, projection);
    }

    private static float Distance(PointF first, PointF second)
    {
        float dx = first.X - second.X;
        float dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
