using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TULU_BI;

public partial class GesInv : ContentPage
{
    private List<clsServicio> _todosLosServicios = new();
    private List<clsClientes> _clientes = new();
    private clsServicio _servicioSeleccionado;
    private string _filtroEstado = "todos"; // "todos", "activos", "inactivos"
    private string _textoBusqueda = "";

    public GesInv()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = CargarDatosServiciosAsync();
    }

    private async Task CargarDatosServiciosAsync()
    {
        panelCargando.IsVisible = true;

        try
        {
            clsDatos datos = new clsDatos();
            var taskServicios = datos.cargarServiciosAsync();
            var taskClientes = datos.cargarClientesAsync();

            await Task.WhenAll(taskServicios, taskClientes);

            List<clsServicio> lista = await taskServicios;
            List<clsClientes> listaClientes = await taskClientes;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                panelCargando.IsVisible = false;
                _todosLosServicios = lista ?? new List<clsServicio>();
                _clientes = listaClientes ?? new List<clsClientes>();
                pickerCliente.ItemsSource = _clientes;

                // Actualizar contadores en las tarjetas KPI
                int total = _todosLosServicios.Count;
                int activos = _todosLosServicios.Count(s => s.activo);
                int inactivos = total - activos;

                lblTotalServicios.Text = total.ToString();
                lblServiciosActivos.Text = activos.ToString();
                lblServiciosInactivos.Text = inactivos.ToString();

                AplicarFiltros();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error al cargar servicios: " + ex.Message);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                panelCargando.IsVisible = false;
            });
        }
    }

    private void AplicarFiltros()
    {
        if (_todosLosServicios == null) return;

        IEnumerable<clsServicio> consulta = _todosLosServicios;

        // Filtro por estado
        if (_filtroEstado == "activos")
        {
            consulta = consulta.Where(s => s.activo);
        }
        else if (_filtroEstado == "inactivos")
        {
            consulta = consulta.Where(s => !s.activo);
        }

        // Filtro por texto de búsqueda
        if (!string.IsNullOrWhiteSpace(_textoBusqueda))
        {
            string busqueda = _textoBusqueda.Trim().ToLower();
            consulta = consulta.Where(s =>
                (s.descripcion_servicio != null && s.descripcion_servicio.ToLower().Contains(busqueda)) ||
                s.id_trabajo.ToString() == busqueda ||
                s.precio_actual.ToString().Contains(busqueda));
        }

        var listaFiltrada = consulta.ToList();

        BindableLayout.SetItemsSource(containerServicios, listaFiltrada);
        panelSinResultados.IsVisible = listaFiltrada.Count == 0;
    }

    private void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
    {
        _textoBusqueda = e.NewTextValue ?? "";
        AplicarFiltros();
    }

    private void OnFiltroTodosTapped(object sender, EventArgs e)
    {
        _filtroEstado = "todos";
        ActualizarEstiloChips();
        AplicarFiltros();
    }

    private void OnFiltroActivosTapped(object sender, EventArgs e)
    {
        _filtroEstado = "activos";
        ActualizarEstiloChips();
        AplicarFiltros();
    }

    private void OnFiltroInactivosTapped(object sender, EventArgs e)
    {
        _filtroEstado = "inactivos";
        ActualizarEstiloChips();
        AplicarFiltros();
    }

    private void ActualizarEstiloChips()
    {
        // Reseteo general
        chipTodos.BackgroundColor = Color.FromArgb("#131B2E");
        lblChipTodos.TextColor = Color.FromArgb("#9CA3AF");

        chipActivos.BackgroundColor = Color.FromArgb("#131B2E");
        lblChipActivos.TextColor = Color.FromArgb("#9CA3AF");

        chipInactivos.BackgroundColor = Color.FromArgb("#131B2E");
        lblChipInactivos.TextColor = Color.FromArgb("#9CA3AF");

        // Activar el correspondiente
        switch (_filtroEstado)
        {
            case "activos":
                chipActivos.BackgroundColor = Color.FromArgb("#064E3B");
                lblChipActivos.TextColor = Color.FromArgb("#34D399");
                break;
            case "inactivos":
                chipInactivos.BackgroundColor = Color.FromArgb("#451A03");
                lblChipInactivos.TextColor = Color.FromArgb("#F87171");
                break;
            default:
                chipTodos.BackgroundColor = Color.FromArgb("#064E3B");
                lblChipTodos.TextColor = Color.FromArgb("#34D399");
                break;
        }
    }

    // ── MODAL DE VENTA ───────────────────────────────────────────────────
    private void OnVenderTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.BindingContext is clsServicio servicio)
        {
            if (!servicio.activo)
            {
                DisplayAlert("Servicio Inactivo", "No puedes registrar una venta de un servicio inactivo.", "OK");
                return;
            }

            _servicioSeleccionado = servicio;
            lblModalServicio.Text = servicio.descripcion_servicio;
            lblModalTiempo.Text = servicio.tiempo_formateado;
            lblModalTotal.Text = servicio.precio_formateado;
            pickerCliente.SelectedIndex = -1;
            
            modalRegistro.IsVisible = true;
        }
    }

    private void OnCerrarModalTapped(object sender, EventArgs e)
    {
        modalRegistro.IsVisible = false;
        _servicioSeleccionado = null;
    }

    private async void OnConfirmarVentaClicked(object sender, EventArgs e)
    {
        if (_servicioSeleccionado == null) return;
        
        var clienteSelec = pickerCliente.SelectedItem as clsClientes;
        if (clienteSelec == null)
        {
            await DisplayAlert("Falta Cliente", "Por favor selecciona un cliente de la lista.", "OK");
            return;
        }

        btnConfirmarVenta.IsEnabled = false;
        btnConfirmarVenta.Text = "Procesando...";

        try
        {
            // Usar SelectedIndex es más confiable que SelectedItem con x:Array en MAUI Android
            string estadoSelec = pckEstadoPago.SelectedIndex switch
            {
                1 => "Pendiente",
                _ => "Pagado"   // 0 o cualquier otro = Pagado
            };
            string metodoSelec = pckMetodoPago.SelectedIndex switch
            {
                1 => "Tarjeta",
                2 => "Transferencia",
                _ => "Efectivo"  // 0 o cualquier otro = Efectivo
            };

            clsDatos datos = new clsDatos();
            string resultado = await datos.registrarNuevaOperacionAsync(clienteSelec.id, _servicioSeleccionado.id_trabajo, _servicioSeleccionado.precio_actual, estadoSelec, metodoSelec);
            
            if (resultado == "Exito")
            {
                await DisplayAlert("Éxito", $"Venta de {_servicioSeleccionado.descripcion_servicio} registrada para {clienteSelec.nombre}.", "OK");
                modalRegistro.IsVisible = false;
            }
            else
            {
                await DisplayAlert("Error de SQL/WCF", resultado, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Excepción", ex.Message, "OK");
        }
        finally
        {
            btnConfirmarVenta.IsEnabled = true;
            btnConfirmarVenta.Text = "Confirmar Venta";
            _servicioSeleccionado = null;
        }
    }

    // ── NAVEGACIÓN ────────────────────────────────────────────────────────
    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
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