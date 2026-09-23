using Microsoft.Extensions.DependencyInjection;

namespace Sorteos.Cliente.Movil
{
/// <summary>
    /// Clase principal de la aplicacion que orquesta el ciclo de vida general y la seleccion de la pantalla de inicio segun seguridad y terminos.
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="App"/> proveyendo el proveedor de servicios de inyeccion de dependencias.
        /// </summary>
        /// <param name="serviceProvider">Proveedor de servicios global.</param>
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Crea y configura la ventana principal de la aplicacion, evaluando politicas de terminos aceptados y solicitud de PIN.
        /// </summary>
        /// <param name="activationState">Estado de activacion del sistema operativo.</param>
        /// <returns>Instancia de <see cref="Window"/> con la pagina inicial y dimensiones correspondientes a la plataforma.</returns>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            bool terminosAceptados = Preferences.Default.Get("App_Terminos_Aceptados", false);
            bool requierePin = Preferences.Default.Get("App_Security_Pin_Activo", false)
                               && !string.IsNullOrWhiteSpace(Preferences.Default.Get("App_Security_Pin", string.Empty));

            Page paginaInicial;
            if (!terminosAceptados)
            {
                paginaInicial = _serviceProvider.GetRequiredService<Views.TerminosCondicionesPage>();
            }
            else if (requierePin)
            {
                paginaInicial = _serviceProvider.GetRequiredService<Views.LoginPage>();
            }
            else
            {
                paginaInicial = new AppShell();
            }

            Window window = new(paginaInicial);

#if WINDOWS
            const int anchoMovil = 420;
            const int altoMovil = 860;

            window.Width = anchoMovil;
            window.Height = altoMovil;
            window.MinimumWidth = anchoMovil;
            window.MinimumHeight = altoMovil;
            window.MaximumWidth = anchoMovil;
            window.MaximumHeight = altoMovil;
#endif

            return window;
        }
    }
}