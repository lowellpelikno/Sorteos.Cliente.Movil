using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    /// <summary>
    /// ViewModel encargado de la presentacion, consulta y aceptacion de los terminos legales y politicas del servicio.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Modo Dual (Primer Arranque vs Consulta): Adapta la interfaz distinguiendo si el usuario esta en su primer acceso
    ///   (flujo de onboarding con obligatoriedad de aceptacion) o en consulta posterior (modo lectura informativa con boton de regreso).
    /// - Control de Consentimiento Claro: El boton de continuacion permanece inhabilitado hasta que el usuario activa explicitamente
    ///   la casilla de verificacion de lectura y aceptacion.
    /// - Transicion Fluida de Bienvenida: Tras la aceptacion inicial, encamina transparentemente al usuario hacia la pantalla
    ///   de autenticacion o directamente al panel principal segun las preferencias de seguridad configuradas.
    /// </remarks>
    public partial class TerminosCondicionesViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider? _serviceProvider;

        /// <summary>
        /// Obtiene o establece el estado de aceptacion explicta de los terminos y condiciones por parte del usuario.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Dispara la reevaluacion de <see cref="AceptarYContinuarCommand"/> para habilitar el boton de accion.
        /// </remarks>
        [ObservableProperty]
        public partial bool AceptoTerminos { get; set; }

        /// <summary>
        /// Obtiene o establece si la vista se abrio como pantalla de bienvenida obligatoria durante el primer arranque.
        /// </summary>
        [ObservableProperty]
        public partial bool EsPrimerArranque { get; set; }

        /// <summary>
        /// Obtiene o establece si la vista se abrio en modo de consulta informativa desde Ajustes o Login.
        /// </summary>
        [ObservableProperty]
        public partial bool EsConsulta { get; set; }

        /// <summary>
        /// Propiedad de validacion que determina si el boton de confirmacion puede ser presionado.
        /// </summary>
        public bool PuedeAceptar => AceptoTerminos;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TerminosCondicionesViewModel"/> detectando el estado legal previo.
        /// </summary>
        /// <param name="authService">Servicio de gestion de estado de autenticacion y politicas.</param>
        /// <param name="serviceProvider">Proveedor de servicios para resolucion de paginas de arranque.</param>
        public TerminosCondicionesViewModel(IAuthService authService, IServiceProvider? serviceProvider = null)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            Title = "Términos y Condiciones";

            bool aceptados = _authService.HaAceptadoTerminos();
            EsPrimerArranque = !aceptados;
            EsConsulta = aceptados;
            AceptoTerminos = aceptados;
        }

        /// <summary>
        /// Notifica el cambio de disponibilidad del comando de confirmacion cuando el usuario altera la casilla.
        /// </summary>
        partial void OnAceptoTerminosChanged(bool value)
        {
            AceptarYContinuarCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Comando para alternar la casilla de aceptacion mediante toque directo en la fila o texto correspondiente.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Amplia el area tactil util para no obligar al usuario a atinar exclusivamente a la casilla pequena.
        /// </remarks>
        [RelayCommand]
        private void AlternarAceptoTerminos()
        {
            if (!EsPrimerArranque) return;
            AceptoTerminos = !AceptoTerminos;
        }

        /// <summary>
        /// Registra la aceptacion formal de las politicas y conduce al usuario a la experiencia principal de la aplicacion.
        /// </summary>
        /// <remarks>
        /// Usabilidad: En primer arranque reemplaza la pagina raiz de la ventana para impedir volver atras a los terminos no aceptados.
        /// En modo consulta, regresa ordenadamente a la pantalla previa.
        /// </remarks>
        [RelayCommand(CanExecute = nameof(PuedeAceptar))]
        private async Task AceptarYContinuarAsync()
        {
            if (!AceptoTerminos) return;

            _authService.AceptarTerminos();

            if (EsPrimerArranque)
            {
                if (Application.Current != null && Application.Current.Windows.Count > 0)
                {
                    bool requierePin = await _authService.RequiereAutenticacionAsync();
                    Page siguientePagina = (_serviceProvider != null && requierePin)
                        ? _serviceProvider.GetRequiredService<Views.LoginPage>()
                        : new AppShell();

                    Application.Current.Windows[0].Page = siguientePagina;
                }
                else if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//MainPage");
                }
            }
            else
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("..");
                }
            }
        }

        /// <summary>
        /// Comando para volver a la pantalla anterior sin realizar modificaciones.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Proporciona salida limpia e intuitiva en modo consulta.
        /// </remarks>
        [RelayCommand]
        private async Task VolverAsync()
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
