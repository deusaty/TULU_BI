namespace TULU_BI;

public partial class MainPage : ContentPage // (O el nombre de clase que tenga tu Dashboard)
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnDashboardTapped(object sender, EventArgs e)
    {
        // Ya te encuentras en el Dashboard, puedes dejarlo vacío o recargar datos
    }

    private async void OnInventarioTapped(object sender, EventArgs e)
    {
        // Te manda a GesInv.xaml
        await Navigation.PushAsync(new GesInv());
    }

    private async void OnAlertasTapped(object sender, EventArgs e)
    {
        // Te manda a AlertCen.xaml
        await Navigation.PushAsync(new AlertCen());
    }

    private async void OnAnaliticaTapped(object sender, EventArgs e)
    {
        // Te manda a AnaliticRen.xaml
        await Navigation.PushAsync(new AnaliticRen());
    }
}
