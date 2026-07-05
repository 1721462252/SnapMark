using System.Drawing;
using ScreenCaptureTool.UI;

namespace ScreenCaptureTool.Tests.UI;

public sealed class ResizeHandleGeometryTests
{
    [Fact]
    public void HitTest_returns_left_handle_for_left_edge_midpoint()
    {
        RectangleF bounds = new(100, 80, 200, 120);

        ResizeHandle handle = ResizeHandleGeometry.HitTest(bounds, new PointF(100, 140));

        Assert.Equal(ResizeHandle.Left, handle);
    }

    [Fact]
    public void Resize_from_left_handle_changes_width_without_changing_height()
    {
        RectangleF bounds = new(100, 80, 200, 120);

        RectangleF resized = ResizeHandleGeometry.Resize(bounds, ResizeHandle.Left, new PointF(70, 140));

        Assert.Equal(new RectangleF(70, 80, 230, 120), resized);
    }

    [Fact]
    public void Resize_from_top_handle_changes_height_without_changing_width()
    {
        RectangleF bounds = new(100, 80, 200, 120);

        RectangleF resized = ResizeHandleGeometry.Resize(bounds, ResizeHandle.Top, new PointF(150, 50));

        Assert.Equal(new RectangleF(100, 50, 200, 150), resized);
    }

    [Fact]
    public void Resize_clamps_to_minimum_size_without_flipping_bounds()
    {
        RectangleF bounds = new(100, 80, 200, 120);

        RectangleF resized = ResizeHandleGeometry.Resize(
            bounds,
            ResizeHandle.Left,
            new PointF(500, 140),
            minWidth: 12,
            minHeight: 12);

        Assert.Equal(new RectangleF(288, 80, 12, 120), resized);
    }
}
