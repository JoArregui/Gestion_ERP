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

            // Navegar aunque la API no haya respondido: WebView2 mostrará el error pero la ventana NO se cierra
            MainWebView.Source = new Uri(_apiUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo inicializar WebView2:\n{ex.Message}\n\nAsegúrate de tener WebView2 Runtime instalado.", "ERP Escritorio", MessageBoxButton.OK, MessageBoxImage.Error);
            LoadingText.Text = "Error WebView2";
            StatusText.Text = "Error WebView2 — instala WebView2 Runtime";
            // Mantener overlay visible con botón de reintento: no cerrar app
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
        var logPath = Path.Combine(AppContext.BaseDirectory, "erp-desktop.log");
        void Log(string m) { try { File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {m}\n"); } catch { } Debug.WriteLine(m); }

        Log($"EnsureApiRunning start Url={_apiUrl} BaseDir={AppContext.BaseDirectory}");
        if (await IsUrlReachableAsync(_apiUrl))
        {
            Log("API ya responde, no se lanza proceso");
            return;
        }

        LoadingText.Text = "Iniciando API local...";
        StatusText.Text = "Iniciando API local...";

        var exeDir = AppContext.BaseDirectory;
        var apiDll = Path.Combine(exeDir, "ERP.Api.dll");
        var apiExe = Path.Combine(exeDir, "ERP.Api.exe");
        Log($"Buscando API en exeDir={exeDir} dllExists={File.Exists(apiDll)} exeExists={File.Exists(apiExe)}");

        // En desarrollo: buscar el proyecto ERP.Api en varias rutas
        if (!File.Exists(apiDll) && !File.Exists(apiExe))
        {
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "ERP.Api", "bin", "Release", "net9.0", "ERP.Api.dll")),
                Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "ERP.Api", "bin", "Debug", "net9.0", "ERP.Api.dll")),
                Path.Combine(Directory.GetCurrentDirectory(), "ERP.Api", "bin", "Release", "net9.0", "ERP.Api.dll"),
                Path.Combine(Directory.GetCurrentDirectory(), "ERP.Api", "bin", "Debug", "net9.0", "ERP.Api.dll"),
                @"C:\Users\josearregui\Desktop\Proyectos\ERP .NET\ERP.Api\bin\Release\net9.0\ERP.Api.dll",
                @"C:\Users\josearregui\Desktop\Proyectos\ERP .NET\ERP.Api\bin\Debug\net9.0\ERP.Api.dll",
            };
            foreach (var c in candidates)
            {
                Log($"Probando candidato {c} exists={File.Exists(c)}");
                if (File.Exists(c)) { apiDll = c; break; }
            }
        }

        Log($"Final dll={apiDll} exe={apiExe}");

        try
        {
            if (File.Exists(apiDll))
            {
                var psi = new ProcessStartInfo("dotnet", $"\"{apiDll}\" --urls {_apiUrl}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(apiDll)!
                };
                Log($"Lanzando dotnet \"{apiDll}\" --urls {_apiUrl}");
                _apiProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _apiProcess.OutputDataReceived += (s, e) => { if (e.Data != null) Log("[API OUT] " + e.Data); };
                _apiProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Log("[API ERR] " + e.Data); };
                _apiProcess.Start();
                _apiProcess.BeginOutputReadLine();
                _apiProcess.BeginErrorReadLine();
                Log($"Proceso API lanzado PID={_apiProcess.Id}");
            }
            else if (File.Exists(apiExe))
            {
                var psi = new ProcessStartInfo(apiExe, $"--urls {_apiUrl}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(apiExe)!
                };
                Log($"Lanzando {apiExe} --urls {_apiUrl}");
                _apiProcess = Process.Start(psi);
                Log($"Proceso API exe PID={_apiProcess?.Id}");
            }
            else
            {
                Log("ERP.Api.dll/.exe no encontrado, se asume API externo manual");
                StatusText.Text = "API no encontrada — inicia manualmente 'dotnet run --project ERP.Api'";
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR lanzando API: {ex}");
            StatusText.Text = $"Error lanzando API: {ex.Message}";
        }

        // Esperar hasta 20s (antes 15) a que el API responda
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(1000);
            var ok = await IsUrlReachableAsync(_apiUrl);
            Log($"Check {i+1}/20 reachable={ok}");
            if (ok) return;
            LoadingText.Text = $"Iniciando API local... ({i + 1}s)";
            if (_apiProcess != null && _apiProcess.HasExited)
            {
                Log($"Proceso API terminó prematuramente ExitCode={_apiProcess.ExitCode}");
                StatusText.Text = $"API terminó (código {_apiProcess.ExitCode}) — revisa erp-desktop.log";
                break;
            }
        }

        // No se pudo iniciar: dejar ventana abierta con mensaje útil en lugar de cerrar
        Log("API no respondió tras 20s");
        StatusText.Text = $"API no responde en {_apiUrl}";
        LoadingText.Text = "API no disponible";
        // No throw: la ventana seguirá abierta y WebView2 mostrará error de navegación
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
