using System.Drawing;

namespace ScreenCaptureTool.Annotations;

public abstract class AnnotationElement
{
    protected const float HitSlop = 6f;

    protected AnnotationElement(Color? strokeColor = null, float strokeWidth = 3f)
    {
        StrokeColor = strokeColor ?? Color.FromArgb(255, 60, 74);
        StrokeWidth = strokeWidth;
    }

    public Color StrokeColor { get; set; }

    public float StrokeWidth { get; set; }

    public abstract RectangleF Bounds { get; }

    public abstract void Draw(Graphics graphics);

    public abstract bool HitTest(PointF point);

    public abstract void Move(SizeF delta);

    public abstract void Resize(RectangleF bounds);

    protected static bool IsNearRectangleEdge(RectangleF rectangle, PointF point, float slop)
    {
        RectangleF outer = AnnotationGeometry.Inflate(rectangle, slop);
        RectangleF inner = AnnotationGeometry.Inflate(rectangle, -slop);
        return outer.Contains(point) && !inner.Contains(point);
    }
}
