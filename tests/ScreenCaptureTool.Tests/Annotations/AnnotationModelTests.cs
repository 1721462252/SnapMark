using System.Drawing;
using ScreenCaptureTool.Annotations;

namespace ScreenCaptureTool.Tests.Annotations;

public sealed class AnnotationModelTests
{
    [Fact]
    public void RectangleAnnotation_normalizes_dragged_bounds()
    {
        var annotation = new RectangleAnnotation(RectangleF.FromLTRB(100, 80, 20, 10));

        Assert.Equal(new RectangleF(20, 10, 80, 70), annotation.Bounds);
    }

    [Fact]
    public void EllipseAnnotation_hit_tests_inside_ellipse()
    {
        var annotation = new EllipseAnnotation(new RectangleF(10, 20, 100, 60));

        Assert.True(annotation.HitTest(new PointF(60, 50)));
        Assert.False(annotation.HitTest(new PointF(10, 20)));
    }

    [Fact]
    public void Annotation_move_offsets_bounds()
    {
        var annotation = new RectangleAnnotation(new RectangleF(10, 20, 40, 30));

        annotation.Move(new SizeF(5, -10));

        Assert.Equal(new RectangleF(15, 10, 40, 30), annotation.Bounds);
    }

    [Fact]
    public void Annotation_resize_replaces_bounds_with_normalized_rectangle()
    {
        var annotation = new EllipseAnnotation(new RectangleF(10, 20, 40, 30));

        annotation.Resize(RectangleF.FromLTRB(80, 90, 20, 10));

        Assert.Equal(new RectangleF(20, 10, 60, 80), annotation.Bounds);
    }

    [Fact]
    public void FreehandAnnotation_tracks_bounds_and_moves_points()
    {
        var annotation = new FreehandAnnotation(
        [
            new PointF(10, 20),
            new PointF(30, 25),
            new PointF(20, 60)
        ]);

        annotation.Move(new SizeF(5, -10));

        Assert.Equal(new RectangleF(15, 10, 20, 40), annotation.Bounds);
        Assert.Contains(new PointF(35, 15), annotation.Points);
    }

    [Fact]
    public void AnnotationCanvas_selects_topmost_hit_element()
    {
        var canvas = new AnnotationCanvas();
        var bottom = new RectangleAnnotation(new RectangleF(10, 10, 100, 100));
        var top = new EllipseAnnotation(new RectangleF(20, 20, 100, 100));
        canvas.Add(bottom);
        canvas.Add(top);

        AnnotationElement? hit = canvas.SelectAt(new PointF(50, 50));

        Assert.Same(top, hit);
        Assert.Same(top, canvas.SelectedElement);
    }

    [Fact]
    public void AnnotationCanvas_move_all_offsets_every_annotation()
    {
        var canvas = new AnnotationCanvas();
        var first = new RectangleAnnotation(new RectangleF(10, 20, 30, 40));
        var second = new EllipseAnnotation(new RectangleF(50, 60, 70, 80));
        canvas.Add(first);
        canvas.Add(second);

        canvas.MoveAll(new SizeF(5, -10));

        Assert.Equal(new RectangleF(15, 10, 30, 40), first.Bounds);
        Assert.Equal(new RectangleF(55, 50, 70, 80), second.Bounds);
    }
}
