using System.Drawing;

namespace ScreenCaptureTool.UI;

public readonly record struct SelectionMoveResult(Rectangle Selection, Size ActualDelta);

public static class SelectionBoundsEditor
{
    public const int MinimumSelectionSize = 5;

    public static SelectionMoveResult Move(Rectangle selection, Size requestedDelta, Size surfaceSize)
    {
        int maxX = Math.Max(0, surfaceSize.Width - selection.Width);
        int maxY = Math.Max(0, surfaceSize.Height - selection.Height);
        int requestedX = selection.X + requestedDelta.Width;
        int requestedY = selection.Y + requestedDelta.Height;
        int x = Math.Clamp(requestedX, 0, maxX);
        int y = Math.Clamp(requestedY, 0, maxY);

        var moved = new Rectangle(x, y, selection.Width, selection.Height);
        return new SelectionMoveResult(moved, new Size(x - selection.X, y - selection.Y));
    }

    public static Rectangle Resize(
        Rectangle selection,
        ResizeHandle handle,
        Point point,
        Size surfaceSize,
        int minimumSelectionSize = MinimumSelectionSize)
    {
        RectangleF bounds = new(selection.X, selection.Y, selection.Width, selection.Height);
        RectangleF limit = new(0, 0, surfaceSize.Width, surfaceSize.Height);
        RectangleF resized = ResizeHandleGeometry.Resize(
            bounds,
            handle,
            point,
            minimumSelectionSize,
            minimumSelectionSize,
            limit);

        return Rectangle.Round(resized);
    }
}
