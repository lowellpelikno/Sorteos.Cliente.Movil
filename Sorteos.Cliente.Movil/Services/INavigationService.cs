namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato para la navegacion desacoplada entre pantallas de la aplicacion MAUI Shell.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navega hacia la ruta relativa o absoluta especificada en el esquema de Shell.
        /// </summary>
        /// <param name="route">Ruta registrada de navegacion.</param>
        Task GoToAsync(string route);

        /// <summary>
        /// Navega hacia una ruta transfiriendo un diccionario estructurado de parametros de navegacion.
        /// </summary>
        /// <param name="route">Ruta destino.</param>
        /// <param name="parameters">Diccionario con objetos o identificadores a transferir.</param>
        Task GoToAsync(string route, IDictionary<string, object> parameters);

        /// <summary>
        /// Retorna a la pantalla inmediatamente anterior en la pila de navegacion Shell ("..").
        /// </summary>
        Task PopAsync();
    }
}

