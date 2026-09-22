using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Sorteos.Cliente.Movil.Services;
using Sorteos.Cliente.Movil.ViewModels;
using Sorteos.Cliente.Movil.Views;

namespace Sorteos.Cliente.Movil
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Registro de Servicios (Singleton)
            builder.Services.AddSingleton<IAsignacionNumerosPlantaService, AsignacionNumerosPlantaService>();
            builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
            builder.Services.AddSingleton<IComprobanteStorageService, ComprobanteStorageService>();
            builder.Services.AddSingleton<ISorteoImagenStorageService, SorteoImagenStorageService>();
            builder.Services.AddSingleton<IConfiguracionNegocioService, ConfiguracionNegocioService>();
            builder.Services.AddSingleton<IPermisosService, PermisosService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();

            // Registro de Vistas y ViewModels (Transient)
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();

            builder.Services.AddTransient<PremioListPage>();
            builder.Services.AddTransient<PremioListViewModel>();
            builder.Services.AddTransient<PremioPage>();
            builder.Services.AddTransient<PremioViewModel>();

            builder.Services.AddTransient<ClienteListPage>();
            builder.Services.AddTransient<ClienteListViewModel>();
            builder.Services.AddTransient<ClienteFormPage>();
            builder.Services.AddTransient<ClienteFormViewModel>();

            builder.Services.AddTransient<SorteosSemanaPage>();
            builder.Services.AddTransient<SorteosSemanaViewModel>();
            builder.Services.AddTransient<SorteoCrearPage>();
            builder.Services.AddTransient<SorteoCrearViewModel>();

            builder.Services.AddTransient<ReservasSorteoPage>();
            builder.Services.AddTransient<ReservasSorteoViewModel>();

            builder.Services.AddTransient<AjustesPage>();
            builder.Services.AddTransient<AjustesViewModel>();
            builder.Services.AddTransient<TerminosCondicionesPage>();
            builder.Services.AddTransient<TerminosCondicionesViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();

            return builder.Build();
        }
    }
}
