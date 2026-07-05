using System.Drawing;

namespace ScreenCaptureTool.Annotations;

public sealed class EllipseAnnotation : AnnotationElement
{
    private RectangleF bounds;

    public EllipseAnnotation(RectangleF bounds)
    {
        this.bounds = AnnotationGeometry.Normalize(bounds);
    }

    public override RectangleF Bounds => bounds;

    public override void Draw(Graphics graphics)
    {
        using var pen = new Pen(StrokeColor, StrokeWidth);
        graphics.DrawEllipse(pen, bounds);
    }

    public override bool HitTest(PointF point)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerX = bounds.Left + radiusX;
        float centerY = bounds.Top + radiusY;
        float normalizedX = (point.X - centerX) / radiusX;
        float normalizedY = (point.Y - centerY) / radiusY;
        return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
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
