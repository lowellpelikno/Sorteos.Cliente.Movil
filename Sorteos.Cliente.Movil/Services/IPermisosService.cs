namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato de abstraccion para la consulta y solicitud de permisos nativos de hardware y almacenamiento en el dispositivo.
    /// </summary>
    public interface IPermisosService
    {
        /// <summary>
        /// Comprueba el estado actual del permiso de acceso a la camara sin interactuar con el usuario.
        /// </summary>
        /// <returns>Estado actual del permiso (<see cref="PermissionStatus"/>).</returns>
        Task<PermissionStatus> VerificarPermisoCamaraAsync();

        /// <summary>
        /// Solicita interactivamente al usuario el permiso para utilizar la camara fotografica.
        /// </summary>
        /// <returns>Resultado concedido, denegado o restringido tras la respuesta del usuario.</returns>
        Task<PermissionStatus> SolicitarPermisoCamaraAsync();

        /// <summary>
        /// Comprueba el estado actual del permiso de lectura en la galeria de imagenes.
        /// </summary>
        /// <returns>Estado actual del permiso (<see cref="PermissionStatus"/>).</returns>
        Task<PermissionStatus> VerificarPermisoGaleriaAsync();

        /// <summary>
        /// Solicita interactivamente el acceso a la galeria de fotografias del sistema.
        /// </summary>
        /// <returns>Resultado concedido, denegado o restringido tras la respuesta del usuario.</returns>
        Task<PermissionStatus> SolicitarPermisoGaleriaAsync();

        /// <summary>
        /// Comprueba el estado actual del permiso de lectura y escritura en almacenamiento externo.
        /// </summary>
        /// <returns>Estado actual del permiso (<see cref="PermissionStatus"/>).</returns>
        Task<PermissionStatus> VerificarPermisoAlmacenamientoAsync();

        /// <summary>
        /// Solicita interactivamente permisos para acceder al almacenamiento del dispositivo.
        /// </summary>
        /// <returns>Resultado concedido, denegado o restringido tras la respuesta del usuario.</returns>
        Task<PermissionStatus> SolicitarPermisoAlmacenamientoAsync();

        /// <summary>
        /// Abre la seccion de ajustes de la aplicacion en el sistema operativo para permitir al usuario cambiar permisos denegados.
        /// </summary>
        void AbrirConfiguracionSistema();
    }
}

