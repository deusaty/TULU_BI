namespace TULU_BI;

public partial class GesInv : ContentPage
{
    public GesInv()
    {
        InitializeComponent();
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        // Regresa a la pantalla principal (Dashboard)
        await Navigation.PopAsync();
    }

    private void OnInventarioTapped(object sender, EventArgs e)
    {
        // Ya estás en Inventario
    }

    private async void OnAlertasTapped(object sender, EventArgs e)
    {
        // Va a la vista de Alertas
        await Navigation.PushAsync(new AlertCen());
    }

    private async void OnAnaliticaTapped(object sender, EventArgs e)
    {
        // Va a la vista de Analítica
        await Navigation.PushAsync(new AnaliticRen());
    }
}