namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato para el servicio de autenticacion, seguridad por PIN y gestion de terminos legales.
    /// </summary>
    /// <remarks>
    /// Seguridad: Administra el resguardo de credenciales criptograficas en el almacenamiento seguro
    /// del dispositivo (KeyStore en Android / KeyChain en iOS) conforme a directivas OWASP MASVS.
    /// </remarks>
    public interface IAuthService
    {
        /// <summary>
        /// Determina de forma asincrona si existe un PIN registrado en el almacenamiento seguro.
        /// </summary>
        /// <returns><c>true</c> si el usuario ya definio un PIN previamente; de lo contrario, <c>false</c>.</returns>
        Task<bool> TienePinConfiguradoAsync();

        /// <summary>
        /// Determina si la aplicacion tiene activada la politica de bloqueo de acceso por PIN.
        /// </summary>
        /// <returns><c>true</c> si se debe presentar la pantalla de login; de lo contrario, <c>false</c>.</returns>
        Task<bool> RequiereAutenticacionAsync();

        /// <summary>
        /// Valida si el PIN capturado coincide con el hash o valor resguardado en el almacenamiento seguro.
        /// </summary>
        /// <param name="pin">Secuencia numerica ingresada por el usuario.</param>
        /// <returns><c>true</c> si el PIN coincide de forma exacta; de lo contrario, <c>false</c>.</returns>
        Task<bool> ValidarPinAsync(string pin);

        /// <summary>
        /// Registra o actualiza de forma persistente un nuevo PIN en el almacenamiento seguro.
        /// </summary>
        /// <param name="pin">Nuevo codigo numerico de 4 digitos.</param>
        Task EstablecerPinAsync(string pin);

        /// <summary>
        /// Elimina el PIN del almacenamiento seguro y desactiva el requisito de autenticacion al inicio.
        /// </summary>
        Task DesactivarPinAsync();

        /// <summary>
        /// Obtiene o establece el estado de autenticacion en memoria para la sesion activa.
        /// </summary>
        bool EstaAutenticado { get; set; }

        /// <summary>
        /// Consulta en las preferencias locales si el usuario ha aceptado los terminos y condiciones de uso.
        /// </summary>
        /// <returns><c>true</c> si los terminos fueron previamente aceptados; de lo contrario, <c>false</c>.</returns>
        bool HaAceptadoTerminos();

        /// <summary>
        /// Registra de forma permanente en las preferencias locales la aceptacion de los terminos legales.
        /// </summary>
        void AceptarTerminos();
    }
}

