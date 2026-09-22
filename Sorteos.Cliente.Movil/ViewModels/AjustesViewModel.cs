using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class AjustesViewModel : BaseViewModel
    {
        private readonly IConfiguracionNegocioService _negocioService;
        private readonly IPermisosService _permisosService;
        private readonly IAuthService _authService;

        // Datos del Negocio
        [ObservableProperty]
        public partial string NombreNegocio { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string TelefonoContacto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string BancoNombre { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string CuentaClabe { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NumeroTarjeta { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string TitularCuenta { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NotasApartado { get; set; } = string.Empty;

        // Permisos
        [ObservableProperty]
        public partial bool CamaraConcedida { get; set; }

        [ObservableProperty]
        public partial string CamaraEstadoTexto { get; set; } = "Sin verificar";

        [ObservableProperty]
        public partial bool GaleriaConcedida { get; set; }

        [ObservableProperty]
        public partial string GaleriaEstadoTexto { get; set; } = "Sin verificar";

        // Seguridad / PIN
        [ObservableProperty]
        public partial bool RequierePin { get; set; }

        [ObservableProperty]
        public partial bool TienePin { get; set; }

        [ObservableProperty]
        public partial bool MostrarModalCambiarPin { get; set; }

        [ObservableProperty]
        public partial string NuevoPin { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ConfirmarPin { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string VersionAppTexto { get; set; } = string.Empty;

        public AjustesViewModel(
            IConfiguracionNegocioService negocioService,
            IPermisosService permisosService,
            IAuthService authService)
        {
            _negocioService = negocioService;
            _permisosService = permisosService;
            _authService = authService;

            Title = "Ajustes del Sistema";
            VersionAppTexto = $"Versión {AppInfo.Current.VersionString} (Compilación {AppInfo.Current.BuildString})";
        }

        public async Task InicializarAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                DatosNegocioDto datos = await _negocioService.ObtenerDatosNegocioAsync();

                NombreNegocio = datos.NombreNegocio;
                TelefonoContacto = datos.TelefonoContacto;
                BancoNombre = datos.BancoNombre;
                CuentaClabe = datos.CuentaClabe;
                NumeroTarjeta = datos.NumeroTarjeta;
                TitularCuenta = datos.TitularCuenta;
                NotasApartado = datos.NotasApartado;

                await ActualizarEstadoPermisosAsync();

                TienePin = await _authService.TienePinConfiguradoAsync();
                RequierePin = await _authService.RequiereAutenticacionAsync();
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible cargar algunos parámetros de configuración.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GuardarConfiguracionAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                DatosNegocioDto datos = new()
                {
                    NombreNegocio = NombreNegocio,
                    TelefonoContacto = TelefonoContacto,
                    BancoNombre = BancoNombre,
                    CuentaClabe = CuentaClabe,
                    NumeroTarjeta = NumeroTarjeta,
                    TitularCuenta = TitularCuenta,
                    NotasApartado = NotasApartado
                };

                await _negocioService.GuardarDatosNegocioAsync(datos);
                await Shell.Current.DisplayAlertAsync("Configuración guardada", "Los datos comerciales y de pago se actualizaron correctamente.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un problema al guardar la configuración.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SolicitarPermisoCamaraAsync()
        {
            PermissionStatus status = await _permisosService.SolicitarPermisoCamaraAsync();
            CamaraConcedida = status == PermissionStatus.Granted;
            CamaraEstadoTexto = CamaraConcedida ? "Concedido" : "Denegado";

            if (!CamaraConcedida)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permiso requerido",
                    "El acceso a la cámara permite capturar fotos de comprobantes de pago. Si fue denegado de forma permanente, puedes habilitarlo en los ajustes del dispositivo.",
                    "Aceptar");
            }
        }

        [RelayCommand]
        private async Task SolicitarPermisoGaleriaAsync()
        {
            PermissionStatus status = await _permisosService.SolicitarPermisoGaleriaAsync();
            GaleriaConcedida = status == PermissionStatus.Granted;
            GaleriaEstadoTexto = GaleriaConcedida ? "Concedido" : "Denegado";

            if (!GaleriaConcedida)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permiso requerido",
                    "El acceso a fotos permite seleccionar comprobantes almacenados en el teléfono. Si fue denegado de forma permanente, puedes habilitarlo en los ajustes del dispositivo.",
                    "Aceptar");
            }
        }

        [RelayCommand]
        private void AbrirConfiguracionSistema()
        {
            _permisosService.AbrirConfiguracionSistema();
        }

        private async Task ActualizarEstadoPermisosAsync()
        {
            PermissionStatus camara = await _permisosService.VerificarPermisoCamaraAsync();
            CamaraConcedida = camara == PermissionStatus.Granted;
            CamaraEstadoTexto = CamaraConcedida ? "Concedido" : (camara == PermissionStatus.Denied ? "Denegado" : "No solicitado");

            PermissionStatus galeria = await _permisosService.VerificarPermisoGaleriaAsync();
            GaleriaConcedida = galeria == PermissionStatus.Granted;
            GaleriaEstadoTexto = GaleriaConcedida ? "Concedido" : (galeria == PermissionStatus.Denied ? "Denegado" : "No solicitado");
        }

        [RelayCommand]
        private async Task AlternarRequierePinAsync()
        {
            if (RequierePin)
            {
                // Si quiere activarlo pero no tiene PIN, abrir modal para configurarlo
                if (!TienePin)
                {
                    AbrirModalCambiarPin();
                }
                else
                {
                    Preferences.Default.Set("App_Security_Pin_Activo", true);
                }
            }
            else
            {
                await _authService.DesactivarPinAsync();
                TienePin = false;
                await Shell.Current.DisplayAlertAsync("Seguridad", "El bloqueo de acceso por PIN ha sido desactivado.", "Aceptar");
            }
        }

        [RelayCommand]
        private void AbrirModalCambiarPin()
        {
            NuevoPin = string.Empty;
            ConfirmarPin = string.Empty;
            MostrarModalCambiarPin = true;
        }

        [RelayCommand]
        private void CerrarModalCambiarPin()
        {
            NuevoPin = string.Empty;
            ConfirmarPin = string.Empty;
            MostrarModalCambiarPin = false;

            if (!TienePin)
            {
                RequierePin = false;
            }
        }

        [RelayCommand]
        private async Task GuardarNuevoPinAsync()
        {
            string pin1 = NuevoPin.Trim();
            string pin2 = ConfirmarPin.Trim();

            if (pin1.Length != 4 || !pin1.All(char.IsDigit))
            {
                await Shell.Current.DisplayAlertAsync("PIN Inválido", "El PIN debe componerse exactamente de 4 dígitos numéricos.", "Aceptar");
                return;
            }

            if (!string.Equals(pin1, pin2, StringComparison.Ordinal))
            {
                await Shell.Current.DisplayAlertAsync("Discrepancia", "La confirmación no coincide con el nuevo PIN.", "Aceptar");
                return;
            }

            try
            {
                await _authService.EstablecerPinAsync(pin1);
                TienePin = true;
                RequierePin = true;
                MostrarModalCambiarPin = false;
                NuevoPin = string.Empty;
                ConfirmarPin = string.Empty;

                await Shell.Current.DisplayAlertAsync("Seguridad actualizada", "El PIN de seguridad ha sido establecido correctamente.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error al registrar el PIN de seguridad.", "Aceptar");
            }
        }

        [RelayCommand]
        private async Task VerTerminosAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.TerminosCondicionesPage));
        }

        [RelayCommand]
        private async Task VolverAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

