using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Xaml;

namespace ERP.Movil;

[XamlCompilation(XamlCompilationOptions.Skip)]
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string route)
        {
            GoToAsync($"//{route}");
        }
    }
}

public class FlyoutMenuItem
{
    public string Route { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}