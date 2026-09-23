using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Sorteos.Cliente.Movil.ViewModels;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
/// <summary>
    /// Pruebas unitarias y de integracion para <see cref="MainViewModel"/>,
    /// verificando el calculo de metricas ejecutivas, proyecciones financieras y deteccion de sorteo destacado.
    /// </summary>
    public class MainViewModelTests : IAsyncLifetime
    {
        private readonly string _dbPath;
        private readonly LocalDatabaseService _dbService;

        public MainViewModelTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"sorteos_mainvm_test_{Guid.NewGuid():N}.db3");
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
                // Limpieza de archivos temporales
            }
        }

        [Fact]
        public async Task CargarDashboardAsync_ConBaseVacia_DevuelveResumenInicializado()
        {
            using MainViewModel vm = new(_dbService);

            await vm.CargarDashboardAsync();

            Assert.NotNull(vm.Resumen);
            Assert.False(string.IsNullOrWhiteSpace(vm.FechaActualTexto));
            Assert.False(string.IsNullOrWhiteSpace(vm.Saludo));
            Assert.Equal(0, vm.Resumen.TotalSorteosActivos);
            Assert.Equal(0, vm.Resumen.TotalClientes);
            Assert.Equal(0m, vm.Resumen.TotalMontoPagado);
            Assert.Equal(0m, vm.Resumen.TotalMontoApartado);
            Assert.Equal(0.0, vm.Resumen.PorcentajeOcupacion);
        }

        [Fact]
        public async Task CargarDashboardAsync_ConDatos_CalculaMetricasCorrectamente()
        {
            // 1. Guardar cliente
            int clienteId = await _dbService.GuardarClienteAsync(new Clientes
            {
                Nombre = "Carlos",
                ApellidoPaterno = "Mendoza",
                Telefono = "5512345678"
            });

            // 2. Guardar premio
            int premioId = await _dbService.GuardarPremioAsync(new PremioLocal
            {
                Lugar = 1,
                DescripcionPremio = "1000",
                EsMonetario = true,
                DescripcionLarga = "Premio Mayor"
            });

            // 3. Crear sorteo
            DateTime hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            int idDiaHoy = diff + 1;

            SorteoPlantilla sorteo = new()
            {
                Descripcion = "Gran Sorteo Ejecutivo",
                NumeroDeSorteo = "01",
                Costo = 100m,
                CantidadNumeros = 100,
                IdDiasSorteo = idDiaHoy,
                FechaInicio = hoy,
                FechaFin = hoy,
                Activo = true,
                IdEstatusSorteo = 1
            };

            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, [premioId]);

            // 4. Registrar apartado (pagado)
            await _dbService.RegistrarPagoNumerosDirectoAsync(sorteoId, clienteId, [1, 2], 200m, "Efectivo", "Carlos Mendoza");

            // 5. Registrar apartado (pendiente)
            await _dbService.ApartarNumerosMasivoAsync(sorteoId, clienteId, [3, 4], 200m, "Efectivo", "Carlos Mendoza");

            DashboardResumenDto directo = await _dbService.ObtenerResumenDashboardAsync();
            Assert.NotNull(directo);

            using MainViewModel vm = new(_dbService);
            await vm.CargarDashboardAsync();

            Assert.True(vm.Resumen.TotalClientes >= 1);
            Assert.True(vm.Resumen.TotalSorteosActivos >= 1);
            Assert.Equal(200m, vm.Resumen.TotalMontoPagado);
            Assert.Equal(200m, vm.Resumen.TotalMontoApartado);
            Assert.Equal(400m, vm.Resumen.TotalMontoProyectado);
            Assert.Equal(1, vm.Resumen.TotalApartadosPendientes);
            Assert.True(vm.Resumen.TieneSorteoDestacado);
            Assert.Equal("01", vm.Resumen.SorteoDestacadoNumero);
            Assert.Equal(sorteoId, vm.Resumen.SorteoDestacadoId);
            Assert.True(vm.Resumen.SorteoDestacadoProgreso > 0.0);
        }

        [Fact]
        public void ActualizarSaludoYFecha_GeneraValoresValidos()
        {
            using MainViewModel vm = new(_dbService);
            vm.ActualizarSaludoYFecha();

            Assert.False(string.IsNullOrWhiteSpace(vm.Saludo));
            Assert.False(string.IsNullOrWhiteSpace(vm.FechaActualTexto));
            Assert.Contains(DateTime.Now.Year.ToString(), vm.FechaActualTexto);
        }
    }
}

