using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenCaptureTool.Services;

public sealed class CaptureService
{
    public CaptureResult CaptureVirtualDesktop()
    {
        Rectangle bounds = SystemInformation.VirtualScreen;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("Virtual screen has no visible area.");
        }

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        return new CaptureResult(bitmap, bounds);
    }
}

public sealed class CaptureResult : IDisposable
{
    public CaptureResult(Bitmap desktopBitmap, Rectangle virtualBounds)
    {
        DesktopBitmap = desktopBitmap;
        VirtualBounds = virtualBounds;
    }

    public Bitmap DesktopBitmap { get; }

    public Rectangle VirtualBounds { get; }

    public void Dispose()
    {
        DesktopBitmap.Dispose();
    }
}
