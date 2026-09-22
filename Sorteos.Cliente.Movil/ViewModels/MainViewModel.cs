using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using System.Globalization;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class MainViewModel : BaseViewModel, IDisposable
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");
        private readonly ILocalDatabaseService _databaseService;

        [ObservableProperty]
        public partial DashboardResumenDto Resumen { get; set; } = new();

        [ObservableProperty]
        public partial string Saludo { get; set; } = "Bienvenido";

        [ObservableProperty]
        public partial string FechaActualTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsRefreshing { get; set; }

        public MainViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            ActualizarSaludoYFecha();

            // Suscripcion desacoplada a eventos del sistema para refresco reactivo
            WeakReferenceMessenger.Default.Register<SorteoCreadoMessage>(this, (r, m) =>
            {
                _ = InvocarEnHiloPrincipalAsync(() => CargarDashboardAsync());
            });

            WeakReferenceMessenger.Default.Register<ClienteGuardadoMessage>(this, (r, m) =>
            {
                _ = InvocarEnHiloPrincipalAsync(() => CargarDashboardAsync());
            });

            WeakReferenceMessenger.Default.Register<PremioGuardadoMessage>(this, (r, m) =>
            {
                _ = InvocarEnHiloPrincipalAsync(() => CargarDashboardAsync());
            });

            WeakReferenceMessenger.Default.Register<PremioCreadoMessage>(this, (r, m) =>
            {
                _ = InvocarEnHiloPrincipalAsync(() => CargarDashboardAsync());
            });
        }

        private static async Task InvocarEnHiloPrincipalAsync(Func<Task> accion)
        {
            try
            {
                if (MainThread.IsMainThread)
                {
                    await accion();
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(accion);
                }
            }
            catch (Exception)
            {
                // Soporte para pruebas unitarias o entornos sin Dispatcher nativo
                await accion();
            }
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            GC.SuppressFinalize(this);
        }

        public void ActualizarSaludoYFecha()
        {
            DateTime ahora = DateTime.Now;
            int hora = ahora.Hour;

            Saludo = hora switch
            {
                < 12 => "Buenos días",
                < 19 => "Buenas tardes",
                _ => "Buenas noches"
            };

            string fechaCruda = ahora.ToString("dddd, dd 'de' MMMM 'de' yyyy", CulturaEsMx);
            FechaActualTexto = char.ToUpper(fechaCruda[0], CulturaEsMx) + fechaCruda[1..];
        }

        [RelayCommand]
        public async Task CargarDashboardAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                ActualizarSaludoYFecha();

                DashboardResumenDto datos = await _databaseService.ObtenerResumenDashboardAsync();
                Resumen = datos;
            }
            catch (Exception)
            {
                // Fallo silencioso en dashboard para preservar estabilidad visual
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task RefrescarAsync()
        {
            IsRefreshing = true;
            await CargarDashboardAsync();
        }

        [RelayCommand]
        public async Task NavegarASorteosAsync()
        {
            await Shell.Current.GoToAsync("//SorteosSemanaPage");
        }

        [RelayCommand]
        public async Task NavegarACrearSorteoAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.SorteoCrearPage));
        }

        [RelayCommand]
        public async Task NavegarAClientesAsync()
        {
            await Shell.Current.GoToAsync("//ClienteListPage");
        }

        [RelayCommand]
        public async Task NavegarACrearClienteAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.ClienteFormPage));
        }

        [RelayCommand]
        public async Task NavegarAPremiosAsync()
        {
            await Shell.Current.GoToAsync("//PremioListPage");
        }

        [RelayCommand]
        public async Task NavegarAReservasSorteoDestacadoAsync()
        {
            if (!Resumen.TieneSorteoDestacado || Resumen.SorteoDestacadoId <= 0) return;

            await Shell.Current.GoToAsync($"{nameof(Views.ReservasSorteoPage)}?SorteoId={Resumen.SorteoDestacadoId}");
        }

        [RelayCommand]
        public async Task NavegarAAjustesAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.AjustesPage));
        }
    }
}

