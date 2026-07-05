using System.Drawing;
using ScreenCaptureTool.Annotations;
using ScreenCaptureTool.UI;

namespace ScreenCaptureTool.Tests.UI;

public sealed class NoteTextEditSessionTests
{
    [Fact]
    public void TryBegin_returns_session_only_when_point_is_inside_text_part()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        PointF textPoint = new(note.TextBounds.Left + 4, note.TextBounds.Top + 4);

        NoteTextEditSession? session = NoteTextEditSession.TryBegin(note, textPoint);

        Assert.NotNull(session);
        Assert.Equal(note.TextBounds, session.Value.Bounds);
        Assert.Null(NoteTextEditSession.TryBegin(note, new PointF(110, 110)));
    }

    [Fact]
    public void Commit_updates_note_text()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100));
        NoteTextEditSession session = NoteTextEditSession.TryBegin(
            note,
            new PointF(note.TextBounds.Left + 4, note.TextBounds.Top + 4))!.Value;

        session.Commit("新的备注");

        Assert.Equal("新的备注", note.Text);
    }

    [Fact]
    public void Cancel_restores_original_note_text()
    {
        var note = new NoteAnnotationGroup(new RectangleF(100, 100, 200, 100))
        {
            Text = "原文"
        };
        NoteTextEditSession session = NoteTextEditSession.TryBegin(
            note,
            new PointF(note.TextBounds.Left + 4, note.TextBounds.Top + 4))!.Value;

        note.Text = "临时修改";
        session.Cancel();

        Assert.Equal("原文", note.Text);
    }
}
