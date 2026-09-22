using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    public interface IConfiguracionNegocioService
    {
        Task<DatosNegocioDto> ObtenerDatosNegocioAsync();
        Task GuardarDatosNegocioAsync(DatosNegocioDto datos);
        Task<string> GenerarMensajeBancarioAsync();
    }
}

