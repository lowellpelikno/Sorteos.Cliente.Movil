namespace Sorteos.Cliente.Movil
{
    public partial class AppShell : Shell
    {
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
