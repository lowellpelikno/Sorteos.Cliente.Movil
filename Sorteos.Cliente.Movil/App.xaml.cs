using Microsoft.Extensions.DependencyInjection;

namespace Sorteos.Cliente.Movil
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

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