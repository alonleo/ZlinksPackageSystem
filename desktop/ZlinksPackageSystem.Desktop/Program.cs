using Avalonia;

namespace ZlinksPackageSystem.Desktop;

class Program
{
#if !AS_LIBRARY
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
#endif

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
