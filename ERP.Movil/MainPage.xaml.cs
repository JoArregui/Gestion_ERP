namespace ERP.Movil;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnDashboardClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//dashboard");
    }

    private void OnVentasClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//ventas");
    }

    private void OnComprasClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//compras");
    }

    private void OnRRHHClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//rrhh");
    }

    private void OnStockClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//stock");
    }

    private void OnMaestrosClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//maestros");
    }

    private void OnConfigClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//config");
    }
}