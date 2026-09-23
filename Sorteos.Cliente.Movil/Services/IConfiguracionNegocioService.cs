using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato de servicio para la gestion de informacion comercial y generacion de plantillas bancarias.
    /// </summary>
    public interface IConfiguracionNegocioService
    {
        /// <summary>
        /// Recupera los datos comerciales, numeros de cuenta y notas de apartado persistidos.
        /// </summary>
        /// <returns>Objeto <see cref="DatosNegocioDto"/> con la configuracion actual del negocio.</returns>
        Task<DatosNegocioDto> ObtenerDatosNegocioAsync();

        /// <summary>
        /// Persiste de forma segura las modificaciones a los datos comerciales y cuentas receptoras.
        /// </summary>
        /// <param name="datos">Estructura con los nuevos valores de configuracion.</param>
        Task GuardarDatosNegocioAsync(DatosNegocioDto datos);

        /// <summary>
        /// Compone la plantilla estructurada de texto con datos bancarios lista para inyeccion en mensajes de WhatsApp.
        /// </summary>
        /// <returns>Texto formateado con cuentas, beneficiario y politicas de apartado.</returns>
        Task<string> GenerarMensajeBancarioAsync();
    }
}

