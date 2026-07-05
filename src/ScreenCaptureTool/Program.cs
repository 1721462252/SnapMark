namespace ScreenCaptureTool;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new App.TrayApplicationContext());
    }
}
