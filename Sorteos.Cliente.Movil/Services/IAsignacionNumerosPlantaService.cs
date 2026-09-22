using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    public interface IAsignacionNumerosPlantaService
    {
        List<List<int>> GenerarCombosTeoricos(int cantidadNumeros, int oportunidades);

        List<AsignacionClienteSorteo> ResolverAsignacionesSorteo(
            int cantidadNumeros,
            int oportunidades,
            List<NumeroPlanta> todosNumerosPlanta,
            List<Clientes> clientes);
    }
}

