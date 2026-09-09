using System.Configuration;
using System.IO;
using System.Windows;

namespace ERP.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Log global para diagnosticar cierre inmediato
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "erp-desktop-crash.log"), args.ExceptionObject.ToString() ?? "unknown"); } catch { }
            MessageBox.Show(args.ExceptionObject.ToString(), "ERP Escritorio — Error no controlado", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        DispatcherUnhandledException += (s, args) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "erp-desktop-crash.log"), args.Exception.ToString()); } catch { }
            MessageBox.Show(args.Exception.ToString(), "ERP Escritorio — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);

        // Escritorio reutiliza 100% del proyecto web tal cual:
        // - DB: mismo erp.db / Gestion*.db (maestro + tenant) en %LocalAppData%/ERP o junto al exe
        // - API: http://localhost:5109 (configurable via ERP.Desktop.json o args)
        var apiUrl = GetApiUrl(e.Args);

        var main = new MainWindow(apiUrl);
        main.Show();
    }

    private static string GetApiUrl(string[] args)
    {
        // 1) Arg --urls
        foreach (var a in args)
            if (a.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return a.TrimEnd('/');

        // 2) ERP.Desktop.json { "ApiUrl": "http://localhost:5109" }
        try
        {
            var cfg = Path.Combine(AppContext.BaseDirectory, "ERP.Desktop.json");
            if (File.Exists(cfg))
            {
                var json = File.ReadAllText(cfg);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ApiUrl", out var v) && v.GetString() is { } s && !string.IsNullOrWhiteSpace(s))
                    return s.TrimEnd('/');
            }
        }
        catch { }

        // 3) Default: mismo que ERP.Api/Properties/launchSettings.json
        return "http://localhost:5109";
    }
}
