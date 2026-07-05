using System.Drawing;
using ScreenCaptureTool.UI;

namespace ScreenCaptureTool.Tests.UI;

public sealed class OverlaySelectionPreviewTests
{
    [Fact]
    public void FromState_shows_draft_selection_while_dragging_before_commit()
    {
        Rectangle draft = new(10, 20, 100, 50);

        OverlaySelectionPreview preview = OverlaySelectionPreview.FromState(
            hasCommittedSelection: false,
            isSelectingRegion: true,
            selection: draft);

        Assert.True(preview.ShouldDrawSelection);
        Assert.Equal(draft, preview.Selection);
    }

    [Fact]
    public void FromState_hides_selection_when_idle_before_commit()
    {
        OverlaySelectionPreview preview = OverlaySelectionPreview.FromState(
            hasCommittedSelection: false,
            isSelectingRegion: false,
            selection: Rectangle.Empty);

        Assert.False(preview.ShouldDrawSelection);
    }

    [Fact]
    public void FromState_shows_committed_selection_after_mouse_up()
    {
        Rectangle committed = new(30, 40, 120, 90);

        OverlaySelectionPreview preview = OverlaySelectionPreview.FromState(
            hasCommittedSelection: true,
            isSelectingRegion: false,
            selection: committed);

        Assert.True(preview.ShouldDrawSelection);
        Assert.Equal(committed, preview.Selection);
    }
}
