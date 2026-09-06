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
            // Ejecutar en hilo secundario para evitar congelar la interfaz
            List<clsServicio> lista = await Task.Run(() => datos.cargarServicios());

            MainThread.BeginInvokeOnMainThread(() =>
            {
                panelCargando.IsVisible = false;
                _todosLosServicios = lista ?? new List<clsServicio>();

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

    // Navegación Inferior
    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
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