using Avalonia;

namespace Capstone.Gui;

internal static class Program
{
    // STAThread is what Avalonia needs on Windows; it is harmless elsewhere.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
