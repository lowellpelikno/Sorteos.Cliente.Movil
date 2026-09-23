using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    /// <summary>
    /// ViewModel responsable de la pantalla de autenticacion por PIN y acceso seguro al sistema.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Ergonomia Tactil: Esta diseñado para operar con un teclado numerico visual en pantalla con blancos tactiles amplios,
    ///   evitando el despliegue intrusivo del teclado virtual del sistema operativo.
    /// - Retroalimentacion Visual Inmediata: Los indicadores graficos de digitos (dots o circulos) se iluminan secuencialmente
    ///   conforme el usuario pulsa cada tecla, proporcionando confirmacion sensorial instantanea.
    /// - Cero Friccion en Envio: Al ingresar el cuarto digito, la validacion se desencadena automaticamente sin requerir
    ///   pulsar un boton adicional de confirmacion ("Entrar" o "Aceptar").
    /// - Recuperacion Agil ante Errores: Incluye comandos dedicados para retroceso unitario y limpieza completa del PIN,
    ///   permitiendo al usuario corregir equivocaciones con un solo toque y visualizando mensajes de error claros y no tecnicos.
    /// </remarks>
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private string _pinActual = string.Empty;

        /// <summary>
        /// Obtiene o establece el estado visual del primer indicador de digito del PIN.
        /// </summary>
        [ObservableProperty]
        public partial bool Digito1Lleno { get; set; }

        /// <summary>
        /// Obtiene o establece el estado visual del segundo indicador de digito del PIN.
        /// </summary>
        [ObservableProperty]
        public partial bool Digito2Lleno { get; set; }

        /// <summary>
        /// Obtiene o establece el estado visual del tercer indicador de digito del PIN.
        /// </summary>
        [ObservableProperty]
        public partial bool Digito3Lleno { get; set; }

        /// <summary>
        /// Obtiene o establece el estado visual del cuarto indicador de digito del PIN.
        /// </summary>
        [ObservableProperty]
        public partial bool Digito4Lleno { get; set; }

        /// <summary>
        /// Obtiene o establece el mensaje explicativo para el usuario cuando ocurre un error en la autenticacion.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Presenta texto sobrio y claro, evitando codigos de excepcion crudos.
        /// </remarks>
        [ObservableProperty]
        public partial string MensajeError { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece la visibilidad del contenedor de alerta de error en la interfaz.
        /// </summary>
        [ObservableProperty]
        public partial bool MostrarError { get; set; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="LoginViewModel"/> configurando el titulo accesible de la vista.
        /// </summary>
        /// <param name="authService">Servicio de validacion criptografica de credenciales y PIN.</param>
        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Acceso Seguro";
        }

        /// <summary>
        /// Comando invocado al pulsar una tecla numerica del teclado visual en pantalla.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Descarta entradas si el buffer ya alcanzo 4 digitos y limpia cualquier mensaje de error previo,
        /// activando la validacion automatica una vez completado el cuarto digito.
        /// </remarks>
        /// <param name="digito">Caracter numerico presionado por el usuario.</param>
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

        /// <summary>
        /// Comando de retroceso para eliminar el ultimo digito capturado.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Permite corregir una pulsacion accidental sin reiniciar la captura completa del PIN.
        /// </remarks>
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

        /// <summary>
        /// Comando de limpieza rapida que restablece por completo la captura del PIN.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Proporciona una via directa de restablecimiento si el usuario prefiere reiniciar su intento.
        /// </remarks>
        [RelayCommand]
        private void LimpiarPin()
        {
            _pinActual = string.Empty;
            MostrarError = false;
            MensajeError = string.Empty;
            ActualizarEstadoCirculos();
        }

        /// <summary>
        /// Ejecuta la verificacion de seguridad del PIN contra el almacenamiento protegido del dispositivo.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Activa <see cref="BaseViewModel.IsBusy"/> para prevenir interacciones duplicadas,
        /// redirige a la concha principal (<see cref="AppShell"/>) al autenticar correctamente o restablece
        /// los indicadores visuales desplegando una alerta comprensible si el PIN es invalido.
        /// </remarks>
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

        /// <summary>
        /// Sincroniza las banderas booleanas de los 4 indicadores graficos con la longitud actual de la secuencia ingresada.
        /// </summary>
        private void ActualizarEstadoCirculos()
        {
            Digito1Lleno = _pinActual.Length >= 1;
            Digito2Lleno = _pinActual.Length >= 2;
            Digito3Lleno = _pinActual.Length >= 3;
            Digito4Lleno = _pinActual.Length >= 4;
        }

        /// <summary>
        /// Comando de navegacion que conduce al usuario a la pantalla informativa de terminos y condiciones.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Ofrece acceso visible y directo a la documentacion legal y politicas antes de completar el ingreso.
        /// </remarks>
        [RelayCommand]
        private async Task VerTerminosAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.TerminosCondicionesPage));
        }
    }
}

