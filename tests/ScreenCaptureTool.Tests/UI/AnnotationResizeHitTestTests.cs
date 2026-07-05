using System.Drawing;
using ScreenCaptureTool.Annotations;
using ScreenCaptureTool.UI;

namespace ScreenCaptureTool.Tests.UI;

public sealed class AnnotationResizeHitTestTests
{
    [Fact]
    public void TryHit_returns_text_part_when_pointer_is_on_note_text_right_handle()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        var point = new PointF(note.TextBounds.Right, note.TextBounds.Top + note.TextBounds.Height / 2f);

        AnnotationResizeHit hit = AnnotationResizeHitTest.TryHit(note, point);

        Assert.True(hit.HasHit);
        Assert.Equal(NotePart.Text, hit.NotePart);
        Assert.Equal(ResizeHandle.Right, hit.Handle);
        Assert.Equal(note.TextBounds, hit.Bounds);
    }

    [Fact]
    public void TryHit_returns_outer_part_when_pointer_is_on_note_outer_bottom_right_handle()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        var point = new PointF(note.Bounds.Right, note.Bounds.Bottom);

        AnnotationResizeHit hit = AnnotationResizeHitTest.TryHit(note, point);

        Assert.True(hit.HasHit);
        Assert.Equal(NotePart.Outer, hit.NotePart);
        Assert.Equal(ResizeHandle.BottomRight, hit.Handle);
        Assert.Equal(note.Bounds, hit.Bounds);
    }

    [Fact]
    public void TryHit_returns_regular_annotation_handle_for_non_note_annotations()
    {
        var rectangle = new RectangleAnnotation(new RectangleF(10, 20, 100, 80));

        AnnotationResizeHit hit = AnnotationResizeHitTest.TryHit(rectangle, new PointF(60, 20));

        Assert.True(hit.HasHit);
        Assert.Equal(NotePart.None, hit.NotePart);
        Assert.Equal(ResizeHandle.Top, hit.Handle);
        Assert.Equal(rectangle.Bounds, hit.Bounds);
    }
}
