using System.Drawing.Drawing2D;
using ScreenCaptureTool.Annotations;
using ScreenCaptureTool.Core;
using ScreenCaptureTool.Services;

namespace ScreenCaptureTool.UI;

public sealed class OverlayForm : Form
{
    private readonly CaptureResult capture;
    private readonly AppSettings settings;
    private readonly SettingsStore settingsStore;
    private readonly ExportService exportService;
    private readonly AnnotationCanvas canvas = new();
    private readonly FloatingToolbar toolbar = new();
    private Rectangle selection;
    private bool hasSelection;
    private Point dragStart;
    private Rectangle selectionBeforeDrag;
    private PointF lastPoint;
    private RectangleF resizeStartBounds;
    private ResizeHandle activeResizeHandle = ResizeHandle.None;
    private NotePart resizingNotePart = NotePart.None;
    private OverlayInteraction interaction = OverlayInteraction.None;
    private AnnotationTool currentTool = AnnotationTool.Select;
    private AnnotationElement? activeElement;
    private FreehandAnnotation? activeStroke;
    private NotePart selectedNotePart = NotePart.None;
    private TextBox? inlineTextEditor;
    private NoteTextEditSession? activeTextEdit;
    private bool closingInlineTextEditor;

    public OverlayForm(CaptureResult capture, AppSettings settings, SettingsStore settingsStore, ExportService exportService)
    {
        this.capture = capture;
        this.settings = settings;
        this.settingsStore = settingsStore;
        this.exportService = exportService;

        InitializeComponent();
    }

    protected override bool ShowWithoutActivation => false;

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.None;
        Bounds = capture.VirtualBounds;
        ClientSize = capture.VirtualBounds.Size;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;

        toolbar.ToolSelected += (_, tool) =>
        {
            currentTool = tool;
            Cursor = tool == AnnotationTool.Select ? Cursors.Default : Cursors.Cross;
        };
        toolbar.CopyClicked += (_, _) => CopySelection();
        toolbar.SaveClicked += (_, _) => SaveSelection();
        toolbar.CancelClicked += (_, _) => CancelCapture();
        Controls.Add(toolbar);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Focus();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        inlineTextEditor?.Dispose();
        capture.Dispose();
        base.OnFormClosed(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawImageUnscaled(capture.DesktopBitmap, Point.Empty);

        OverlaySelectionPreview preview = OverlaySelectionPreview.FromState(
            hasSelection,
            interaction == OverlayInteraction.SelectingRegion,
            selection);

        if (!preview.ShouldDrawSelection)
        {
            using var shade = new SolidBrush(Color.FromArgb(100, Color.Black));
            e.Graphics.FillRectangle(shade, ClientRectangle);
            return;
        }

        DrawShade(e.Graphics, preview.Selection);
        DrawSelectionFrame(e.Graphics, preview.Selection);

        if (!hasSelection || IsSelectionAdjustmentInProgress())
        {
            return;
        }

        GraphicsState state = e.Graphics.Save();
        e.Graphics.SetClip(selection);
        canvas.Draw(e.Graphics);
        DrawSelectedResizeHandles(e.Graphics);
        e.Graphics.Restore(state);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (inlineTextEditor is not null && !inlineTextEditor.Bounds.Contains(e.Location))
        {
            CommitInlineTextEdit();
        }

        if (e.Clicks > 1)
        {
            interaction = OverlayInteraction.None;
            return;
        }

        if (!hasSelection)
        {
            interaction = OverlayInteraction.SelectingRegion;
            dragStart = e.Location;
            selection = new Rectangle(e.Location, Size.Empty);
            Invalidate();
            return;
        }

        if (!selection.Contains(e.Location))
        {
            ResizeHandle selectionResizeHandle = ResizeHandleGeometry.HitTest(ToRectangleF(selection), e.Location, 12f);
            if (currentTool == AnnotationTool.Select && selectionResizeHandle != ResizeHandle.None)
            {
                BeginSelectionResize(selectionResizeHandle, e.Location);
            }

            return;
        }

        lastPoint = e.Location;
        selectedNotePart = NotePart.None;

        if (currentTool == AnnotationTool.Select)
        {
            if (BeginSelectInteraction(e.Location))
            {
                return;
            }

            ResizeHandle selectionResizeHandle = ResizeHandleGeometry.HitTest(ToRectangleF(selection), e.Location, 12f);
            if (selectionResizeHandle != ResizeHandle.None)
            {
                BeginSelectionResize(selectionResizeHandle, e.Location);
                return;
            }

            BeginSelectionMove(e.Location);
            return;
        }

        BeginDrawInteraction(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        switch (interaction)
        {
            case OverlayInteraction.SelectingRegion:
                selection = NormalizeRectangle(dragStart, e.Location);
                Invalidate();
                break;
            case OverlayInteraction.DrawingShape:
                Point drawPoint = ClampPoint(e.Location, selection);
                activeElement?.Resize(CreateDrawBounds(dragStart, drawPoint, activeElement is EllipseAnnotation && ModifierKeys.HasFlag(Keys.Shift)));
                Invalidate(selection);
                break;
            case OverlayInteraction.DrawingPen:
                activeStroke?.AddPoint(ClampPoint(e.Location, selection));
                Invalidate(selection);
                break;
            case OverlayInteraction.MovingSelection:
                MoveSelection(e.Location);
                break;
            case OverlayInteraction.ResizingSelection:
                ResizeSelection(e.Location);
                break;
            case OverlayInteraction.Moving:
                MoveSelected(e.Location);
                break;
            case OverlayInteraction.Resizing:
                ResizeSelected(e.Location);
                break;
            case OverlayInteraction.None:
                UpdateHoverCursor(e.Location);
                break;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        OverlayInteraction completedInteraction = interaction;
        if (interaction == OverlayInteraction.SelectingRegion)
        {
            selection = NormalizeRectangle(dragStart, e.Location);
            hasSelection = selection.Width > 4 && selection.Height > 4;
            toolbar.Visible = hasSelection;
            if (hasSelection)
            {
                PlaceToolbar();
            }
        }

        interaction = OverlayInteraction.None;
        activeResizeHandle = ResizeHandle.None;
        resizingNotePart = NotePart.None;
        activeElement = null;
        activeStroke = null;
        if (completedInteraction is OverlayInteraction.DrawingShape or OverlayInteraction.DrawingPen)
        {
            SwitchToSelectTool();
        }

        if (completedInteraction is OverlayInteraction.MovingSelection or OverlayInteraction.ResizingSelection && hasSelection)
        {
            toolbar.Visible = true;
            PlaceToolbar();
        }

        Invalidate();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button == MouseButtons.Left)
        {
            TryEditNoteText(e.Location);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            if (inlineTextEditor is not null)
            {
                CancelInlineTextEdit();
                return true;
            }

            CancelCapture();
            return true;
        }

        if (keyData == Keys.Enter)
        {
            if (inlineTextEditor is not null)
            {
                CommitInlineTextEdit();
                return true;
            }

            CopySelection();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool BeginSelectInteraction(Point point)
    {
        AnnotationElement? selected = canvas.SelectAt(point);
        if (selected is null)
        {
            return false;
        }

        AnnotationResizeHit resizeHit = AnnotationResizeHitTest.TryHit(selected, point);
        if (resizeHit.HasHit)
        {
            resizeStartBounds = resizeHit.Bounds;
            activeResizeHandle = resizeHit.Handle;
            resizingNotePart = resizeHit.NotePart;
            selectedNotePart = resizeHit.NotePart;
            interaction = OverlayInteraction.Resizing;
            return true;
        }

        selectedNotePart = selected is NoteAnnotationGroup note ? note.HitTestPart(point) : NotePart.None;
        interaction = OverlayInteraction.Moving;
        Invalidate(selection);
        return true;
    }

    private void BeginDrawInteraction(Point point)
    {
        dragStart = point;
        interaction = currentTool == AnnotationTool.Pen ? OverlayInteraction.DrawingPen : OverlayInteraction.DrawingShape;

        activeElement = currentTool switch
        {
            AnnotationTool.Rectangle => new RectangleAnnotation(RectangleF.FromLTRB(point.X, point.Y, point.X, point.Y)),
            AnnotationTool.Ellipse => new EllipseAnnotation(RectangleF.FromLTRB(point.X, point.Y, point.X, point.Y)),
            AnnotationTool.Note => new NoteAnnotationGroup(RectangleF.FromLTRB(point.X, point.Y, point.X, point.Y)),
            AnnotationTool.Pen => new FreehandAnnotation([point]),
            _ => null
        };

        if (activeElement is null)
        {
            return;
        }

        if (activeElement is FreehandAnnotation stroke)
        {
            activeStroke = stroke;
        }

        canvas.Add(activeElement);
        Invalidate(selection);
    }

    private void BeginSelectionMove(Point point)
    {
        canvas.ClearSelection();
        dragStart = point;
        selectionBeforeDrag = selection;
        interaction = OverlayInteraction.MovingSelection;
        toolbar.Visible = false;
        Invalidate();
    }

    private void BeginSelectionResize(ResizeHandle handle, Point point)
    {
        canvas.ClearSelection();
        dragStart = point;
        selectionBeforeDrag = selection;
        activeResizeHandle = handle;
        interaction = OverlayInteraction.ResizingSelection;
        toolbar.Visible = false;
        Invalidate();
    }

    private void MoveSelection(Point point)
    {
        var totalDelta = new Size(point.X - dragStart.X, point.Y - dragStart.Y);
        SelectionMoveResult result = SelectionBoundsEditor.Move(selectionBeforeDrag, totalDelta, ClientSize);
        Size frameDelta = new(result.Selection.Left - selection.Left, result.Selection.Top - selection.Top);
        selection = result.Selection;
        if (!frameDelta.IsEmpty)
        {
            canvas.MoveAll(new SizeF(frameDelta.Width, frameDelta.Height));
        }

        Invalidate();
    }

    private void ResizeSelection(Point point)
    {
        if (activeResizeHandle == ResizeHandle.None)
        {
            return;
        }

        selection = SelectionBoundsEditor.Resize(selectionBeforeDrag, activeResizeHandle, point, ClientSize);
        Invalidate();
    }

    private void MoveSelected(Point point)
    {
        var delta = new SizeF(point.X - lastPoint.X, point.Y - lastPoint.Y);
        if (canvas.SelectedElement is NoteAnnotationGroup note &&
            selectedNotePart is NotePart.Target or NotePart.Text)
        {
            note.MovePart(selectedNotePart, delta);
        }
        else
        {
            canvas.MoveSelected(delta);
        }

        lastPoint = point;
        Invalidate(selection);
    }

    private void ResizeSelected(Point point)
    {
        if (activeResizeHandle == ResizeHandle.None)
        {
            return;
        }

        Point clampedPoint = ClampPoint(point, selection);
        RectangleF newBounds = ResizeHandleGeometry.Resize(
            resizeStartBounds,
            activeResizeHandle,
            clampedPoint,
            limit: ToRectangleF(selection));

        if (canvas.SelectedElement is NoteAnnotationGroup note && resizingNotePart is NotePart.Target or NotePart.Text or NotePart.Outer)
        {
            note.ResizePart(resizingNotePart, newBounds);
        }
        else
        {
            canvas.ResizeSelected(newBounds);
        }

        Invalidate(selection);
    }

    private bool TryEditNoteText(Point point)
    {
        if (!hasSelection || !selection.Contains(point))
        {
            return false;
        }

        if (canvas.SelectAt(point) is not NoteAnnotationGroup note || note.HitTestPart(point) != NotePart.Text)
        {
            return false;
        }

        BeginInlineTextEdit(NoteTextEditSession.TryBegin(note, point)!.Value);

        return true;
    }

    private void BeginInlineTextEdit(NoteTextEditSession session)
    {
        CommitInlineTextEdit();
        activeTextEdit = session;
        Rectangle editorBounds = Rectangle.Round(session.Bounds);
        editorBounds.Inflate(-2, -2);

        inlineTextEditor = new TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(35, 38, 46),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f),
            Text = session.Note.Text,
            Bounds = editorBounds,
            ScrollBars = ScrollBars.Vertical
        };
        inlineTextEditor.KeyDown += InlineTextEditor_KeyDown;
        inlineTextEditor.LostFocus += InlineTextEditor_LostFocus;
        Controls.Add(inlineTextEditor);
        inlineTextEditor.BringToFront();
        inlineTextEditor.Focus();
        inlineTextEditor.SelectAll();
    }

    private void InlineTextEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            CancelInlineTextEdit();
            return;
        }

        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            CommitInlineTextEdit();
        }
    }

    private void InlineTextEditor_LostFocus(object? sender, EventArgs e)
    {
        if (!closingInlineTextEditor)
        {
            CommitInlineTextEdit();
        }
    }

    private void CommitInlineTextEdit()
    {
        if (inlineTextEditor is null || activeTextEdit is null)
        {
            return;
        }

        closingInlineTextEditor = true;
        activeTextEdit.Value.Commit(inlineTextEditor.Text);
        EndInlineTextEdit();
        Invalidate(selection);
    }

    private void CancelInlineTextEdit()
    {
        if (inlineTextEditor is null || activeTextEdit is null)
        {
            return;
        }

        closingInlineTextEditor = true;
        activeTextEdit.Value.Cancel();
        EndInlineTextEdit();
        Invalidate(selection);
    }

    private void EndInlineTextEdit()
    {
        TextBox? editor = inlineTextEditor;
        inlineTextEditor = null;
        activeTextEdit = null;
        if (editor is not null)
        {
            editor.KeyDown -= InlineTextEditor_KeyDown;
            editor.LostFocus -= InlineTextEditor_LostFocus;
            Controls.Remove(editor);
            editor.Dispose();
        }

        closingInlineTextEditor = false;
        Focus();
    }

    private void SwitchToSelectTool()
    {
        currentTool = AnnotationTool.Select;
        toolbar.SetActiveTool(AnnotationTool.Select);
        Cursor = Cursors.Default;
    }

    private void UpdateHoverCursor(Point point)
    {
        if (!hasSelection || !selection.Contains(point) || currentTool != AnnotationTool.Select)
        {
            ResizeHandle outsideSelectionHandle = hasSelection && currentTool == AnnotationTool.Select
                ? ResizeHandleGeometry.HitTest(ToRectangleF(selection), point, 12f)
                : ResizeHandle.None;
            Cursor = outsideSelectionHandle != ResizeHandle.None
                ? CursorForResizeHandle(outsideSelectionHandle)
                : currentTool == AnnotationTool.Select ? Cursors.Default : Cursors.Cross;
            return;
        }

        AnnotationElement? selected = canvas.SelectedElement;
        if (selected is not null)
        {
            AnnotationResizeHit resizeHit = AnnotationResizeHitTest.TryHit(selected, point);
            if (resizeHit.HasHit)
            {
                Cursor = CursorForResizeHandle(resizeHit.Handle);
                return;
            }
        }

        ResizeHandle selectionHandle = ResizeHandleGeometry.HitTest(ToRectangleF(selection), point, 12f);
        if (selectionHandle != ResizeHandle.None)
        {
            Cursor = CursorForResizeHandle(selectionHandle);
            return;
        }

        Cursor = Cursors.Default;
    }

    private void CopySelection()
    {
        if (!hasSelection)
        {
            return;
        }

        try
        {
            CommitInlineTextEdit();
            using Bitmap rendered = exportService.Render(capture.DesktopBitmap, selection, canvas.Elements);
            exportService.CopyToClipboardWithRetry(rendered);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "复制失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveSelection()
    {
        if (!hasSelection)
        {
            return;
        }

        string? directory = settings.SaveDirectory;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            directory = PromptForDirectory(directory);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            settings.SaveDirectory = directory;
            settingsStore.Save(settings);
        }

        try
        {
            CommitInlineTextEdit();
            exportService.SaveToDirectory(capture.DesktopBitmap, selection, canvas.Elements, directory, DateTime.Now, settings.ImageFormat);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? PromptForDirectory(string? currentDirectory)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择截图保存目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(currentDirectory) ? currentDirectory : string.Empty
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void CancelCapture()
    {
        Close();
    }

    private void PlaceToolbar()
    {
        int x = Math.Clamp(selection.Left, 8, Math.Max(8, ClientSize.Width - toolbar.Width - 8));
        int below = selection.Bottom + 10;
        int above = selection.Top - toolbar.Height - 10;
        int y = below + toolbar.Height <= ClientSize.Height ? below : Math.Max(8, above);
        toolbar.Location = new Point(x, y);
        toolbar.BringToFront();
    }

    private static Rectangle NormalizeRectangle(Point start, Point end)
    {
        int left = Math.Min(start.X, end.X);
        int top = Math.Min(start.Y, end.Y);
        int right = Math.Max(start.X, end.X);
        int bottom = Math.Max(start.Y, end.Y);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static RectangleF CreateDrawBounds(Point start, Point end, bool keepSquare)
    {
        if (!keepSquare)
        {
            return RectangleF.FromLTRB(start.X, start.Y, end.X, end.Y);
        }

        int dx = end.X - start.X;
        int dy = end.Y - start.Y;
        int size = Math.Min(Math.Abs(dx), Math.Abs(dy));
        int right = start.X + Math.Sign(dx == 0 ? 1 : dx) * size;
        int bottom = start.Y + Math.Sign(dy == 0 ? 1 : dy) * size;
        return RectangleF.FromLTRB(start.X, start.Y, right, bottom);
    }

    private static Point ClampPoint(Point point, Rectangle rectangle)
    {
        return new Point(
            Math.Clamp(point.X, rectangle.Left, rectangle.Right),
            Math.Clamp(point.Y, rectangle.Top, rectangle.Bottom));
    }

    private bool IsSelectionAdjustmentInProgress()
    {
        return interaction is OverlayInteraction.MovingSelection or OverlayInteraction.ResizingSelection;
    }

    private static void DrawShade(Graphics graphics, Rectangle selection)
    {
        using var shade = new SolidBrush(Color.FromArgb(120, Color.Black));
        graphics.FillRectangle(shade, 0, 0, graphics.VisibleClipBounds.Width, selection.Top);
        graphics.FillRectangle(shade, 0, selection.Bottom, graphics.VisibleClipBounds.Width, graphics.VisibleClipBounds.Height - selection.Bottom);
        graphics.FillRectangle(shade, 0, selection.Top, selection.Left, selection.Height);
        graphics.FillRectangle(shade, selection.Right, selection.Top, graphics.VisibleClipBounds.Width - selection.Right, selection.Height);
    }

    private static void DrawSelectionFrame(Graphics graphics, Rectangle selection)
    {
        using var pen = new Pen(Color.FromArgb(245, 255, 255, 255), 1.5f);
        graphics.DrawRectangle(pen, selection);
        DrawResizeHandles(graphics, selection, 10f);
    }

    private void DrawSelectedResizeHandles(Graphics graphics)
    {
        AnnotationElement? selected = canvas.SelectedElement;
        if (selected is NoteAnnotationGroup note)
        {
            DrawResizeHandles(graphics, note.Bounds);
            DrawResizeHandles(graphics, note.TargetBounds);
            DrawResizeHandles(graphics, note.TextBounds);
            return;
        }

        if (selected is not null)
        {
            DrawResizeHandles(graphics, selected.Bounds);
        }
    }

    private static void DrawResizeHandles(Graphics graphics, RectangleF bounds, float size = ResizeHandleGeometry.DefaultHandleSize)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var brush = new SolidBrush(UiColors.ButtonActive);
        using var pen = new Pen(Color.White, 1f);
        foreach ((_, RectangleF handleBounds) in ResizeHandleGeometry.GetHandleBounds(bounds, size))
        {
            graphics.FillRectangle(brush, handleBounds);
            graphics.DrawRectangle(pen, handleBounds.X, handleBounds.Y, handleBounds.Width, handleBounds.Height);
        }
    }

    private static Cursor CursorForResizeHandle(ResizeHandle handle)
    {
        return handle switch
        {
            ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
            ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
            _ => Cursors.Default
        };
    }

    private static RectangleF ToRectangleF(Rectangle rectangle)
    {
        return new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    private enum OverlayInteraction
    {
        None,
        SelectingRegion,
        DrawingShape,
        DrawingPen,
        MovingSelection,
        ResizingSelection,
        Moving,
        Resizing
    }
}
