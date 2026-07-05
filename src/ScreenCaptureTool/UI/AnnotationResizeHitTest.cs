using System.Drawing;
using ScreenCaptureTool.Annotations;

namespace ScreenCaptureTool.UI;

public readonly record struct AnnotationResizeHit(NotePart NotePart, ResizeHandle Handle, RectangleF Bounds)
{
    public bool HasHit => Handle != ResizeHandle.None;

    public static AnnotationResizeHit None => new(NotePart.None, ResizeHandle.None, RectangleF.Empty);
}

public static class AnnotationResizeHitTest
{
    public static AnnotationResizeHit TryHit(AnnotationElement element, PointF point)
    {
        if (element is NoteAnnotationGroup note)
        {
            AnnotationResizeHit targetHit = TryHitPart(note, NotePart.Target, point);
            if (targetHit.HasHit)
            {
                return targetHit;
            }

            AnnotationResizeHit textHit = TryHitPart(note, NotePart.Text, point);
            if (textHit.HasHit)
            {
                return textHit;
            }

            return TryHitPart(note, NotePart.Outer, point);
        }

        ResizeHandle handle = ResizeHandleGeometry.HitTest(element.Bounds, point);
        return handle == ResizeHandle.None
            ? AnnotationResizeHit.None
            : new AnnotationResizeHit(NotePart.None, handle, element.Bounds);
    }

    private static AnnotationResizeHit TryHitPart(NoteAnnotationGroup note, NotePart part, PointF point)
    {
        RectangleF bounds = note.GetPartBounds(part);
        ResizeHandle handle = ResizeHandleGeometry.HitTest(bounds, point);
        return handle == ResizeHandle.None
            ? AnnotationResizeHit.None
            : new AnnotationResizeHit(part, handle, bounds);
    }
}
