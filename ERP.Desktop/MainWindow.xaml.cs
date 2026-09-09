using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace ERP.Desktop;

public partial class MainWindow : Window
{
    private readonly string _apiUrl;
    private Process? _apiProcess;

    public MainWindow(string apiUrl)
    {
        InitializeComponent();
        _apiUrl = apiUrl;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = $"Conectando a {_apiUrl}...";
            LoadingText.Text = $"Conectando a {_apiUrl}...";

            // 1. Asegurar que el API esté corriendo (modo escritorio = API embebido o externo)
            await EnsureApiRunningAsync();

            // 2. Inicializar WebView2
            try
            {
                await MainWebView.EnsureCoreWebView2Async();
                MainWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                MainWebView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    StatusText.Text = $"Conectado — {_apiUrl}";
                };

                MainWebView.Source = new Uri(_apiUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo inicializar WebView2:\n{ex.Message}\n\nAsegúrate de tener WebView2 Runtime instalado.", "ERP Escritorio", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingText.Text = "Error WebView2";
                StatusText.Text = "Error WebView2";
            }
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "erp-desktop-crash.log"), ex.ToString()); } catch { }
            MessageBox.Show(ex.ToString(), "ERP Escritorio — Error en arranque", MessageBoxButton.OK, MessageBoxImage.Error);
            LoadingText.Text = "Error de arranque";
        }
    }

    private async Task EnsureApiRunningAsync()
    {
        // Si el API ya responde, no lanzar otro proceso
        if (await IsUrlReachableAsync(_apiUrl))
            return;

        LoadingText.Text = "Iniciando API local...";
        StatusText.Text = "Iniciando API local...";

        // Intentar lanzar ERP.Api.dll desde la misma carpeta que el exe escritorio
        var exeDir = AppContext.BaseDirectory;
        var apiDll = Path.Combine(exeDir, "ERP.Api.dll");
        var apiExe = Path.Combine(exeDir, "ERP.Api.exe");

        // En desarrollo: buscar el proyecto ERP.Api
        if (!File.Exists(apiDll) && !File.Exists(apiExe))
        {
            var devApi = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "ERP.Api", "bin", "Release", "net9.0", "ERP.Api.dll"));
            if (File.Exists(devApi)) apiDll = devApi;
            else
            {
                var devApiDebug = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "ERP.Api", "bin", "Debug", "net9.0", "ERP.Api.dll"));
                if (File.Exists(devApiDebug)) apiDll = devApiDebug;
            }
        }

        if (File.Exists(apiDll))
        {
            var psi = new ProcessStartInfo("dotnet", $"\"{apiDll}\" --urls {_apiUrl}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(apiDll)!
            };
            _apiProcess = Process.Start(psi);
        }
        else if (File.Exists(apiExe))
        {
            var psi = new ProcessStartInfo(apiExe, $"--urls {_apiUrl}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(apiExe)!
            };
            _apiProcess = Process.Start(psi);
        }
        else
        {
            // No se encontró binario: asumir que el usuario ejecuta API manualmente (dotnet run)
            Debug.WriteLine("ERP.Api.dll no encontrado, esperando API externa en " + _apiUrl);
        }

        // Esperar hasta 15s a que el API responda
        for (int i = 0; i < 15; i++)
        {
            await Task.Delay(1000);
            if (await IsUrlReachableAsync(_apiUrl))
                return;
            LoadingText.Text = $"Iniciando API local... ({i + 1}s)";
        }
    }

    private static async Task<bool> IsUrlReachableAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var resp = await client.GetAsync(url);
            return resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch { return false; }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            // No matar el API si fue externo; solo si lo lanzó el escritorio
            if (_apiProcess != null && !_apiProcess.HasExited)
            {
                _apiProcess.Kill(entireProcessTree: true);
                _apiProcess.Dispose();
            }
        }
        catch { }
    }

    private void Reload_Click(object sender, RoutedEventArgs e) => MainWebView.Reload();

    private void DevTools_Click(object sender, RoutedEventArgs e)
    {
        try { MainWebView.CoreWebView2.OpenDevToolsWindow(); } catch { }
    }
}
