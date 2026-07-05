using System.Drawing;

namespace ScreenCaptureTool.Annotations;

public sealed class RectangleAnnotation : AnnotationElement
{
    private RectangleF bounds;

    public RectangleAnnotation(RectangleF bounds)
    {
        this.bounds = AnnotationGeometry.Normalize(bounds);
    }

    public override RectangleF Bounds => bounds;

    public override void Draw(Graphics graphics)
    {
        using var pen = new Pen(StrokeColor, StrokeWidth);
        graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    public override bool HitTest(PointF point)
    {
        return IsNearRectangleEdge(bounds, point, HitSlop);
    }

    public override void Move(SizeF delta)
    {
        bounds.Offset(delta.Width, delta.Height);
    }

    public override void Resize(RectangleF bounds)
    {
        this.bounds = AnnotationGeometry.Normalize(bounds);
    }
}
