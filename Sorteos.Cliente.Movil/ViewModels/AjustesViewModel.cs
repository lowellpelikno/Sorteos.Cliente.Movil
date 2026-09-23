using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    /// <summary>
    /// ViewModel para la administracion de ajustes generales del sistema, datos comerciales de pago,
    /// gestion de permisos de hardware (camara y almacenamiento) y seguridad por PIN.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Centralizacion de Informacion Comercial: Permite capturar y mantener actualizados los datos bancarios
    ///   y notas de apartado que se inyectan en los mensajes para clientes via WhatsApp, ahorrando tipeo repetitivo.
    /// - Transparencia en Permisos: Informa con claridad el estado actual de cada permiso (Concedido/Denegado)
    ///   y orienta al usuario para habilitarlos desde los ajustes del sistema operativo si fueron denegados previamente.
    /// - Gestion Guiada de Seguridad: La configuracion del PIN se realiza a traves de un cuadro modal asistido
    ///   con doble captura para evitar bloqueos accidentales por errores tipograficos.
    /// - Informacion de Versionado: Muestra visiblemente la version y compilacion para soporte tecnico rapido.
    /// </remarks>
    public partial class AjustesViewModel : BaseViewModel
    {
        private readonly IConfiguracionNegocioService _negocioService;
        private readonly IPermisosService _permisosService;
        private readonly IAuthService _authService;

        // Datos del Negocio

        /// <summary>
        /// Obtiene o establece el nombre publico del negocio organizador.
        /// </summary>
        [ObservableProperty]
        public partial string NombreNegocio { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el telefono principal de atencion y contacto para los participantes.
        /// </summary>
        [ObservableProperty]
        public partial string TelefonoContacto { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece la entidad bancaria donde se reciben las transferencias o depositos.
        /// </summary>
        [ObservableProperty]
        public partial string BancoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece la clave bancaria estandarizada (CLABE) de 18 digitos.
        /// </summary>
        [ObservableProperty]
        public partial string CuentaClabe { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el numero de tarjeta para pagos o depositos rapidos.
        /// </summary>
        [ObservableProperty]
        public partial string NumeroTarjeta { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el nombre completo del titular de la cuenta receptora.
        /// </summary>
        [ObservableProperty]
        public partial string TitularCuenta { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece las politicas breves o instrucciones de apartado que se envian a los compradores.
        /// </summary>
        [ObservableProperty]
        public partial string NotasApartado { get; set; } = string.Empty;

        // Permisos

        /// <summary>
        /// Obtiene o establece si el permiso de acceso a la camara ha sido concedido por el usuario.
        /// </summary>
        [ObservableProperty]
        public partial bool CamaraConcedida { get; set; }

        /// <summary>
        /// Obtiene o establece el texto descriptivo del estado del permiso de camara para visualizacion accesible en la UI.
        /// </summary>
        [ObservableProperty]
        public partial string CamaraEstadoTexto { get; set; } = "Sin verificar";

        /// <summary>
        /// Obtiene o establece si el permiso de seleccion en galeria ha sido concedido.
        /// </summary>
        [ObservableProperty]
        public partial bool GaleriaConcedida { get; set; }

        /// <summary>
        /// Obtiene o establece el texto descriptivo del estado del permiso de galeria para la interfaz de usuario.
        /// </summary>
        [ObservableProperty]
        public partial string GaleriaEstadoTexto { get; set; } = "Sin verificar";

        // Seguridad / PIN

        /// <summary>
        /// Obtiene o establece si la aplicacion debe solicitar el PIN de seguridad al iniciar sesion.
        /// </summary>
        [ObservableProperty]
        public partial bool RequierePin { get; set; }

        /// <summary>
        /// Obtiene o establece si el usuario ya cuenta con un PIN registrado en el almacenamiento seguro.
        /// </summary>
        [ObservableProperty]
        public partial bool TienePin { get; set; }

        /// <summary>
        /// Obtiene o establece la visibilidad del modal de dialogo para definir o modificar el PIN.
        /// </summary>
        [ObservableProperty]
        public partial bool MostrarModalCambiarPin { get; set; }

        /// <summary>
        /// Obtiene o establece el nuevo PIN capturado en el formulario modal.
        /// </summary>
        [ObservableProperty]
        public partial string NuevoPin { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece la confirmacion repetida del PIN para cotejo y prevencion de errores.
        /// </summary>
        [ObservableProperty]
        public partial string ConfirmarPin { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece la cadena formateada con la version y compilacion actual de la aplicacion.
        /// </summary>
        [ObservableProperty]
        public partial string VersionAppTexto { get; set; } = string.Empty;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AjustesViewModel"/> inyectando dependencias operativas y datos de version.
        /// </summary>
        /// <param name="negocioService">Servicio de persistencia de datos comerciales.</param>
        /// <param name="permisosService">Servicio de consulta y solicitud de permisos nativos.</param>
        /// <param name="authService">Servicio de seguridad y administracion de PIN.</param>
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

        /// <summary>
        /// Carga asincrona inicial de los parametros comerciales, estatus de permisos y configuracion de PIN.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Muestra estado de carga via <see cref="BaseViewModel.IsBusy"/> y presenta avisos comprensibles
        /// si algun componente de configuracion no pudo ser leido, sin interrumpir la navegacion del usuario.
        /// </remarks>
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

        /// <summary>
        /// Comando para persistir los cambios introducidos en la informacion comercial y de pago del negocio.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Protege la operacion contra doble envio mediante <see cref="BaseViewModel.IsBusy"/> y muestra
        /// una confirmacion emergente clara cuando los datos han sido almacenados exitosamente.
        /// </remarks>
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

        /// <summary>
        /// Comando para solicitar interactivamente al usuario el permiso de camara del dispositivo.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Si el usuario deniega el permiso, despliega un dialogo educativo explicando el motivo funcional
        /// (captura de comprobantes de pago) y como reactivarlo en la configuracion nativa.
        /// </remarks>
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

        /// <summary>
        /// Comando para solicitar interactivamente al usuario el permiso de seleccion en la galeria de imagenes.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Si el permiso es denegado, ofrece explicacion constructiva orientada a seleccionar comprobantes existentes.
        /// </remarks>
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

        /// <summary>
        /// Comando de enlace directo hacia la pantalla de ajustes de la aplicacion en el sistema operativo.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Evita que el usuario tenga que navegar manualmente entre los menus del sistema operativo
        /// para resolver un permiso bloqueado.
        /// </remarks>
        [RelayCommand]
        private void AbrirConfiguracionSistema()
        {
            _permisosService.AbrirConfiguracionSistema();
        }

        /// <summary>
        /// Consulta el estado actual de los permisos del sistema sin solicitar accion al usuario.
        /// </summary>
        private async Task ActualizarEstadoPermisosAsync()
        {
            PermissionStatus camara = await _permisosService.VerificarPermisoCamaraAsync();
            CamaraConcedida = camara == PermissionStatus.Granted;
            CamaraEstadoTexto = CamaraConcedida ? "Concedido" : (camara == PermissionStatus.Denied ? "Denegado" : "No solicitado");

            PermissionStatus galeria = await _permisosService.VerificarPermisoGaleriaAsync();
            GaleriaConcedida = galeria == PermissionStatus.Granted;
            GaleriaEstadoTexto = GaleriaConcedida ? "Concedido" : (galeria == PermissionStatus.Denied ? "Denegado" : "No solicitado");
        }

        /// <summary>
        /// Comando para activar o desactivar la obligatoriedad del PIN de seguridad.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Si el usuario intenta activar la seguridad pero no ha configurado un PIN previo,
        /// abre de inmediato el modal de definicion; si decide desactivarlo, solicita confirmacion y muestra retroalimentacion.
        /// </remarks>
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

        /// <summary>
        /// Comando para presentar el dialogo modal de captura de nuevo PIN de seguridad.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Inicializa los campos en blanco para garantizar privacidad y prevenir capturas previas residuales.
        /// </remarks>
        [RelayCommand]
        private void AbrirModalCambiarPin()
        {
            NuevoPin = string.Empty;
            ConfirmarPin = string.Empty;
            MostrarModalCambiarPin = true;
        }

        /// <summary>
        /// Comando para cancelar o cerrar el dialogo modal de PIN sin registrar cambios.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Restablece el interruptor de seguridad a su estado previo si el usuario no contaba con PIN establecido.
        /// </remarks>
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

        /// <summary>
        /// Valida y registra el nuevo PIN en el almacenamiento de seguridad criptografica.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Previene errores verificando que conste de exactamente 4 digitos y que la confirmacion coincida,
        /// alertando al usuario en lenguaje claro antes de persistir.
        /// </remarks>
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

        /// <summary>
        /// Comando para consultar los terminos y condiciones desde la pantalla de ajustes.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Permite consultar las politicas y condiciones del servicio en cualquier momento.
        /// </remarks>
        [RelayCommand]
        private async Task VerTerminosAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.TerminosCondicionesPage));
        }

        /// <summary>
        /// Comando para retornar a la pantalla previa mediante navegacion Shell.
        /// </summary>
        [RelayCommand]
        private async Task VolverAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

