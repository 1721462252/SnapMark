using System.Drawing;
using ScreenCaptureTool.UI;

namespace ScreenCaptureTool.Tests.UI;

public sealed class SelectionBoundsEditorTests
{
    [Fact]
    public void Move_clamps_selection_to_surface_and_reports_actual_delta()
    {
        Rectangle selection = new(80, 70, 40, 30);

        SelectionMoveResult result = SelectionBoundsEditor.Move(selection, new Size(30, 50), new Size(120, 100));

        Assert.Equal(new Rectangle(80, 70, 40, 30), result.Selection);
        Assert.Equal(Size.Empty, result.ActualDelta);
    }

    [Fact]
    public void Move_reports_delta_used_to_keep_annotations_aligned()
    {
        Rectangle selection = new(20, 30, 40, 30);

        SelectionMoveResult result = SelectionBoundsEditor.Move(selection, new Size(15, -10), new Size(120, 100));

        Assert.Equal(new Rectangle(35, 20, 40, 30), result.Selection);
        Assert.Equal(new Size(15, -10), result.ActualDelta);
    }

    [Fact]
    public void Resize_from_right_handle_changes_width_without_changing_height()
    {
        Rectangle selection = new(20, 30, 40, 30);

        Rectangle resized = SelectionBoundsEditor.Resize(
            selection,
            ResizeHandle.Right,
            new Point(75, 45),
            new Size(120, 100));

        Assert.Equal(new Rectangle(20, 30, 55, 30), resized);
    }

    [Fact]
    public void Resize_from_top_left_handle_clamps_to_surface()
    {
        Rectangle selection = new(20, 30, 40, 30);

        Rectangle resized = SelectionBoundsEditor.Resize(
            selection,
            ResizeHandle.TopLeft,
            new Point(-10, -20),
            new Size(120, 100));

        Assert.Equal(new Rectangle(0, 0, 60, 60), resized);
    }
}
