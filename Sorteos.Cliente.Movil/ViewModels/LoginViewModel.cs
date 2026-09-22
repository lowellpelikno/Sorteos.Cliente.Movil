using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private string _pinActual = string.Empty;

        [ObservableProperty]
        public partial bool Digito1Lleno { get; set; }

        [ObservableProperty]
        public partial bool Digito2Lleno { get; set; }

        [ObservableProperty]
        public partial bool Digito3Lleno { get; set; }

        [ObservableProperty]
        public partial bool Digito4Lleno { get; set; }

        [ObservableProperty]
        public partial string MensajeError { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool MostrarError { get; set; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Acceso Seguro";
        }

        [RelayCommand]
        private async Task IngresarDigitoAsync(string? digito)
        {
            if (string.IsNullOrWhiteSpace(digito) || _pinActual.Length >= 4) return;

            MostrarError = false;
            MensajeError = string.Empty;

            _pinActual += digito;
            ActualizarEstadoCirculos();

            if (_pinActual.Length == 4)
            {
                await ValidarPinIngresadoAsync();
            }
        }

        [RelayCommand]
        private void BorrarDigito()
        {
            if (_pinActual.Length > 0)
            {
                _pinActual = _pinActual[..^1];
                MostrarError = false;
                MensajeError = string.Empty;
                ActualizarEstadoCirculos();
            }
        }

        [RelayCommand]
        private void LimpiarPin()
        {
            _pinActual = string.Empty;
            MostrarError = false;
            MensajeError = string.Empty;
            ActualizarEstadoCirculos();
        }

        private async Task ValidarPinIngresadoAsync()
        {
            IsBusy = true;
            try
            {
                bool esValido = await _authService.ValidarPinAsync(_pinActual);
                if (esValido)
                {
                    if (Application.Current != null && Application.Current.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                }
                else
                {
                    MostrarError = true;
                    MensajeError = "PIN incorrecto. Por favor, intenta de nuevo.";
                    _pinActual = string.Empty;
                    ActualizarEstadoCirculos();
                }
            }
            catch (Exception)
            {
                MostrarError = true;
                MensajeError = "Ocurrió un problema al verificar el PIN. Intenta nuevamente.";
                _pinActual = string.Empty;
                ActualizarEstadoCirculos();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ActualizarEstadoCirculos()
        {
            Digito1Lleno = _pinActual.Length >= 1;
            Digito2Lleno = _pinActual.Length >= 2;
            Digito3Lleno = _pinActual.Length >= 3;
            Digito4Lleno = _pinActual.Length >= 4;
        }

        [RelayCommand]
        private async Task VerTerminosAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.TerminosCondicionesPage));
        }
    }
}

