using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TULU_BI;

public partial class MainPage : ContentPage
{
    private System.Threading.Timer? _relojTimer;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActualizarReloj();
        IniciarReloj();
        _ = CargarDashboardAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _relojTimer?.Dispose();
        _relojTimer = null;
    }

    // ── RELOJ EN TIEMPO REAL ────────────────────────────────────────────
    private void ActualizarReloj()
    {
        var ahora = DateTime.Now;
        string diaSemana = ahora.DayOfWeek switch
        {
            DayOfWeek.Monday    => "lunes",
            DayOfWeek.Tuesday   => "martes",
            DayOfWeek.Wednesday => "miércoles",
            DayOfWeek.Thursday  => "jueves",
            DayOfWeek.Friday    => "viernes",
            DayOfWeek.Saturday  => "sábado",
            _                   => "domingo"
        };
        string mes = ahora.Month switch
        {
            1 => "ene", 2 => "feb", 3 => "mar", 4 => "abr",
            5 => "may", 6 => "jun", 7 => "jul", 8 => "ago",
            9 => "sep", 10 => "oct", 11 => "nov", _ => "dic"
        };

        lblFecha.Text = $"{diaSemana}, {ahora.Day} {mes}";
        lblHora.Text  = ahora.ToString("HH:mm");

        lblSaludo.Text = ahora.Hour switch
        {
            < 12 => "¡Buenos días!",
            < 19 => "¡Buenas tardes!",
            _    => "¡Buenas noches!"
        };
    }

    private void IniciarReloj()
    {
        _relojTimer = new System.Threading.Timer(_ =>
        {
            MainThread.BeginInvokeOnMainThread(ActualizarReloj);
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    // ── CARGA PRINCIPAL DE DATOS ──────────────────────────────────────────
    private async Task CargarDashboardAsync()
    {
        panelCargando.IsVisible = true;

        try
        {
            clsDatos datos = new clsDatos();

            // Cargar todo en paralelo para mayor velocidad
            var tareasTask = Task.WhenAll(
                Task.Run(() => datos.cargarVentas()),
                Task.Run(() => datos.cargarOrdenes()),
                Task.Run(() => datos.cargarClientes()),
                Task.Run(() => datos.cargarEmpleados()),
                Task.Run(() => datos.cargarServicios())
            );

            await tareasTask;

            // Recuperar resultados
            var ventas    = await Task.Run(() => datos.cargarVentas());
            var ordenes   = await Task.Run(() => datos.cargarOrdenes());
            var clientes  = await Task.Run(() => datos.cargarClientes());
            var empleados = await Task.Run(() => datos.cargarEmpleados());
            var servicios = await Task.Run(() => datos.cargarServicios());

            // Filtro para mostrar el último mes con datos reales
            var ultimaVenta = ventas.OrderByDescending(v => v.fecha_venta).FirstOrDefault();
            DateTime mesFiltro = ultimaVenta != null ? ultimaVenta.fecha_venta : DateTime.Now;

            var ventasMes = ventas.Where(v => v.fecha_venta.Month == mesFiltro.Month && v.fecha_venta.Year == mesFiltro.Year).ToList();
            var ordenesMes = ordenes.Where(o => o.fecha_ingreso.Month == mesFiltro.Month && o.fecha_ingreso.Year == mesFiltro.Year).ToList();

            // --- REQUERIMIENTO DEL CLIENTE: DATOS REALISTAS PARA DEMO (Meta ~48.5k) ---
            // Aumentar la cantidad de ventas en memoria y ajustar sus precios para 
            // que el ticket promedio sea congruente (aprox. 280 ventas de ~$170)
            if (ventasMes.Count > 0)
            {
                decimal targetSuma = 48500m;
                int targetCountVentas = 285;
                decimal sumaReal = ventasMes.Sum(v => v.subtotal);
                
                if (sumaReal > 0)
                {
                    decimal factorPrecio = targetSuma / sumaReal;
                    var ventasSimuladas = new List<clsVenta>();
                    var ordenesSimuladas = new List<clsOrden>();
                    Random rnd = new Random();

                    for (int i = 0; i < targetCountVentas; i++)
                    {
                        var originalVenta = ventasMes[i % ventasMes.Count];
                        ventasSimuladas.Add(new clsVenta
                        {
                            id_venta = originalVenta.id_venta + 10000 + i,
                            id_cliente = originalVenta.id_cliente + (i % 150), // Genera aprox 150 usuarios únicos
                            id_trabajo = originalVenta.id_trabajo,
                            cantidad = originalVenta.cantidad,
                            subtotal = originalVenta.subtotal * factorPrecio,
                            fecha_venta = originalVenta.fecha_venta,
                            metodo_pago = originalVenta.metodo_pago
                        });
                    }

                    if (ordenesMes.Count > 0)
                    {
                        for (int i = 0; i < targetCountVentas; i++)
                        {
                            var originalOrden = ordenesMes[i % ordenesMes.Count];
                            ordenesSimuladas.Add(new clsOrden
                            {
                                id_orden = originalOrden.id_orden + 10000 + i,
                                id_cliente = originalOrden.id_cliente + (i % 150), // Debe coincidir con la generación de ventas
                                id_empleado = originalOrden.id_empleado,
                                estado = originalOrden.estado,
                                total = originalOrden.total * factorPrecio,
                                fecha_ingreso = originalOrden.fecha_ingreso,
                                fecha_listo = originalOrden.fecha_listo,
                                fecha_entrega_a = originalOrden.fecha_entrega_a,
                                tipo_entrega = originalOrden.tipo_entrega
                            });
                        }
                    }
                    
                    // Ajuste fino para clavar la suma exacta
                    decimal sumGenerada = ventasSimuladas.Sum(v => v.subtotal);
                    decimal ajustador = targetSuma / (sumGenerada == 0 ? 1 : sumGenerada);
                    ventasSimuladas.ForEach(v => v.subtotal *= ajustador);
                    ordenesSimuladas.ForEach(o => o.total *= ajustador);

                    ventasMes = ventasSimuladas;
                    ordenesMes = ordenesSimuladas;
                }
            }
            // -------------------------------------------------------------------------

            MainThread.BeginInvokeOnMainThread(() =>
            {
                panelCargando.IsVisible = false;
                PoblarEmpleadoActivo(empleados);
                PoblarKPIs(ventasMes, ordenesMes, clientes);
                PoblarMetodosPago(ventasMes);
                PoblarTopServicios(servicios);
                PoblarEquipo(empleados);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error CargarDashboardAsync: " + ex.Message);
            MainThread.BeginInvokeOnMainThread(() => panelCargando.IsVisible = false);
        }
    }

    // ── SALUDO PERSONALIZADO (primer empleado como "usuario activo") ───────
    private void PoblarEmpleadoActivo(List<clsEmpleado> empleados)
    {
        // Tomamos el primer empleado como el usuario activo de esta sesión
        if (empleados.Count > 0)
        {
            var emp = empleados[0];
            lblNombreEmpleado.Text = emp.nombre_corto;
            lblRol.Text = emp.rol;
            lblTurno.Text = $"• Lavandería TULU — ID #{emp.id_empleado}";
        }
    }

    // ── KPIs ──────────────────────────────────────────────────────────────
    private void PoblarKPIs(List<clsVenta> ventasMesActual, List<clsOrden> ordenesMesActual, List<clsClientes> clientes)
    {
        // Ingresos y Ventas Totales (Del mes filtrado)
        decimal totalIngresos = ventasMesActual.Sum(v => v.subtotal);
        int totalVentas       = ventasMesActual.Count;
        int pagadas           = ordenesMesActual.Count(o => o.estado == "Pagado");
        int pendientes        = ordenesMesActual.Count(o => o.estado == "Pendiente");
        decimal montoPendiente = ordenesMesActual.Where(o => o.estado == "Pendiente").Sum(o => o.total);
        int totalClientes     = clientes.Count;
        int clientesActivos   = clientes.Count(c => c.activo);

        lblTotalVentas.Text          = $"${totalIngresos:0.00}";
        lblTotalVentasSubtitle.Text  = $"{totalVentas} ventas registradas";
        lblTotalClientes.Text        = totalClientes.ToString();
        lblClientesActivos.Text      = $"{clientesActivos} activos";
        lblOrdenesPagadas.Text       = pagadas.ToString();
        lblOrdenesPagadasSubtitle.Text = "órdenes completadas";
        lblOrdenesPendientes.Text    = pendientes.ToString();
        lblOrdenesPendientesSubtitle.Text = $"${montoPendiente:0} por cobrar";
    }

    // ── MÉTODOS DE PAGO ───────────────────────────────────────────────────
    private void PoblarMetodosPago(List<clsVenta> ventasMesActual)
    {
        // --- SECCIÓN: MÉTODOS DE PAGO (Del mes filtrado) ---
        int total = ventasMesActual.Count;
        int efectivo = ventasMesActual.Count(v => v.metodo_pago == "Efectivo");
        int tarjeta  = ventasMesActual.Count(v => v.metodo_pago == "Tarjeta");
        int transferencia = ventasMesActual.Count(v => v.metodo_pago == "Transferencia");

        lblTotalTransacciones.Text = $"{total} transacciones este mes";

        lblEfectivoCount.Text      = $"{efectivo} ventas";
        lblTarjetaCount.Text       = $"{tarjeta} ventas";
        lblTransferenciaCount.Text = $"{transferencia} ventas";

        double dTotal = total > 0 ? total : 1;
        pbEfectivo.Progress      = efectivo / dTotal;
        pbTarjeta.Progress       = tarjeta / dTotal;
        pbTransferencia.Progress = transferencia / dTotal;
    }

    // ── TOP SERVICIOS (por precio más alto, representando los más rentables) ─
    private void PoblarTopServicios(List<clsServicio> servicios)
    {
        var top = servicios.Where(s => s.activo)
                           .OrderByDescending(s => s.precio_actual)
                           .Take(3)
                           .ToList();

        if (top.Count >= 1)
        {
            lblTop1Nombre.Text = top[0].descripcion_servicio;
            lblTop1Sub.Text    = $"⏱️ {top[0].tiempo_estimado} min estimado";
            lblTop1Precio.Text = $"${top[0].precio_actual:0}";
        }
        if (top.Count >= 2)
        {
            lblTop2Nombre.Text = top[1].descripcion_servicio;
            lblTop2Sub.Text    = $"⏱️ {top[1].tiempo_estimado} min estimado";
            lblTop2Precio.Text = $"${top[1].precio_actual:0}";
        }
        if (top.Count >= 3)
        {
            lblTop3Nombre.Text = top[2].descripcion_servicio;
            lblTop3Sub.Text    = $"⏱️ {top[2].tiempo_estimado} min estimado";
            lblTop3Precio.Text = $"${top[2].precio_actual:0}";
        }
    }

    // ── EQUIPO DE TURNO ───────────────────────────────────────────────────
    private void PoblarEquipo(List<clsEmpleado> empleados)
    {
        containerEmpleados.Children.Clear();

        foreach (var emp in empleados)
        {
            // Fila de empleado: Avatar + Nombre/Rol
            var fila = new Grid { ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };

            // Avatar con iniciales
            var avatar = new Border
            {
                BackgroundColor = Color.FromArgb("#0F2A1A"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                WidthRequest = 38,
                HeightRequest = 38,
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.Center
            };
            var lblInic = new Label
            {
                Text = emp.iniciales,
                TextColor = Color.FromArgb("#34D399"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            avatar.Content = lblInic;

            // Datos
            var info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 1 };
            info.Add(new Label { Text = emp.nombre_corto, TextColor = Colors.White, FontSize = 13, FontAttributes = FontAttributes.Bold });
            info.Add(new Label { Text = emp.rol, TextColor = Color.FromArgb("#64748B"), FontSize = 11 });

            // Emoji de rol
            var emojiLabel = new Label
            {
                Text = emp.emoji_rol,
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            fila.Add(avatar);      Grid.SetColumn(avatar, 0);
            fila.Add(info);        Grid.SetColumn(info, 1);
            fila.Add(emojiLabel);  Grid.SetColumn(emojiLabel, 2);

            containerEmpleados.Children.Add(fila);

            // Separador, excepto el último
            if (emp.id_empleado != empleados.Last().id_empleado)
            {
                containerEmpleados.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = Color.FromArgb("#1E293B"),
                    Margin = new Thickness(0, 4)
                });
            }
        }
    }

    // ── NAVEGACIÓN INFERIOR ───────────────────────────────────────────────
    private void OnDashboardTapped(object sender, EventArgs e)
    {
        // Ya estamos aquí, recargar si se desea
    }

    private async void OnInventarioTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GesInv());
    }

    private async void OnAlertasTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AlertCen());
    }

    private async void OnAnaliticaTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnaliticRen());
    }
}
