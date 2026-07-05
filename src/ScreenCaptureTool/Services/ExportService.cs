using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenCaptureTool.Annotations;

namespace ScreenCaptureTool.Services;

public sealed class ExportService
{
    public Bitmap Render(Bitmap desktopBitmap, Rectangle selection, IEnumerable<AnnotationElement> annotations)
    {
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            throw new ArgumentException("Selection must have positive size.", nameof(selection));
        }

        var result = new Bitmap(selection.Width, selection.Height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(result);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(
            desktopBitmap,
            new Rectangle(0, 0, selection.Width, selection.Height),
            selection,
            GraphicsUnit.Pixel);

        graphics.TranslateTransform(-selection.Left, -selection.Top);
        foreach (AnnotationElement annotation in annotations)
        {
            if (annotation.Bounds.IntersectsWith(selection))
            {
                annotation.Draw(graphics);
            }
        }

        return result;
    }

    public void CopyToClipboardWithRetry(Bitmap image, int attempts = 5, int delayMilliseconds = 80)
    {
        Exception? lastError = null;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return;
            }
            catch (ExternalException ex)
            {
                lastError = ex;
                Thread.Sleep(delayMilliseconds);
            }
        }

        throw new InvalidOperationException("Clipboard is unavailable.", lastError);
    }

    public string SaveToDirectory(
        Bitmap desktopBitmap,
        Rectangle selection,
        IEnumerable<AnnotationElement> annotations,
        string directory,
        DateTime? now = null,
        string imageFormat = "png")
    {
        Directory.CreateDirectory(directory);
        string path = CreateUniqueFilePath(directory, now ?? DateTime.Now, imageFormat);
        using Bitmap rendered = Render(desktopBitmap, selection, annotations);
        rendered.Save(path, ToImageFormat(imageFormat));
        return path;
    }

    public string CreateUniqueFilePath(string directory, DateTime timestamp, string extension)
    {
        string normalizedExtension = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedExtension))
        {
            normalizedExtension = "png";
        }

        string stem = $"Screenshot_{timestamp:yyyyMMdd_HHmmss}";
        string candidate = Path.Combine(directory, $"{stem}.{normalizedExtension}");
        int suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{stem}_{suffix}.{normalizedExtension}");
            suffix++;
        }

        return candidate;
    }

    private static ImageFormat ToImageFormat(string imageFormat)
    {
        return imageFormat.Equals("png", StringComparison.OrdinalIgnoreCase)
            ? ImageFormat.Png
            : ImageFormat.Png;
    }
}
