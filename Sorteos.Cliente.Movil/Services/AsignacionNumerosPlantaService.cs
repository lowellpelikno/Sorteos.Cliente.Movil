using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Algoritmo para la generacion de combinaciones teoricas y distribucion de numeros de planta.
    /// </summary>
    /// <remarks>
    /// Manejo de Colisiones:
    /// Si dos clientes tienen asignados numeros de planta que coinciden en el mismo boleto teorico multicombinacion,
    /// el servicio detecta la colision, disuelve el combo y asigna exclusivamente los numeros individuales a cada cliente.
    /// </remarks>
    public class AsignacionNumerosPlantaService : IAsignacionNumerosPlantaService
    {
        /// <inheritdoc/>
        public List<List<int>> GenerarCombosTeoricos(int cantidadNumeros, int oportunidades)
        {
            List<List<int>> combos = [];

            if (cantidadNumeros <= 0) return combos;

            if (oportunidades <= 1)
            {
                for (int i = 1; i <= cantidadNumeros; i++)
                {
                    int numero = (i == cantidadNumeros) ? 0 : i;
                    combos.Add([numero]);
                }
                return combos;
            }

            int paso = cantidadNumeros / oportunidades;
            if (paso <= 0) paso = 1;

            for (int i = 1; i <= paso; i++)
            {
                List<int> grupo = [];
                for (int op = 0; op < oportunidades; op++)
                {
                    int val = i + (op * paso);
                    if (val == cantidadNumeros) val = 0; // El ultimo numero se mapea convencionalmente a 0/00
                    grupo.Add(val);
                }
                combos.Add(grupo);
            }

            // Si por configuracion quedan numeros huerfanos que no completan un grupo, se agregan como oportunidades individuales
            HashSet<int> numerosCubiertos = [];
            for (int i = 0; i < combos.Count; i++)
            {
                for (int j = 0; j < combos[i].Count; j++)
                {
                    numerosCubiertos.Add(combos[i][j]);
                }
            }

            for (int i = 1; i <= cantidadNumeros; i++)
            {
                int val = (i == cantidadNumeros) ? 0 : i;
                if (!numerosCubiertos.Contains(val))
                {
                    combos.Add([val]);
                }
            }

            return combos;
        }

        /// <inheritdoc/>
        public List<AsignacionClienteSorteo> ResolverAsignacionesSorteo(
            int cantidadNumeros,
            int oportunidades,
            List<NumeroPlanta> todosNumerosPlanta,
            List<Clientes> clientes)
        {
            List<AsignacionClienteSorteo> resultado = [];

            if (todosNumerosPlanta == null || todosNumerosPlanta.Count == 0 || cantidadNumeros <= 0)
                return resultado;

            HashSet<int> numerosValidosSorteo = [];
            for (int i = 1; i <= cantidadNumeros; i++)
            {
                numerosValidosSorteo.Add((i == cantidadNumeros) ? 0 : i);
            }

            Dictionary<int, string> dictClientes = clientes?.ToDictionary(c => c.Id, c => c.NombreCompleto) 
                               ?? [];

            Dictionary<int, List<int>> plantasPorCliente = todosNumerosPlanta
                .Where(p => p.Activo && numerosValidosSorteo.Contains(p.Numero))
                .GroupBy(p => p.IdCliente)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Numero).Distinct().ToList());

            if (plantasPorCliente.Count == 0)
                return resultado;

            // Caso 1: Sorteo de 1 sola oportunidad
            if (oportunidades <= 1)
            {
                foreach (KeyValuePair<int, List<int>> kvp in plantasPorCliente)
                {
                    resultado.Add(new AsignacionClienteSorteo
                    {
                        IdCliente = kvp.Key,
                        NombreCliente = dictClientes.GetValueOrDefault(kvp.Key, $"Cliente #{kvp.Key}"),
                        NumerosAsignados = kvp.Value,
                        EsComboCompleto = false,
                        Observaciones = "Número(s) individual(es)"
                    });
                }
                return resultado;
            }

            // Caso 2: Sorteo con combos (N oportunidades)
            List<List<int>> combosTeoricos = GenerarCombosTeoricos(cantidadNumeros, oportunidades);
            Dictionary<int, List<int>> numerosAsignadosPorCliente = [];
            Dictionary<int, (bool Completo, string Observacion)> estadosComboPorCliente = [];

            foreach (List<int> combo in combosTeoricos)
            {
                // Identificar que clientes tienen numeros de planta en este combo
                Dictionary<int, List<int>> clientesEnEsteCombo = [];

                foreach (KeyValuePair<int, List<int>> kvp in plantasPorCliente)
                {
                    int idCli = kvp.Key;
                    List<int> coincidencias = kvp.Value.Intersect(combo).ToList();
                    if (coincidencias.Count > 0)
                    {
                        clientesEnEsteCombo[idCli] = coincidencias;
                    }
                }

                if (clientesEnEsteCombo.Count > 1)
                {
                    // Colision: 2 o mas clientes tienen numeros de planta en el mismo combo.
                    // El grupo deja de existir como combo y se disuelve en numeros sueltos.
                    foreach (KeyValuePair<int, List<int>> cKvp in clientesEnEsteCombo)
                    {
                        int idCli = cKvp.Key;
                        if (!numerosAsignadosPorCliente.ContainsKey(idCli))
                            numerosAsignadosPorCliente[idCli] = [];

                        numerosAsignadosPorCliente[idCli].AddRange(cKvp.Value);
                        estadosComboPorCliente[idCli] = (false, "Colisión en combo con otro cliente (grupo disuelto, números sueltos)");
                    }
                }
                else if (clientesEnEsteCombo.Count == 1)
                {
                    // Cliente unico en el combo
                    KeyValuePair<int, List<int>> unicoCliente = clientesEnEsteCombo.First();
                    int idCli = unicoCliente.Key;

                    if (!numerosAsignadosPorCliente.ContainsKey(idCli))
                        numerosAsignadosPorCliente[idCli] = [];

                    // Se le otorga el combo teorico completo
                    numerosAsignadosPorCliente[idCli].AddRange(combo);

                    bool esCompleto = combo.Count == oportunidades;
                    string obs = esCompleto 
                        ? "Combo completo" 
                        : $"Combo parcial ({combo.Count}/{oportunidades})";

                    estadosComboPorCliente[idCli] = (esCompleto, obs);
                }
            }

            foreach (KeyValuePair<int, List<int>> kvp in numerosAsignadosPorCliente)
            {
                (bool Completo, string Observacion) estado = estadosComboPorCliente.TryGetValue(kvp.Key, out (bool Completo, string Observacion) est) 
                    ? est 
                    : (false, string.Empty);

                resultado.Add(new AsignacionClienteSorteo
                {
                    IdCliente = kvp.Key,
                    NombreCliente = dictClientes.GetValueOrDefault(kvp.Key, $"Cliente #{kvp.Key}"),
                    NumerosAsignados = kvp.Value.Distinct().OrderBy(n => n).ToList(),
                    EsComboCompleto = estado.Completo,
                    Observaciones = estado.Observacion
                });
            }

            return resultado;
        }
    }
}
