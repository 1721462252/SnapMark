using System.Drawing;
using ScreenCaptureTool.Annotations;

namespace ScreenCaptureTool.Tests.Annotations;

public sealed class NoteAnnotationGroupTests
{
    [Fact]
    public void Create_aligns_target_rectangle_to_outer_top_left()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 80, 200, 120));

        Assert.Equal(note.Bounds.Location, note.TargetBounds.Location);
        Assert.Equal(90, note.TargetBounds.Width);
        Assert.Equal(54, note.TargetBounds.Height);
    }

    [Fact]
    public void Move_offsets_all_note_parts()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 80, 200, 120));
        RectangleF oldTarget = note.TargetBounds;
        RectangleF oldText = note.TextBounds;
        PointF oldArrowStart = note.ArrowStart;

        note.Move(new SizeF(20, 10));

        Assert.Equal(120, note.Bounds.X);
        Assert.Equal(90, note.Bounds.Y);
        Assert.Equal(oldTarget.X + 20, note.TargetBounds.X);
        Assert.Equal(oldText.Y + 10, note.TextBounds.Y);
        Assert.Equal(oldArrowStart.X + 20, note.ArrowStart.X);
    }

    [Fact]
    public void Resize_scales_children_proportionally()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));

        note.Resize(new RectangleF(100, 100, 400, 200));

        Assert.Equal(new RectangleF(100, 100, 180, 90), note.TargetBounds);
        Assert.True(note.TextBounds.Width > 150);
        Assert.True(note.TextBounds.Height > 50);
    }

    [Fact]
    public void Resize_from_zero_sized_draft_initializes_all_note_parts()
    {
        var note = new NoteAnnotationGroup(RectangleF.FromLTRB(100, 100, 100, 100));

        note.Resize(new RectangleF(100, 100, 200, 120));

        Assert.Equal(new RectangleF(100, 100, 90, 54), note.TargetBounds);
        Assert.True(note.TextBounds.Width > 0);
        Assert.True(note.TextBounds.Height > 0);
        Assert.NotEqual(note.ArrowStart, note.ArrowEnd);
        Assert.Equal(NotePart.Target, note.HitTestPart(new PointF(110, 110)));
        Assert.Equal(NotePart.Text, note.HitTestPart(new PointF(note.TextBounds.Left + 4, note.TextBounds.Top + 4)));
    }

    [Fact]
    public void Moving_target_reconnects_arrow()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        PointF oldArrowStart = note.ArrowStart;
        PointF oldArrowEnd = note.ArrowEnd;

        note.MovePart(NotePart.Target, new SizeF(20, 0));

        Assert.Equal(oldArrowStart.X + 20, note.ArrowStart.X);
        Assert.Equal(oldArrowEnd, note.ArrowEnd);
    }

    [Fact]
    public void Moving_text_reconnects_arrow_end()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        PointF oldArrowStart = note.ArrowStart;
        PointF oldArrowEnd = note.ArrowEnd;

        note.MovePart(NotePart.Text, new SizeF(30, -10));

        Assert.Equal(oldArrowStart, note.ArrowStart);
        Assert.Equal(oldArrowEnd.X + 30, note.ArrowEnd.X);
        Assert.Equal(oldArrowEnd.Y - 10, note.ArrowEnd.Y);
    }

    [Fact]
    public void ResizePart_resizes_target_without_resizing_text()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        RectangleF oldText = note.TextBounds;

        note.ResizePart(NotePart.Target, new RectangleF(100, 100, 120, 80));

        Assert.Equal(new RectangleF(100, 100, 120, 80), note.TargetBounds);
        Assert.Equal(oldText, note.TextBounds);
        Assert.Equal(new PointF(220, 140), note.ArrowStart);
    }

    [Fact]
    public void ResizePart_resizes_text_without_resizing_target()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        RectangleF oldTarget = note.TargetBounds;

        note.ResizePart(NotePart.Text, new RectangleF(210, 150, 110, 70));

        Assert.Equal(oldTarget, note.TargetBounds);
        Assert.Equal(new RectangleF(210, 150, 110, 70), note.TextBounds);
        Assert.Equal(new PointF(210, 185), note.ArrowEnd);
    }

    [Fact]
    public void HitTestPart_returns_arrow_for_point_near_connector()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        var midpoint = new PointF(
            (note.ArrowStart.X + note.ArrowEnd.X) / 2f,
            (note.ArrowStart.Y + note.ArrowEnd.Y) / 2f);

        Assert.Equal(NotePart.Arrow, note.HitTestPart(midpoint));
    }

    [Fact]
    public void HitTestPart_returns_outer_only_near_outer_boundary()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));

        Assert.Equal(NotePart.Outer, note.HitTestPart(new PointF(100, 190)));
        Assert.Equal(NotePart.None, note.HitTestPart(new PointF(150, 180)));
    }
}
