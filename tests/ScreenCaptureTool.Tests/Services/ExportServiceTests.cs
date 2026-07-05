using System.Drawing;
using System.Drawing.Imaging;
using ScreenCaptureTool.Annotations;
using ScreenCaptureTool.Services;

namespace ScreenCaptureTool.Tests.Services;

public sealed class ExportServiceTests
{
    [Fact]
    public void Render_crops_selected_region()
    {
        using Bitmap desktop = CreateBitmap(100, 80, Color.CornflowerBlue);
        var service = new ExportService();

        using Bitmap result = service.Render(desktop, new Rectangle(10, 20, 30, 25), []);

        Assert.Equal(30, result.Width);
        Assert.Equal(25, result.Height);
        Assert.Equal(Color.CornflowerBlue.ToArgb(), result.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Render_draws_annotations_relative_to_selection()
    {
        using Bitmap desktop = CreateBitmap(80, 80, Color.White);
        var annotation = new RectangleAnnotation(new RectangleF(20, 20, 30, 20))
        {
            StrokeColor = Color.Red,
            StrokeWidth = 4f
        };
        var service = new ExportService();

        using Bitmap result = service.Render(desktop, new Rectangle(10, 10, 50, 50), [annotation]);

        Color pixel = result.GetPixel(10, 10);
        Assert.True(pixel.R > 180);
        Assert.True(pixel.G < 120);
        Assert.True(pixel.B < 120);
    }

    [Fact]
    public void CreateUniqueFilePath_uses_timestamped_png_name()
    {
        string directory = CreateTempDirectory();
        var now = new DateTime(2026, 7, 5, 6, 45, 30);
        var service = new ExportService();

        string path = service.CreateUniqueFilePath(directory, now, "png");

        Assert.Equal(Path.Combine(directory, "Screenshot_20260705_064530.png"), path);
    }

    [Fact]
    public void CreateUniqueFilePath_avoids_existing_name_collision()
    {
        string directory = CreateTempDirectory();
        var now = new DateTime(2026, 7, 5, 6, 45, 30);
        string existing = Path.Combine(directory, "Screenshot_20260705_064530.png");
        File.WriteAllText(existing, "exists");
        var service = new ExportService();

        string path = service.CreateUniqueFilePath(directory, now, "png");

        Assert.Equal(Path.Combine(directory, "Screenshot_20260705_064530_1.png"), path);
    }

    [Fact]
    public void SaveToDirectory_writes_png_file()
    {
        string directory = CreateTempDirectory();
        using Bitmap desktop = CreateBitmap(30, 30, Color.Green);
        var service = new ExportService();

        string path = service.SaveToDirectory(desktop, new Rectangle(0, 0, 20, 20), [], directory, new DateTime(2026, 7, 5));

        Assert.True(File.Exists(path));
        using Image saved = Image.FromFile(path);
        Assert.Equal(ImageFormat.Png.Guid, saved.RawFormat.Guid);
        Assert.Equal(20, saved.Width);
        Assert.Equal(20, saved.Height);
    }

    private static Bitmap CreateBitmap(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ScreenCaptureTool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
