using Avalonia;

namespace NetPdf.Previewer.App;

/// <summary>Entry point for the previewer application.</summary>
public static class Program
{
    /// <summary>The port the preview server listens on, parsed from --port (default 12500).</summary>
    public static int Port { get; private set; } = 12500;

    /// <summary>Starts the previewer application.</summary>
    [STAThread]
    public static void Main(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port" && int.TryParse(args[i + 1], out var port))
            {
                Port = port;
            }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Configures Avalonia; also used by the designer.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
