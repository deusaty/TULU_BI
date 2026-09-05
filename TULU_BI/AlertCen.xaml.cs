namespace TULU_BI;

public partial class AlertCen : ContentPage
{
    public AlertCen()
    {
        InitializeComponent();
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        // Regresa a la vista principal (Dashboard)
        await Navigation.PopToRootAsync();
    }

    private async void OnInventarioTapped(object sender, EventArgs e)
    {
        // Va a la vista de Inventario
        await Navigation.PushAsync(new GesInv());
    }

    private void OnAlertasTapped(object sender, EventArgs e)
    {
        // Ya te encuentras en Alertas
    }

    private async void OnAnaliticaTapped(object sender, EventArgs e)
    {
        // Va a la vista de Analítica
        await Navigation.PushAsync(new AnaliticRen());
    }
}