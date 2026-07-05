using System.Drawing;

namespace ScreenCaptureTool.Annotations;

public sealed class FreehandAnnotation : AnnotationElement
{
    private readonly List<PointF> points;

    public FreehandAnnotation(IEnumerable<PointF> points)
    {
        this.points = points.ToList();
    }

    public IReadOnlyList<PointF> Points => points;

    public override RectangleF Bounds
    {
        get
        {
            if (points.Count == 0)
            {
                return RectangleF.Empty;
            }

            float minX = points.Min(point => point.X);
            float minY = points.Min(point => point.Y);
            float maxX = points.Max(point => point.X);
            float maxY = points.Max(point => point.Y);
            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }
    }

    public void AddPoint(PointF point)
    {
        points.Add(point);
    }

    public override void Draw(Graphics graphics)
    {
        if (points.Count < 2)
        {
            return;
        }

        using var pen = new Pen(StrokeColor, StrokeWidth)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        graphics.DrawLines(pen, points.ToArray());
    }

    public override bool HitTest(PointF point)
    {
        for (int index = 1; index < points.Count; index++)
        {
            if (DistanceToSegment(point, points[index - 1], points[index]) <= HitSlop)
            {
                return true;
            }
        }

        return false;
    }

    public override void Move(SizeF delta)
    {
        for (int index = 0; index < points.Count; index++)
        {
            PointF point = points[index];
            points[index] = new PointF(point.X + delta.Width, point.Y + delta.Height);
        }
    }

    public override void Resize(RectangleF bounds)
    {
        RectangleF oldBounds = Bounds;
        RectangleF newBounds = AnnotationGeometry.Normalize(bounds);
        if (oldBounds.Width <= 0 || oldBounds.Height <= 0)
        {
            return;
        }

        float scaleX = newBounds.Width / oldBounds.Width;
        float scaleY = newBounds.Height / oldBounds.Height;
        for (int index = 0; index < points.Count; index++)
        {
            PointF point = points[index];
            points[index] = new PointF(
                newBounds.Left + (point.X - oldBounds.Left) * scaleX,
                newBounds.Top + (point.Y - oldBounds.Top) * scaleY);
        }
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
