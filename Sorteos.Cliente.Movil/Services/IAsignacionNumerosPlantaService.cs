using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato para el algoritmo de generacion de combinaciones teoricas y resolucion automatica de numeros de planta.
    /// </summary>
    public interface IAsignacionNumerosPlantaService
    {
        /// <summary>
        /// Genera la lista completa de combinaciones o boletos teoricos segun el total de numeros y oportunidades por boleto.
        /// </summary>
        /// <param name="cantidadNumeros">Total de numeros en el sorteo (ej. 100, 1000).</param>
        /// <param name="oportunidades">Cantidad de numeros que integran un mismo boleto (ej. 1, 2, 4).</param>
        /// <returns>Lista de combinaciones numericas de boletos.</returns>
        List<List<int>> GenerarCombosTeoricos(int cantidadNumeros, int oportunidades);

        /// <summary>
        /// Resuelve y asigna de forma determinista los boletos correspondientes a los clientes que poseen numeros de planta.
        /// </summary>
        /// <param name="cantidadNumeros">Total de numeros del sorteo.</param>
        /// <param name="oportunidades">Oportunidades por boleto.</param>
        /// <param name="todosNumerosPlanta">Catalogo de numeros de planta registrados.</param>
        /// <param name="clientes">Listado de clientes activos.</param>
        /// <returns>Lista de asignaciones resueltas listas para insercion en base de datos.</returns>
        List<AsignacionClienteSorteo> ResolverAsignacionesSorteo(
            int cantidadNumeros,
            int oportunidades,
            List<NumeroPlanta> todosNumerosPlanta,
            List<Clientes> clientes);
    }
}

