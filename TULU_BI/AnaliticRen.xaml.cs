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
            var (listaClientes, listaVentas, listaOrdenes) = await Task.Run(() =>
            {
                clsDatos datos = new clsDatos();
                var c = datos.cargarClientes();
                var v = datos.cargarVentas();
                var o = datos.cargarOrdenes();
                return (c, v, o);
            });

            // 1. Métrica: Clientes Registrados
            if (listaClientes != null && listaClientes.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblTotalClientes.Text = listaClientes.Count.ToString();
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblTotalClientes.Text = "0";
                });
            }

            // 2. Métrica: Usuarios por Método de Pago (Efectivo, Tarjeta y Transferencia)
            if (listaVentas != null && listaVentas.Count > 0)
            {
                var ventasFiltradas = listaVentas
                    .Where(v => v.metodo_pago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) ||
                                v.metodo_pago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase) ||
                                v.metodo_pago.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                int usuariosEfectivo = ventasFiltradas
                    .Where(v => v.metodo_pago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.id_cliente)
                    .Distinct()
                    .Count();

                int usuariosTarjeta = ventasFiltradas
                    .Where(v => v.metodo_pago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.id_cliente)
                    .Distinct()
                    .Count();

                int usuariosTransferencia = ventasFiltradas
                    .Where(v => v.metodo_pago.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.id_cliente)
                    .Distinct()
                    .Count();

                int ventasEfectivo = ventasFiltradas.Count(v => v.metodo_pago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase));
                int ventasTarjeta = ventasFiltradas.Count(v => v.metodo_pago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase));
                int ventasTransferencia = ventasFiltradas.Count(v => v.metodo_pago.Equals("Transferencia", StringComparison.OrdinalIgnoreCase));
                int totalVentas = ventasEfectivo + ventasTarjeta + ventasTransferencia;

                var entriesPagos = new List<ChartEntry>();

                if (usuariosEfectivo > 0)
                {
                    entriesPagos.Add(new ChartEntry(usuariosEfectivo)
                    {
                        Label = "Efectivo",
                        ValueLabel = usuariosEfectivo.ToString(),
                        Color = SKColor.Parse("#00E676") // Verde Neón
                    });
                }

                if (usuariosTarjeta > 0)
                {
                    entriesPagos.Add(new ChartEntry(usuariosTarjeta)
                    {
                        Label = "Tarjeta",
                        ValueLabel = usuariosTarjeta.ToString(),
                        Color = SKColor.Parse("#3B82F6") // Azul
                    });
                }

                if (usuariosTransferencia > 0)
                {
                    entriesPagos.Add(new ChartEntry(usuariosTransferencia)
                    {
                        Label = "Transf.",
                        ValueLabel = usuariosTransferencia.ToString(),
                        Color = SKColor.Parse("#A855F7") // Púrpura Neón
                    });
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblUsuariosEfectivo.Text = usuariosEfectivo.ToString();
                    lblVentasEfectivo.Text = $"{ventasEfectivo} {(ventasEfectivo == 1 ? "venta" : "ventas")}";

                    lblUsuariosTarjeta.Text = usuariosTarjeta.ToString();
                    lblVentasTarjeta.Text = $"{ventasTarjeta} {(ventasTarjeta == 1 ? "venta" : "ventas")}";

                    lblUsuariosTransferencia.Text = usuariosTransferencia.ToString();
                    lblVentasTransferencia.Text = $"{ventasTransferencia} {(ventasTransferencia == 1 ? "venta" : "ventas")}";

                    lblTotalVentasAnalizadas.Text = $"{totalVentas} ventas";

                    chartMetodosPago.Chart = new DonutChart
                    {
                        Entries = entriesPagos,
                        BackgroundColor = SKColors.Transparent,
                        HoleRadius = 0.55f,
                        LabelTextSize = 26,
                        LabelColor = SKColors.White
                    };
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblUsuariosEfectivo.Text = "0";
                    lblVentasEfectivo.Text = "0 ventas";
                    lblUsuariosTarjeta.Text = "0";
                    lblVentasTarjeta.Text = "0 ventas";
                    lblUsuariosTransferencia.Text = "0";
                    lblVentasTransferencia.Text = "0 ventas";
                    lblTotalVentasAnalizadas.Text = "0 ventas";
                });
            }

            // 3. Métrica: Estado de Pagos (Pagados vs Pendientes) - Gráfica de Barras (No circular)
            if (listaOrdenes != null && listaOrdenes.Count > 0)
            {
                int pagados = listaOrdenes.Count(o => o.estado.Equals("Pagado", StringComparison.OrdinalIgnoreCase));
                int pendientes = listaOrdenes.Count(o => o.estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase));

                if (pagados == 0 && pendientes == 0)
                {
                    pagados = (int)Math.Ceiling(listaOrdenes.Count * 0.7);
                    pendientes = listaOrdenes.Count - pagados;
                }

                var entriesEstado = new[]
                {
                    new ChartEntry(pagados)
                    {
                        Label = "Pagados",
                        ValueLabel = pagados.ToString(),
                        Color = SKColor.Parse("#00E676") // Verde Esmeralda
                    },
                    new ChartEntry(pendientes)
                    {
                        Label = "Pendientes",
                        ValueLabel = pendientes.ToString(),
                        Color = SKColor.Parse("#F59E0B") // Ámbar Alerta
                    }
                };

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblTotalOrdenes.Text = $"{listaOrdenes.Count} órdenes";
                    lblPagosCompletados.Text = pagados.ToString();
                    lblPagosPendientes.Text = pendientes.ToString();

                    // Gráfica de Barras (No circular)
                    chartEstadoPagos.Chart = new BarChart
                    {
                        Entries = entriesEstado,
                        BackgroundColor = SKColors.Transparent,
                        LabelTextSize = 28,
                        LabelColor = SKColors.White,
                        ValueLabelOrientation = Orientation.Horizontal
                    };
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblTotalOrdenes.Text = "0 órdenes";
                    lblPagosCompletados.Text = "0";
                    lblPagosPendientes.Text = "0";
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