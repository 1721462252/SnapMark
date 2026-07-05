using System.Drawing;

namespace ScreenCaptureTool.Annotations;

public sealed class AnnotationCanvas
{
    private readonly List<AnnotationElement> elements = [];

    public IReadOnlyList<AnnotationElement> Elements => elements;

    public AnnotationElement? SelectedElement { get; private set; }

    public void Add(AnnotationElement element)
    {
        elements.Add(element);
        SelectedElement = element;
    }

    public void ClearSelection()
    {
        SelectedElement = null;
    }

    public AnnotationElement? SelectAt(PointF point)
    {
        for (int index = elements.Count - 1; index >= 0; index--)
        {
            AnnotationElement element = elements[index];
            if (element.HitTest(point))
            {
                SelectedElement = element;
                return element;
            }
        }

        SelectedElement = null;
        return null;
    }

    public void MoveSelected(SizeF delta)
    {
        SelectedElement?.Move(delta);
    }

    public void MoveAll(SizeF delta)
    {
        foreach (AnnotationElement element in elements)
        {
            element.Move(delta);
        }
    }

    public void ResizeSelected(RectangleF bounds)
    {
        SelectedElement?.Resize(bounds);
    }

    public void Draw(Graphics graphics)
    {
        foreach (AnnotationElement element in elements)
        {
            element.Draw(graphics);
        }

        if (SelectedElement is not null)
        {
            DrawSelection(graphics, SelectedElement.Bounds);
        }
    }

    private static void DrawSelection(Graphics graphics, RectangleF bounds)
    {
        using var pen = new Pen(Color.FromArgb(220, Color.White), 1f)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
        };
        graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
}
