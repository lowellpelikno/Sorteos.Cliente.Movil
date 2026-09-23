namespace Sorteos.Cliente.Movil
{
/// <summary>
    /// Contenedor de navegacion principal de tipo <see cref="Shell"/> que registra el arbol de rutas y destinos de la aplicacion.
    /// </summary>
    public partial class AppShell : Shell
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AppShell"/> y registra las rutas estaticas de navegacion por Shell.
        /// </summary>
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(Views.PremioPage), typeof(Views.PremioPage));
            Routing.RegisterRoute(nameof(Views.ClienteFormPage), typeof(Views.ClienteFormPage));
            Routing.RegisterRoute(nameof(Views.SorteoCrearPage), typeof(Views.SorteoCrearPage));
            Routing.RegisterRoute(nameof(Views.ReservasSorteoPage), typeof(Views.ReservasSorteoPage));
            Routing.RegisterRoute(nameof(Views.AjustesPage), typeof(Views.AjustesPage));
            Routing.RegisterRoute(nameof(Views.TerminosCondicionesPage), typeof(Views.TerminosCondicionesPage));
            Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
        }
    }
}
