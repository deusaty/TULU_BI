using Microcharts;
using SkiaSharp;
using System.Linq;

namespace TULU_BI;

public partial class AnaliticRen : ContentPage
{
    public AnaliticRen()
    {
        InitializeComponent();
        CargarDatosAnaliticos();
    }

    private async void CargarDatosAnaliticos()
    {
        try
        {
            // Ejecutar en background para no congelar la UI
            var listaClientes = await Task.Run(() =>
            {
                clsDatos datos = new clsDatos();
                return datos.cargarClientes();
            });

            if (listaClientes != null && listaClientes.Count > 0)
            {
                // Métrica 3: Distribución (Ej: Por longitud de dirección o perfiles completos)
                int conDireccion = listaClientes.Count(c => !string.IsNullOrWhiteSpace(c.direccion));
                int sinDireccion = listaClientes.Count - conDireccion;

                int conTelefono = listaClientes.Count(c => !string.IsNullOrWhiteSpace(c.telefono));
                int sinTelefono = listaClientes.Count - conTelefono;

                var entries = new[]
                {
                    new ChartEntry(conDireccion)
                    {
                        Label = "Con Dirección",
                        ValueLabel = conDireccion.ToString(),
                        Color = SKColor.Parse("#00E676")
                    },
                    new ChartEntry(sinDireccion)
                    {
                        Label = "Sin Dirección",
                        ValueLabel = sinDireccion.ToString(),
                        Color = SKColor.Parse("#EF4444")
                    },
                    new ChartEntry(conTelefono)
                    {
                        Label = "Con Teléfono",
                        ValueLabel = conTelefono.ToString(),
                        Color = SKColor.Parse("#3B82F6")
                    },
                    new ChartEntry(sinTelefono)
                    {
                        Label = "Sin Teléfono",
                        ValueLabel = sinTelefono.ToString(),
                        Color = SKColor.Parse("#F59E0B")
                    }
                };

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Métrica 1: Total de Clientes
                    lblTotalClientes.Text = listaClientes.Count.ToString();
                    
                    // Asignar el Chart al ChartView
                    chartDistribucion.Chart = new BarChart
                    {
                        Entries = entries,
                        BackgroundColor = SKColors.Transparent,
                        LabelTextSize = 35,
                        LabelColor = SKColors.White,
                        ValueLabelOrientation = Orientation.Horizontal
                    };
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblTotalClientes.Text = "0";
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", "No se pudieron cargar los datos de analítica: " + ex.Message, "OK");
            });
        }
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }

    private async void OnInventarioTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GesInv());
    }

    private async void OnAlertasTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AlertCen());
    }

    private void OnAnaliticaTapped(object sender, EventArgs e)
    {
        // Ya te encuentras en Analítica
    }
}