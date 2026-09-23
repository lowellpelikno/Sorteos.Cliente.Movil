using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using System.Globalization;

namespace Sorteos.Cliente.Movil.ViewModels
{
    /// <summary>
    /// ViewModel principal que gestiona el tablero de control (Dashboard) y centro de operaciones de la aplicacion.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Vision General Inmediata: Presenta un resumen ejecutivo condensado de la operacion diaria (sorteo activo,
    ///   boletos apartados vs pagados, clientes registrados) para que el organizador conozca el estado de su negocio al instante.
    /// - Ergonomia en Accesos Rapidos: Dispone de atajos directos con un solo toque hacia las acciones mas frecuentes:
    ///   crear nuevo cliente, registrar sorteo, ver premios o acceder directamente a la cuadricula de reservas del sorteo destacado.
    /// - Saludo Personalizado y Contexto Temporal: Adapta dinamicamente el saludo segun la franja horaria local
    ///   (manana, tarde, noche) y exhibe la fecha en formato natural para generar cercania y ubicacion temporal.
    /// - Sincronizacion Reactiva Sin Parpadeos: Escucha eventos globales via <see cref="WeakReferenceMessenger"/>
    ///   para refrescar las metricas de forma automatica en cuanto se guarda un cliente, premio o sorteo en otras pantallas.
    /// - Soporte para Deslizar para Actualizar (Pull-to-Refresh): Permite al usuario refrescar manualmente cuando lo desee.
    /// </remarks>
    public partial class MainViewModel : BaseViewModel, IDisposable
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");
        private readonly ILocalDatabaseService _databaseService;

        /// <summary>
        /// Obtiene o establece el objeto con los totales agregados e indicadores clave de rendimiento (KPIs) del negocio.
        /// </summary>
        [ObservableProperty]
        public partial DashboardResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// Obtiene o establece el saludo cordial contextualizado segun la hora actual del dispositivo.
        /// </summary>
        [ObservableProperty]
        public partial string Saludo { get; set; } = "Bienvenido";

        /// <summary>
        /// Obtiene o establece la fecha actual formateada en lenguaje natural en espanol (ej. "Lunes, 23 de septiembre de 2024").
        /// </summary>
        [ObservableProperty]
        public partial string FechaActualTexto { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el estado visual del indicador de arrastre (Pull-to-Refresh).
        /// </summary>
        [ObservableProperty]
        public partial bool IsRefreshing { get; set; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MainViewModel"/> y suscribe los receptores de mensajeria desacoplada.
        /// </summary>
        /// <param name="databaseService">Servicio de acceso a la base de datos local SQLite.</param>
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

        /// <summary>
        /// Asegura la ejecucion de la accion en el hilo principal de la interfaz para evitar colisiones visuales.
        /// </summary>
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

        /// <summary>
        /// Libera las suscripciones de mensajeria global para evitar retencion de memoria al destruir la vista.
        /// </summary>
        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Calcula el saludo y la fecha actual en base al reloj del sistema para actualizar la cabecera informativa.
        /// </summary>
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

        /// <summary>
        /// Carga asincrona de las metricas del tablero desde SQLite de forma agregada y sin bloqueos de interfaz.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Mantiene la fluidez de la app gestionando <see cref="BaseViewModel.IsBusy"/> y silenciando
        /// errores transitorios de lectura para no alarmar al usuario con popups intrusivos en la pantalla de bienvenida.
        /// </remarks>
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

        /// <summary>
        /// Comando activado por el gesto Pull-to-Refresh del usuario para refrescar manualmente los datos.
        /// </summary>
        [RelayCommand]
        public async Task RefrescarAsync()
        {
            IsRefreshing = true;
            await CargarDashboardAsync();
        }

        /// <summary>
        /// Navega a la pestana o pantalla de calendario de sorteos semanales.
        /// </summary>
        [RelayCommand]
        public async Task NavegarASorteosAsync()
        {
            await Shell.Current.GoToAsync("//SorteosSemanaPage");
        }

        /// <summary>
        /// Navega directamente al formulario de alta de nuevo sorteo.
        /// </summary>
        [RelayCommand]
        public async Task NavegarACrearSorteoAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.SorteoCrearPage));
        }

        /// <summary>
        /// Navega a la lista principal de clientes registrados.
        /// </summary>
        [RelayCommand]
        public async Task NavegarAClientesAsync()
        {
            await Shell.Current.GoToAsync("//ClienteListPage");
        }

        /// <summary>
        /// Navega de inmediato al formulario para registrar un nuevo cliente.
        /// </summary>
        [RelayCommand]
        public async Task NavegarACrearClienteAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.ClienteFormPage));
        }

        /// <summary>
        /// Navega a la pestana de catalogo de premios.
        /// </summary>
        [RelayCommand]
        public async Task NavegarAPremiosAsync()
        {
            await Shell.Current.GoToAsync("//PremioListPage");
        }

        /// <summary>
        /// Acceso rapido a la cuadricula de reservas del sorteo activo o mas proximo a vencer.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Si no hay ningun sorteo vigente, la accion se ignora silenciosamente para evitar navegaciones invalidas.
        /// </remarks>
        [RelayCommand]
        public async Task NavegarAReservasSorteoDestacadoAsync()
        {
            if (!Resumen.TieneSorteoDestacado || Resumen.SorteoDestacadoId <= 0) return;

            await Shell.Current.GoToAsync($"{nameof(Views.ReservasSorteoPage)}?SorteoId={Resumen.SorteoDestacadoId}");
        }

        /// <summary>
        /// Navega a la pantalla de configuracion general y ajustes del negocio.
        /// </summary>
        [RelayCommand]
        public async Task NavegarAAjustesAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.AjustesPage));
        }
    }
}

