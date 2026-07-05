using System.Drawing;
using ScreenCaptureTool.Annotations;

namespace ScreenCaptureTool.UI;

public readonly record struct NoteTextEditSession(NoteAnnotationGroup Note, string OriginalText, RectangleF Bounds)
{
    public static NoteTextEditSession? TryBegin(NoteAnnotationGroup note, PointF point)
    {
        return note.HitTestPart(point) == NotePart.Text
            ? new NoteTextEditSession(note, note.Text, note.TextBounds)
            : null;
    }

    public void Commit(string text)
    {
        Note.Text = text;
    }

    public void Cancel()
    {
        Note.Text = OriginalText;
    }
}
