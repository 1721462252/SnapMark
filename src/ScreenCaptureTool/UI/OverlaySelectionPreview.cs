using System.Drawing;

namespace ScreenCaptureTool.UI;

public readonly record struct OverlaySelectionPreview(bool ShouldDrawSelection, Rectangle Selection)
{
    public static OverlaySelectionPreview FromState(
        bool hasCommittedSelection,
        bool isSelectingRegion,
        Rectangle selection)
    {
        bool shouldDraw = (hasCommittedSelection || isSelectingRegion) && selection.Width > 0 && selection.Height > 0;
        return new OverlaySelectionPreview(shouldDraw, shouldDraw ? selection : Rectangle.Empty);
    }
}
