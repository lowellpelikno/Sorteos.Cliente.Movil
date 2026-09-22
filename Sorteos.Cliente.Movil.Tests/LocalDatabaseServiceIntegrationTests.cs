using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
    public class LocalDatabaseServiceIntegrationTests : IAsyncLifetime
    {
        private readonly string _dbPath;
        private readonly LocalDatabaseService _dbService;

        public LocalDatabaseServiceIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"sorteos_test_{Guid.NewGuid():N}.db3");
            AsignacionNumerosPlantaService asignacionService = new();
            _dbService = new LocalDatabaseService(_dbPath, asignacionService);
        }

        public async Task InitializeAsync()
        {
            await _dbService.InitAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbService.CerrarConexionAsync();

            try
            {
                if (File.Exists(_dbPath)) File.Delete(_dbPath);
                string wal = _dbPath + "-wal";
                if (File.Exists(wal)) File.Delete(wal);
                string shm = _dbPath + "-shm";
                if (File.Exists(shm)) File.Delete(shm);
            }
            catch
            {
                // Limpieza silenciosa de archivos temporales
            }
        }

        [Fact]
        public async Task GuardarCliente_Y_GuardarNumerosPlanta_PueblaResumenYRetornaEnBusqueda()
        {
            Clientes nuevo = new()
            {
                Nombre = "Juan Carlos",
                ApellidoPaterno = "López",
                ApellidoMaterno = "Trejo",
                Telefono = "2131321233",
                Email = "trejo@punto.com",
                Activo = true
            };

            int clienteId = await _dbService.GuardarClienteAsync(nuevo);
            Assert.True(clienteId > 0);

            // Asignar numeros de planta: 7 y 42
            List<int> numeros = [7, 42];
            await _dbService.GuardarNumerosPlantaClienteAsync(clienteId, numeros);

            // Consultar clientes mediante BuscarClientesAsync (misma consulta que usa el listado)
            List<Clientes> clientes = await _dbService.BuscarClientesAsync(string.Empty, limite: 100);

            Assert.True(clientes.Count > 0);
            Clientes? encontrado = clientes.FirstOrDefault(c => c.Id == clienteId);
            Assert.NotNull(encontrado);
            Assert.Equal("07, 42", encontrado.NumerosPlantaResumen);
            Assert.True(encontrado.TieneNumerosPlanta);
        }

        [Fact]
        public async Task ExisteNumeroPlantaAsync_DetectaDuplicadosExcluyendoMismoCliente()
        {
            Clientes cliente1 = new() { Nombre = "Cliente Uno", Telefono = "1111111111", Activo = true };
            Clientes cliente2 = new() { Nombre = "Cliente Dos", Telefono = "2222222222", Activo = true };

            int idCli1 = await _dbService.GuardarClienteAsync(cliente1);
            int idCli2 = await _dbService.GuardarClienteAsync(cliente2);

            // Asignar el numero 15 al cliente 1
            await _dbService.GuardarNumerosPlantaClienteAsync(idCli1, [15]);

            // Cliente 2 no puede apartar el 15
            bool existeParaOtro = await _dbService.ExisteNumeroPlantaAsync(15, idClienteExcluir: idCli2);
            Assert.True(existeParaOtro);

            // Cliente 1 si puede mantener el 15 (se excluye a si mismo)
            bool existeParaElMismo = await _dbService.ExisteNumeroPlantaAsync(15, idClienteExcluir: idCli1);
            Assert.False(existeParaElMismo);

            // Un numero no asignado retorna false
            bool existeNumeroLibre = await _dbService.ExisteNumeroPlantaAsync(99, idClienteExcluir: idCli2);
            Assert.False(existeNumeroLibre);
        }

        [Fact]
        public async Task EliminarClienteAsync_EliminaClienteYNumerosPlantaAsociados()
        {
            Clientes cliente = new() { Nombre = "Para Eliminar", Telefono = "3333333333", Activo = true };
            int idCli = await _dbService.GuardarClienteAsync(cliente);

            await _dbService.GuardarNumerosPlantaClienteAsync(idCli, [5, 10, 15]);

            // Confirmar que existen los numeros de planta
            List<NumeroPlanta> antes = await _dbService.ObtenerNumerosPlantaPorClienteAsync(idCli);
            Assert.Equal(3, antes.Count);

            // Eliminar cliente
            int eliminados = await _dbService.EliminarClienteAsync(idCli);
            Assert.True(eliminados > 0);

            // Verificar que no exista el cliente
            Clientes? clienteConsultado = await _dbService.ObtenerClientePorIdAsync(idCli);
            Assert.Null(clienteConsultado);

            // Verificar que sus numeros de planta fueron eliminados
            List<NumeroPlanta> despues = await _dbService.ObtenerNumerosPlantaPorClienteAsync(idCli);
            Assert.Empty(despues);
        }

        [Fact]
        public async Task Sorteo_InicializarCuadricula_Y_Metricas_CalculaTotalesCorrectos()
        {
            // 1. Guardar sorteo plantilla
            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 1,
                Descripcion = "Sorteo Prueba",
                CantidadNumeros = 100,
                Oportunidades = 1,
                Costo = 50m,
                IdEstatusSorteo = 1
            };

            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, [1]);
            Assert.True(sorteoId > 0);

            // 2. Inicializar numeros
            await _dbService.InicializarNumerosSorteoAsync(sorteoId, 100);

            // 3. Registrar un cliente y apartar 2 numeros
            Clientes cliente = new() { Nombre = "Comprador", Telefono = "4444444444" };
            int clienteId = await _dbService.GuardarClienteAsync(cliente);

            int reservaId = await _dbService.ApartarNumerosMasivoAsync(
                sorteoId, clienteId, [10, 20], 100m, "Efectivo", "Comprador");

            Assert.True(reservaId > 0);

            // 4. Validar metricas
            MetricasSorteoDto metricas = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(100, metricas.TotalNumeros);
            Assert.Equal(1, metricas.TotalApartados); // Cuenta reservas en estatus 1 (Apartado)
            Assert.Equal(98, metricas.TotalDisponibles); // 100 - 2 boletos asignados a la reserva
            Assert.Equal(0, metricas.TotalPagados);
        }

        [Fact]
        public async Task BuscarClientesAsync_ConFiltroTexto_RetornaClientesFiltradosConResumenPlanta()
        {
            Clientes c1 = new() { Nombre = "Pedro", ApellidoPaterno = "Ramírez", Telefono = "5512345670", Activo = true };
            Clientes c2 = new() { Nombre = "Sofía", ApellidoPaterno = "López", Telefono = "5512345671", Activo = true };

            int id1 = await _dbService.GuardarClienteAsync(c1);
            int id2 = await _dbService.GuardarClienteAsync(c2);

            await _dbService.GuardarNumerosPlantaClienteAsync(id1, [8, 16]);
            await _dbService.GuardarNumerosPlantaClienteAsync(id2, [33]);

            // Buscar por "López"
            List<Clientes> filtroLopez = await _dbService.BuscarClientesAsync("López", limite: 10);
            Assert.Single(filtroLopez);
            Assert.Equal(id2, filtroLopez[0].Id);
            Assert.Equal("33", filtroLopez[0].NumerosPlantaResumen);

            // Buscar por "Pedro"
            List<Clientes> filtroPedro = await _dbService.BuscarClientesAsync("Pedro", limite: 10);
            Assert.Single(filtroPedro);
            Assert.Equal(id1, filtroPedro[0].Id);
            Assert.Equal("08, 16", filtroPedro[0].NumerosPlantaResumen);
        }

        [Fact]
        public async Task GuardarNumerosPlantaClienteAsync_ReemplazaNumerosAnteriores_Y_ActualizaResumen()
        {
            Clientes cliente = new() { Nombre = "Eduardo", Telefono = "5599001122", Activo = true };
            int id = await _dbService.GuardarClienteAsync(cliente);

            // Asignacion inicial: 10, 20
            await _dbService.GuardarNumerosPlantaClienteAsync(id, [10, 20]);
            List<NumeroPlanta> lista1 = await _dbService.ObtenerNumerosPlantaPorClienteAsync(id);
            Assert.Equal(2, lista1.Count);

            // Reemplazo con nuevos numeros: 99
            await _dbService.GuardarNumerosPlantaClienteAsync(id, [99]);
            List<NumeroPlanta> lista2 = await _dbService.ObtenerNumerosPlantaPorClienteAsync(id);
            Assert.Single(lista2);
            Assert.Equal(99, lista2[0].Numero);

            // Consultar cliente y verificar actualizacion del resumen
            List<Clientes> listaClientes = await _dbService.BuscarClientesAsync(string.Empty, limite: 10);
            Clientes? cConsultado = listaClientes.FirstOrDefault(c => c.Id == id);
            Assert.NotNull(cConsultado);
            Assert.Equal("99", cConsultado.NumerosPlantaResumen);
        }
    }
}
