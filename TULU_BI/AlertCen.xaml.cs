using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TULU_BI;

public partial class AlertCen : ContentPage
{
    private List<clsOrden> _allOrdenes = new();
    private List<clsEmpleado> _allEmpleados = new();
    private List<clsOrden> _allListas = new();
    private List<clsOrden> _allDomicilios = new();
    private List<clsOrden> _allMorosos = new();
    private List<clsClientes> _allClientes = new();

    public AlertCen()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = CargarOperacionesAsync();
    }

    private async Task CargarOperacionesAsync()
    {
        actListas.IsVisible = true;
        actListas.IsRunning = true;

        try
        {
            clsDatos datos = new clsDatos();

            var taskOrdenes = datos.cargarOrdenesAsync();
            var taskEmpleados = datos.cargarEmpleadosAsync();
            var taskClientes = datos.cargarClientesAsync();

            await Task.WhenAll(taskOrdenes, taskEmpleados, taskClientes);

            var ordenes = await taskOrdenes;
            var empleados = await taskEmpleados;
            var clientes = await taskClientes;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                actListas.IsVisible = false;

                _allOrdenes = ordenes;
                _allEmpleados = empleados;
                _allClientes = clientes;

                _allListas     = ordenes.Where(o => o.fecha_listo != DateTime.MinValue).OrderBy(o => o.fecha_listo).ToList();
                _allDomicilios = ordenes.Where(o => o.es_domicilio).OrderBy(o => o.fecha_entrega_a).ToList();
                _allMorosos    = ordenes.Where(o => o.es_pendiente).OrderBy(o => o.fecha_entrega_a).ToList();

                // KPIs
                lblKpiListas.Text    = _allListas.Count.ToString();
                lblKpiDomicilio.Text = _allDomicilios.Count.ToString();
                lblKpiMorosos.Text   = _allMorosos.Count.ToString();

                AplicarFiltros();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error CargarOperacionesAsync: " + ex.Message);
            MainThread.BeginInvokeOnMainThread(() => actListas.IsVisible = false);
        }
    }

    // ── SECCIÓN 1: Órdenes Listas ─────────────────────────────────────────
    private void PoblarOrdenesListas(List<clsOrden> listas)
    {
        containerListas.Children.Clear();
        panelSinListas.IsVisible = listas.Count == 0;

        foreach (var orden in listas)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#E2E8F0"), // Gris claro
                Padding = new Thickness(14, 12)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                ColumnSpacing = 12
            };

            // Icono tipo entrega
            var iconBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#064E3B"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                WidthRequest = 44, HeightRequest = 44,
                StrokeThickness = 0, VerticalOptions = LayoutOptions.Center
            };
            iconBorder.Content = new Label { Text = orden.tipo_entrega_icono, FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

            // Info central
            var info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 3 };
            string nombreCliente = _allClientes.FirstOrDefault(c => c.id == orden.id_cliente)?.nombre ?? "Desconocido";
            info.Add(new Label { Text = $"Orden #{orden.id_orden} — Cliente #{orden.id_cliente} ({nombreCliente})", TextColor = Color.FromArgb("#1E293B"), FontSize = 13, FontAttributes = FontAttributes.Bold });
            info.Add(new Label { Text = $"🏁 Lista: {orden.fecha_listo_corta}", TextColor = Color.FromArgb("#059669"), FontSize = 11 });
            info.Add(new Label { Text = $"📅 Entrega: {orden.fecha_entrega_corta}  {orden.tipo_entrega_texto}", TextColor = Color.FromArgb("#64748B"), FontSize = 11 });

            // Monto
            var monto = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End, Spacing = 3 };
            monto.Add(new Label { Text = $"${orden.total:0}", TextColor = Color.FromArgb("#1E293B"), FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End });
            var badge = new Border
            {
                BackgroundColor = Color.FromArgb("#064E3B"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(6, 2), StrokeThickness = 0
            };
            badge.Content = new Label { Text = "LISTA", TextColor = Color.FromArgb("#34D399"), FontSize = 9, FontAttributes = FontAttributes.Bold };
            monto.Add(badge);

            grid.Add(iconBorder);   Grid.SetColumn(iconBorder, 0);
            grid.Add(info);         Grid.SetColumn(info, 1);
            grid.Add(monto);        Grid.SetColumn(monto, 2);

            card.Content = grid;
            containerListas.Children.Add(card);
        }
    }

    // ── SECCIÓN 2: Entregas a Domicilio ──────────────────────────────────
    private void PoblarDomicilios(List<clsOrden> domicilios, List<clsEmpleado> empleados)
    {
        containerDomicilio.Children.Clear();
        panelSinDomicilio.IsVisible = domicilios.Count == 0;

        foreach (var orden in domicilios)
        {
            // Buscar el repartidor asignado
            var repartidor = empleados.FirstOrDefault(e => e.id_empleado == orden.id_empleado);
            string nomRep = repartidor?.nombre_corto ?? $"Empleado #{orden.id_empleado}";

            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#E2E8F0"),
                Padding = new Thickness(14, 12)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                ColumnSpacing = 12
            };

            var iconBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#1E3A5F"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                WidthRequest = 44, HeightRequest = 44,
                StrokeThickness = 0, VerticalOptions = LayoutOptions.Center
            };
            iconBorder.Content = new Label { Text = "🚚", FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

            var info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 3 };
            string nombreCliente = _allClientes.FirstOrDefault(c => c.id == orden.id_cliente)?.nombre ?? "Desconocido";
            info.Add(new Label { Text = $"Orden #{orden.id_orden} — Cliente #{orden.id_cliente} ({nombreCliente})", TextColor = Color.FromArgb("#1E293B"), FontSize = 13, FontAttributes = FontAttributes.Bold });
            info.Add(new Label { Text = $"👤 Repartidor: {nomRep}", TextColor = Color.FromArgb("#2563EB"), FontSize = 11 });
            info.Add(new Label { Text = $"🕐 Entrega programada: {orden.fecha_entrega_corta}", TextColor = Color.FromArgb("#64748B"), FontSize = 11 });

            var monto = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End, Spacing = 3 };
            monto.Add(new Label { Text = $"${orden.total:0}", TextColor = Color.FromArgb("#1E293B"), FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End });

            var badgePago = new Border
            {
                BackgroundColor = Color.FromArgb(orden.es_pendiente ? "#451A03" : "#1E3A5F"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(6, 2), StrokeThickness = 0
            };
            badgePago.Content = new Label
            {
                Text = orden.es_pendiente ? "PENDIENTE" : "PAGADO",
                TextColor = Color.FromArgb(orden.es_pendiente ? "#F87171" : "#60A5FA"),
                FontSize = 9, FontAttributes = FontAttributes.Bold
            };
            monto.Add(badgePago);

            grid.Add(iconBorder); Grid.SetColumn(iconBorder, 0);
            grid.Add(info);       Grid.SetColumn(info, 1);
            grid.Add(monto);      Grid.SetColumn(monto, 2);

            card.Content = grid;
            containerDomicilio.Children.Add(card);
        }
    }

    // ── SECCIÓN 3: Morosos (saldo pendiente) ─────────────────────────────
    private void PoblarMorosos(List<clsOrden> morosos)
    {
        containerMorosos.Children.Clear();
        panelSinMorosos.IsVisible = morosos.Count == 0;

        foreach (var orden in morosos)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#E2E8F0"),
                Padding = new Thickness(14, 12)
            };

            var mainStack = new VerticalStackLayout { Spacing = 10 };

            // Fila superior: Badge + ID
            var topRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };

            var alertBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#451A03"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(8, 3), StrokeThickness = 0, HorizontalOptions = LayoutOptions.Start
            };
            var badgeRow = new HorizontalStackLayout { Spacing = 6 };
            badgeRow.Add(new Microsoft.Maui.Controls.Shapes.Ellipse { WidthRequest = 6, HeightRequest = 6, Fill = Color.FromArgb("#F87171"), VerticalOptions = LayoutOptions.Center });
            badgeRow.Add(new Label { Text = "SALDO PENDIENTE", TextColor = Color.FromArgb("#F87171"), FontSize = 10, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center });
            alertBadge.Content = badgeRow;

            var idLabel = new Label { Text = $"Orden #{orden.id_orden}", TextColor = Color.FromArgb("#94A3B8"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center };
            topRow.Add(alertBadge); Grid.SetColumn(alertBadge, 0);
            topRow.Add(idLabel);    Grid.SetColumn(idLabel, 1);

            // Descripción
            var desc = new VerticalStackLayout { Spacing = 4 };
            string nombreCliente = _allClientes.FirstOrDefault(c => c.id == orden.id_cliente)?.nombre ?? "Desconocido";
            desc.Add(new Label { Text = $"Cliente #{orden.id_cliente} ({nombreCliente}) — Pago sin liquidar", TextColor = Color.FromArgb("#1E293B"), FontSize = 15, FontAttributes = FontAttributes.Bold });
            desc.Add(new Label { Text = $"Monto pendiente: ${orden.total:0.00} MXN  •  Ingresó: {orden.fecha_ingreso_corta}", TextColor = Color.FromArgb("#475569"), FontSize = 12, LineBreakMode = LineBreakMode.WordWrap });

            // Separador
            var sep = new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9"), Margin = new Thickness(0, 2) };

            // Fila inferior: tiempo + acción
            var botRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
            var timeRow = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            timeRow.Add(new Label { Text = "📅", FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
            timeRow.Add(new Label { Text = $"Entrega: {orden.fecha_entrega_corta}", TextColor = Color.FromArgb("#94A3B8"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center });

            var actionBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#064E3B"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(14, 8), StrokeThickness = 0
            };
            actionBtn.Content = new Label { Text = "GESTIONAR", TextColor = Color.FromArgb("#34D399"), FontSize = 11, FontAttributes = FontAttributes.Bold };

            botRow.Add(timeRow);   Grid.SetColumn(timeRow, 0);
            botRow.Add(actionBtn); Grid.SetColumn(actionBtn, 1);

            mainStack.Add(topRow);
            mainStack.Add(desc);
            mainStack.Add(sep);
            mainStack.Add(botRow);

            card.Content = mainStack;
            containerMorosos.Children.Add(card);
        }
    }

    // ── SECCIÓN 4: Productividad ─────────────────────────────────────────
    private void PoblarProductividad(List<clsOrden> ordenes, List<clsEmpleado> empleados)
    {
        containerProductividad.Children.Clear();

        // Agrupar órdenes por empleado
        var grupos = ordenes
            .Where(o => o.id_empleado > 0)
            .GroupBy(o => o.id_empleado)
            .Select(g => new { id = g.Key, count = g.Count(), total = g.Sum(o => o.total) })
            .OrderByDescending(g => g.count)
            .ToList();

        int maxCount = grupos.Any() ? grupos.Max(g => g.count) : 1;

        bool primero = true;
        foreach (var grupo in grupos)
        {
            var emp = empleados.FirstOrDefault(e => e.id_empleado == grupo.id);
            string nombre = emp?.nombre_corto ?? $"Empleado #{grupo.id}";
            string rol    = emp?.rol ?? "";

            if (!primero)
            {
                containerProductividad.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#1E293B"), Margin = new Thickness(0, 2) });
            }
            primero = false;

            var row = new VerticalStackLayout { Spacing = 6 };

            // Nombre + conteo
            var nameRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
            var lblNombre = new Label { Text = nombre, TextColor = Colors.White, FontSize = 13, FontAttributes = FontAttributes.Bold };
            var lblCount  = new Label { Text = $"{grupo.count} orden(es)", TextColor = Color.FromArgb("#94A3B8"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center };
            nameRow.SetColumn(lblCount, 1);
            nameRow.Add(lblNombre);
            nameRow.Add(lblCount);

            // Rol
            var rolLabel = new Label { Text = rol, TextColor = Color.FromArgb("#64748B"), FontSize = 11 };

            // Barra de progreso
            var barBg = new Border
            {
                BackgroundColor = Color.FromArgb("#1E293B"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                HeightRequest = 6,
                StrokeThickness = 0
            };

            double progreso = maxCount > 0 ? (double)grupo.count / maxCount : 0;
            // Use a ProgressBar for simplicity
            var progBar = new ProgressBar { Progress = progreso, ProgressColor = Color.FromArgb("#00E676"), HeightRequest = 6, BackgroundColor = Color.FromArgb("#1E293B") };

            row.Add(nameRow);
            row.Add(rolLabel);
            row.Add(progBar);

            containerProductividad.Children.Add(row);
        }

        if (!grupos.Any())
        {
            containerProductividad.Children.Add(new Label { Text = "Sin datos de productividad disponibles", TextColor = Color.FromArgb("#64748B"), FontSize = 12, HorizontalOptions = LayoutOptions.Center });
        }
    }

    // ── FILTROS Y BÚSQUEDA ────────────────────────────────────────────────
    private void AplicarFiltros()
    {
        string term = searchBar.Text?.Trim().ToLower() ?? "";
        string filtro = pickerFiltro.SelectedItem?.ToString() ?? "Todas";

        bool esNumero = int.TryParse(term, out int idBuscado);

        Func<clsOrden, bool> matchFiltro = o => 
        {
            if (string.IsNullOrEmpty(term)) return true;
            if (esNumero) return o.id_cliente == idBuscado;
            
            string nombreCli = _allClientes.FirstOrDefault(c => c.id == o.id_cliente)?.nombre?.ToLower() ?? "";
            return nombreCli.Contains(term);
        };

        // Filtrar por ID de cliente o Nombre
        var listasFiltradas = _allListas.Where(matchFiltro).ToList();
        var domiciliosFiltrados = _allDomicilios.Where(matchFiltro).ToList();
        var morososFiltrados = _allMorosos.Where(matchFiltro).ToList();
        var ordenesFiltradas = _allOrdenes.Where(matchFiltro).ToList();

        // 1. Ocultar todo primero
        secListas.IsVisible = false;
        secDomicilio.IsVisible = false;
        secMorosos.IsVisible = false;
        secProductividad.IsVisible = false;

        // 2. Mostrar según filtro
        if (filtro == "Todas" || filtro == "Listas")
        {
            secListas.IsVisible = true;
            lblContListas.Text = $"{listasFiltradas.Count} orden(es)";
            PoblarOrdenesListas(listasFiltradas);
        }

        if (filtro == "Todas" || filtro == "Domicilio")
        {
            secDomicilio.IsVisible = true;
            lblContDomicilio.Text = $"{domiciliosFiltrados.Count} entrega(s)";
            PoblarDomicilios(domiciliosFiltrados, _allEmpleados);
        }

        if (filtro == "Todas" || filtro == "Morosos")
        {
            secMorosos.IsVisible = true;
            lblContMorosos.Text = morososFiltrados.Count > 0 ? $"{morososFiltrados.Count} pendiente(s)" : "";
            PoblarMorosos(morososFiltrados);
        }

        if (filtro == "Todas" || filtro == "Productividad")
        {
            secProductividad.IsVisible = true;
            PoblarProductividad(ordenesFiltradas, _allEmpleados);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void OnFiltroChanged(object sender, EventArgs e)
    {
        AplicarFiltros();
    }

    // ── NAVEGACIÓN ────────────────────────────────────────────────────────
    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }

    private async void OnInventarioTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GesInv());
    }

    private void OnAlertasTapped(object sender, EventArgs e)
    {
        // Ya estás aquí
    }

    private async void OnAnaliticaTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnaliticRen());
    }
}