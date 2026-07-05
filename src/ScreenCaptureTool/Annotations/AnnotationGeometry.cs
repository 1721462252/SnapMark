using System.Drawing;

namespace ScreenCaptureTool.Annotations;

public static class AnnotationGeometry
{
    public static RectangleF Normalize(RectangleF rectangle)
    {
        float left = Math.Min(rectangle.Left, rectangle.Right);
        float top = Math.Min(rectangle.Top, rectangle.Bottom);
        float right = Math.Max(rectangle.Left, rectangle.Right);
        float bottom = Math.Max(rectangle.Top, rectangle.Bottom);
        return RectangleF.FromLTRB(left, top, right, bottom);
    }

    public static RectangleF Inflate(RectangleF rectangle, float amount)
    {
        RectangleF inflated = rectangle;
        inflated.Inflate(amount, amount);
        return inflated;
    }

    public static PointF Center(RectangleF rectangle)
    {
        return new PointF(rectangle.Left + rectangle.Width / 2f, rectangle.Top + rectangle.Height / 2f);
    }
}
