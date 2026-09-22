using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class TerminosCondicionesViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider? _serviceProvider;

        [ObservableProperty]
        public partial bool AceptoTerminos { get; set; }

        [ObservableProperty]
        public partial bool EsPrimerArranque { get; set; }

        [ObservableProperty]
        public partial bool EsConsulta { get; set; }

        public bool PuedeAceptar => AceptoTerminos;

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

        partial void OnAceptoTerminosChanged(bool value)
        {
            AceptarYContinuarCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void AlternarAceptoTerminos()
        {
            if (!EsPrimerArranque) return;
            AceptoTerminos = !AceptoTerminos;
        }

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
