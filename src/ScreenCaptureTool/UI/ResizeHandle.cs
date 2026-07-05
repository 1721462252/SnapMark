using System.Drawing;

namespace ScreenCaptureTool.UI;

public enum ResizeHandle
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

public static class ResizeHandleGeometry
{
    public const float DefaultHandleSize = 10f;

    public static ResizeHandle HitTest(RectangleF bounds, PointF point, float handleSize = DefaultHandleSize)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return ResizeHandle.None;
        }

        foreach ((ResizeHandle handle, RectangleF handleBounds) in GetHandleBounds(bounds, handleSize))
        {
            if (handleBounds.Contains(point))
            {
                return handle;
            }
        }

        return ResizeHandle.None;
    }

    public static IEnumerable<(ResizeHandle Handle, RectangleF Bounds)> GetHandleBounds(
        RectangleF bounds,
        float handleSize = DefaultHandleSize)
    {
        float centerX = bounds.Left + bounds.Width / 2f;
        float centerY = bounds.Top + bounds.Height / 2f;

        yield return (ResizeHandle.TopLeft, CreateHandle(bounds.Left, bounds.Top, handleSize));
        yield return (ResizeHandle.TopRight, CreateHandle(bounds.Right, bounds.Top, handleSize));
        yield return (ResizeHandle.BottomRight, CreateHandle(bounds.Right, bounds.Bottom, handleSize));
        yield return (ResizeHandle.BottomLeft, CreateHandle(bounds.Left, bounds.Bottom, handleSize));
        yield return (ResizeHandle.Top, CreateHandle(centerX, bounds.Top, handleSize));
        yield return (ResizeHandle.Right, CreateHandle(bounds.Right, centerY, handleSize));
        yield return (ResizeHandle.Bottom, CreateHandle(centerX, bounds.Bottom, handleSize));
        yield return (ResizeHandle.Left, CreateHandle(bounds.Left, centerY, handleSize));
    }

    public static RectangleF Resize(
        RectangleF bounds,
        ResizeHandle handle,
        PointF point,
        float minWidth = 8f,
        float minHeight = 8f,
        RectangleF? limit = null)
    {
        if (handle == ResizeHandle.None)
        {
            return bounds;
        }

        if (limit is RectangleF limitBounds)
        {
            point = new PointF(
                Math.Clamp(point.X, limitBounds.Left, limitBounds.Right),
                Math.Clamp(point.Y, limitBounds.Top, limitBounds.Bottom));
        }

        float left = bounds.Left;
        float top = bounds.Top;
        float right = bounds.Right;
        float bottom = bounds.Bottom;

        if (MovesLeft(handle))
        {
            left = Math.Min(point.X, right - minWidth);
        }
        else if (MovesRight(handle))
        {
            right = Math.Max(point.X, left + minWidth);
        }

        if (MovesTop(handle))
        {
            top = Math.Min(point.Y, bottom - minHeight);
        }
        else if (MovesBottom(handle))
        {
            bottom = Math.Max(point.Y, top + minHeight);
        }

        return RectangleF.FromLTRB(left, top, right, bottom);
    }

    private static RectangleF CreateHandle(float centerX, float centerY, float size)
    {
        float half = size / 2f;
        return new RectangleF(centerX - half, centerY - half, size, size);
    }

    private static bool MovesLeft(ResizeHandle handle)
    {
        return handle is ResizeHandle.Left or ResizeHandle.TopLeft or ResizeHandle.BottomLeft;
    }

    private static bool MovesRight(ResizeHandle handle)
    {
        return handle is ResizeHandle.Right or ResizeHandle.TopRight or ResizeHandle.BottomRight;
    }

    private static bool MovesTop(ResizeHandle handle)
    {
        return handle is ResizeHandle.Top or ResizeHandle.TopLeft or ResizeHandle.TopRight;
    }

    private static bool MovesBottom(ResizeHandle handle)
    {
        return handle is ResizeHandle.Bottom or ResizeHandle.BottomLeft or ResizeHandle.BottomRight;
    }
}
