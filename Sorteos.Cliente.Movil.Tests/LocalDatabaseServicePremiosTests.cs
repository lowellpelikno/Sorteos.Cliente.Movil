using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
    public class LocalDatabaseServicePremiosTests : IAsyncLifetime
    {
        private readonly string _dbPath;
        private readonly LocalDatabaseService _dbService;

        public LocalDatabaseServicePremiosTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"sorteos_premios_test_{Guid.NewGuid():N}.db3");
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
                // Limpieza silenciosa
            }
        }

        [Fact]
        public async Task GuardarPremioAsync_DetectaDuplicadosPorMontoYLugar_RetornaMenosUno()
        {
            PremioLocal premio1 = new()
            {
                Lugar = 1,
                DescripcionPremio = "5000",
                EsMonetario = true,
                DescripcionLarga = "Premio Mayor",
                Activo = true
            };

            int id1 = await _dbService.GuardarPremioAsync(premio1);
            Assert.True(id1 > 0);

            // Intentar registrar otro premio con el mismo Lugar (1) y la misma Descripcion ("5000")
            PremioLocal premioDuplicado = new()
            {
                Lugar = 1,
                DescripcionPremio = "5000",
                EsMonetario = true,
                DescripcionLarga = "Premio Duplicado",
                Activo = true
            };

            int idDuplicado = await _dbService.GuardarPremioAsync(premioDuplicado);
            Assert.Equal(-1, idDuplicado);
        }

        [Fact]
        public async Task GuardarPremioAsync_EdicionDelMismoPremio_NoAutodetectaDuplicado()
        {
            PremioLocal premio = new()
            {
                Lugar = 2,
                DescripcionPremio = "2000",
                EsMonetario = true,
                DescripcionLarga = "Segundo Premio Original",
                Activo = true
            };

            int id = await _dbService.GuardarPremioAsync(premio);
            Assert.True(id > 0);

            // Editar la descripcion larga manteniendo el mismo lugar y monto
            premio.DescripcionLarga = "Segundo Premio Modificado";
            int idActualizado = await _dbService.GuardarPremioAsync(premio);

            Assert.Equal(id, idActualizado);

            PremioLocal? consultado = await _dbService.ObtenerPremioPorIdAsync(id);
            Assert.NotNull(consultado);
            Assert.Equal("Segundo Premio Modificado", consultado.DescripcionLarga);
        }

        [Fact]
        public async Task ObtenerPremiosAsync_CalculaCantidadSorteosAsignados_Y_OrdenaCorrectamente()
        {
            // 1. Crear premios
            PremioLocal premio1 = new() { Lugar = 1, DescripcionPremio = "10000", Activo = true };
            PremioLocal premio2 = new() { Lugar = 2, DescripcionPremio = "5000", Activo = true };

            int idPremio1 = await _dbService.GuardarPremioAsync(premio1);
            int idPremio2 = await _dbService.GuardarPremioAsync(premio2);

            // 2. Crear un sorteo asignando el premio 1
            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 1,
                Descripcion = "Sorteo con Premio Asignado",
                CantidadNumeros = 100,
                Oportunidades = 1,
                Costo = 100m,
                IdEstatusSorteo = 1
            };

            await _dbService.GuardarSorteoAsync(sorteo, [idPremio1]);

            // 3. Consultar catalogo de premios y verificar calculo SQL
            List<PremioLocal> premios = await _dbService.ObtenerPremiosAsync();
            Assert.True(premios.Count >= 2);

            PremioLocal? p1Consultado = premios.FirstOrDefault(p => p.IdPremio == idPremio1);
            PremioLocal? p2Consultado = premios.FirstOrDefault(p => p.IdPremio == idPremio2);

            Assert.NotNull(p1Consultado);
            Assert.NotNull(p2Consultado);

            // El premio 1 tiene 1 asignacion en sorteo
            Assert.Equal(1, p1Consultado.CantidadSorteosAsignados);
            // El premio 2 tiene 0 asignaciones en sorteo
            Assert.Equal(0, p2Consultado.CantidadSorteosAsignados);
        }

        [Fact]
        public async Task EliminarPremioAsync_EliminaRegistroCorrectamente()
        {
            PremioLocal premio = new() { Lugar = 4, DescripcionPremio = "800", Activo = true };
            int id = await _dbService.GuardarPremioAsync(premio);
            Assert.True(id > 0);

            int eliminados = await _dbService.EliminarPremioAsync(id);
            Assert.True(eliminados > 0);

            PremioLocal? consultado = await _dbService.ObtenerPremioPorIdAsync(id);
            Assert.Null(consultado);
        }
    }
}

