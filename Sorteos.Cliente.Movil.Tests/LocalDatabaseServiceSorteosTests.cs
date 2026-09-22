using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
    public class LocalDatabaseServiceSorteosTests : IAsyncLifetime
    {
        private readonly string _dbPath;
        private readonly LocalDatabaseService _dbService;

        public LocalDatabaseServiceSorteosTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"sorteos_ciclo_test_{Guid.NewGuid():N}.db3");
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
        public async Task CicloVidaSorteo_CreacionConPremios_InicializacionYEliminacionEnCascada()
        {
            // 1. Crear premios
            PremioLocal p1 = new() { Lugar = 1, DescripcionPremio = "3000", Activo = true };
            int idPremio = await _dbService.GuardarPremioAsync(p1);

            // 2. Crear sorteo
            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 2,
                Descripcion = "Sorteo Martes",
                CantidadNumeros = 100,
                Oportunidades = 1,
                Costo = 25m,
                IdEstatusSorteo = 1
            };

            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, [idPremio]);
            Assert.True(sorteoId > 0);

            // Inicializar 100 numeros
            await _dbService.InicializarNumerosSorteoAsync(sorteoId, 100);

            MetricasSorteoDto metricasIniciales = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(100, metricasIniciales.TotalNumeros);
            Assert.Equal(100, metricasIniciales.TotalDisponibles);

            // 3. Eliminar sorteo en cascada
            int eliminados = await _dbService.EliminarSorteoYPremiosAsync(sorteoId);
            Assert.True(eliminados > 0);

            SorteoPlantilla? consultado = await _dbService.ObtenerSorteoPorIdAsync(sorteoId);
            Assert.Null(consultado);

            // Los numeros del sorteo ya no deben existir
            MetricasSorteoDto metricasPostEliminacion = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(0, metricasPostEliminacion.TotalNumeros);
        }

        [Fact]
        public async Task ApartadoMasivo_Y_AlternarPago_ActualizaMetricasCorrectamente()
        {
            // 1. Crear sorteo y cliente
            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 3,
                Descripcion = "Sorteo Miércoles",
                CantidadNumeros = 100,
                Oportunidades = 1,
                Costo = 30m,
                IdEstatusSorteo = 1
            };
            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, []);
            await _dbService.InicializarNumerosSorteoAsync(sorteoId, 100);

            Clientes cliente = new() { Nombre = "Carlos Mendoza", Telefono = "5599887766" };
            int clienteId = await _dbService.GuardarClienteAsync(cliente);

            // 2. Apartar 3 numeros (estatus inicial: 1 = Apartado)
            int reservaId = await _dbService.ApartarNumerosMasivoAsync(
                sorteoId, clienteId, [1, 2, 3], 90m, "Efectivo", "Carlos Mendoza");
            Assert.True(reservaId > 0);

            MetricasSorteoDto mApartado = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(1, mApartado.TotalApartados);
            Assert.Equal(0, mApartado.TotalPagados);
            Assert.Equal(97, mApartado.TotalDisponibles);

            // 3. Alternar estatus de pago -> cambia a 2 (Pagado)
            int resultadoPago = await _dbService.AlternarEstatusPagoAsync(reservaId);
            Assert.True(resultadoPago > 0);

            MetricasSorteoDto mPagado = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(0, mPagado.TotalApartados);
            Assert.Equal(1, mPagado.TotalPagados);
            Assert.Equal(97, mPagado.TotalDisponibles);

            // 4. Alternar nuevamente -> regresa a 1 (Apartado)
            await _dbService.AlternarEstatusPagoAsync(reservaId);
            MetricasSorteoDto mRegreso = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(1, mRegreso.TotalApartados);
            Assert.Equal(0, mRegreso.TotalPagados);
        }

        [Fact]
        public async Task LiberarNumeroIndividual_Y_LiberarReservaCompleta_RestauraDisponibilidad()
        {
            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 4,
                Descripcion = "Sorteo Jueves",
                CantidadNumeros = 100,
                Oportunidades = 1,
                Costo = 20m,
                IdEstatusSorteo = 1
            };
            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, []);
            await _dbService.InicializarNumerosSorteoAsync(sorteoId, 100);

            Clientes cliente = new() { Nombre = "Ana Luisa", Telefono = "5544332211" };
            int clienteId = await _dbService.GuardarClienteAsync(cliente);

            // Apartar 2 numeros: 50 y 51
            int reservaId = await _dbService.ApartarNumerosMasivoAsync(
                sorteoId, clienteId, [50, 51], 40m, "Efectivo", "Ana Luisa");

            // Liberar unicamente el numero 50
            int liberado = await _dbService.LiberarNumeroAsync(sorteoId, 50);
            Assert.True(liberado > 0);

            MetricasSorteoDto m1 = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(99, m1.TotalDisponibles); // Solo 51 sigue apartado

            // Liberar la reserva completa restante
            int liberadosReserva = await _dbService.LiberarReservaCompletaAsync(reservaId);
            Assert.True(liberadosReserva > 0);

            MetricasSorteoDto m2 = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(100, m2.TotalDisponibles);
            Assert.Equal(0, m2.TotalApartados);
        }

        [Fact]
        public async Task FlujoPremiacion_RegistrarGanador_Y_CerrarSorteoConPremios()
        {
            // 1. Configurar premio, sorteo y venta pagada
            PremioLocal premio = new() { Lugar = 1, DescripcionPremio = "15000", Activo = true };
            int premioId = await _dbService.GuardarPremioAsync(premio);

            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 5,
                Descripcion = "Gran Sorteo Viernes",
                CantidadNumeros = 100,
                Oportunidades = 1,
                Costo = 50m,
                IdEstatusSorteo = 2 // En Juego
            };
            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, [premioId]);
            await _dbService.InicializarNumerosSorteoAsync(sorteoId, 100);

            Clientes cliente = new() { Nombre = "Ganador Afortunado", Telefono = "5500112233" };
            int clienteId = await _dbService.GuardarClienteAsync(cliente);

            // Pago directo del numero 77 (estatus 2 = Pagado)
            int reservaId = await _dbService.RegistrarPagoNumerosDirectoAsync(
                sorteoId, clienteId, [77], 50m, "Transferencia", "Ganador Afortunado");

            // 2. Registrar el 77 como ganador del 1er premio
            int idPremiada = await _dbService.RegistrarPremioGanadorAsync(sorteoId, premioId, reservaId, 77);
            Assert.True(idPremiada > 0);

            // 3. Consultar detalle del numero ganador
            DetalleNumeroGanadorDto? detalle = await _dbService.ObtenerDetalleNumeroGanadorAsync(sorteoId, 77);
            Assert.NotNull(detalle);
            Assert.Equal("Ganador Afortunado", detalle.NombreCliente);

            MetricasSorteoDto mConGanador = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(1, mConGanador.TotalGanadores);

            // 4. Cerrar sorteo con premios
            List<ReservaPremiada> premiosGanadores = await _dbService.ObtenerPremiosGanadoresPorSorteoAsync(sorteoId);
            int cierre = await _dbService.CerrarSorteoConPremiosAsync(sorteoId, premiosGanadores);
            Assert.True(cierre > 0);

            SorteoPlantilla? sorteoCerrado = await _dbService.ObtenerSorteoPorIdAsync(sorteoId);
            Assert.NotNull(sorteoCerrado);
            Assert.Equal(3, sorteoCerrado.IdEstatusSorteo); // 3 = Finalizado
            Assert.True(sorteoCerrado.EsSorteoFinalizado);
        }

        [Fact]
        public async Task SecuenciaFolios_CalculaConsecutivo_Y_SobreviveAEliminacionDeSorteos()
        {
            // 1. Folio inicial debe ser 01
            string folioInicial = await _dbService.ObtenerSiguienteFolioSorteoAsync();
            Assert.Equal("01", folioInicial);

            // 2. Guardar primer sorteo con folio 01
            SorteoPlantilla s1 = new()
            {
                IdDiasSorteo = 1,
                Descripcion = "Sorteo Lunes",
                NumeroDeSorteo = folioInicial,
                CantidadNumeros = 100,
                Costo = 50m
            };
            int s1Id = await _dbService.GuardarSorteoAsync(s1, []);
            Assert.True(s1Id > 0);

            // 3. Siguiente folio debe ser 02
            string folioSegundo = await _dbService.ObtenerSiguienteFolioSorteoAsync();
            Assert.Equal("02", folioSegundo);

            // 4. Guardar segundo sorteo con folio 02
            SorteoPlantilla s2 = new()
            {
                IdDiasSorteo = 2,
                Descripcion = "Sorteo Martes",
                NumeroDeSorteo = folioSegundo,
                CantidadNumeros = 100,
                Costo = 50m
            };
            int s2Id = await _dbService.GuardarSorteoAsync(s2, []);
            Assert.True(s2Id > 0);

            // 5. Siguiente folio debe ser 03
            string folioTercero = await _dbService.ObtenerSiguienteFolioSorteoAsync();
            Assert.Equal("03", folioTercero);

            // 6. Eliminar todos los sorteos (simulando fin de ciclo o borrado semanal)
            await _dbService.EliminarSorteoYPremiosAsync(s1Id);
            await _dbService.EliminarSorteoYPremiosAsync(s2Id);

            // 7. El consecutivo DEBE seguir siendo 03 gracias a FolioSecuencia
            string folioTrasBorrado = await _dbService.ObtenerSiguienteFolioSorteoAsync();
            Assert.Equal("03", folioTrasBorrado);

            // 8. El usuario decide manualmente usar un folio superior (ej. 09)
            SorteoPlantilla sManual = new()
            {
                IdDiasSorteo = 3,
                Descripcion = "Sorteo Especial",
                NumeroDeSorteo = "09",
                CantidadNumeros = 100,
                Costo = 50m
            };
            await _dbService.GuardarSorteoAsync(sManual, []);

            // 9. El siguiente debe continuar a partir del maximo usado (10)
            string folioSiguienteManual = await _dbService.ObtenerSiguienteFolioSorteoAsync();
            Assert.Equal("10", folioSiguienteManual);
        }

        [Fact]
        public async Task SorteoConOportunidades_CalculaMetricasYEmisionesCorrectamente()
        {
            // 1. Sorteo con 100 numeros y 2 oportunidades = 50 emisiones
            SorteoPlantilla sorteo = new()
            {
                IdDiasSorteo = 4,
                Descripcion = "Sorteo Jueves 2 Oportunidades",
                CantidadNumeros = 100,
                Oportunidades = 2,
                Costo = 100m,
                IdEstatusSorteo = 1
            };
            int sorteoId = await _dbService.GuardarSorteoAsync(sorteo, []);
            Assert.Equal(50, sorteo.CantidadEmisiones);

            Clientes cliente = new() { Nombre = "Juan Perez", Telefono = "5512345678" };
            int clienteId = await _dbService.GuardarClienteAsync(cliente);

            // 2. Apartar 4 combos completos (4 boletos, 8 numeros)
            await _dbService.ApartarNumerosMasivoAsync(sorteoId, clienteId, [1, 51], 100m, "Efectivo", "Juan Perez");
            await _dbService.ApartarNumerosMasivoAsync(sorteoId, clienteId, [2, 52], 100m, "Efectivo", "Juan Perez");
            await _dbService.ApartarNumerosMasivoAsync(sorteoId, clienteId, [3, 53], 100m, "Efectivo", "Juan Perez");
            await _dbService.ApartarNumerosMasivoAsync(sorteoId, clienteId, [4, 54], 100m, "Efectivo", "Juan Perez");

            // 3. Consultar metricas consolidadas
            MetricasSorteoDto metricas = await _dbService.ObtenerMetricasSorteoAsync(sorteoId);
            Assert.Equal(50, metricas.TotalNumeros);
            Assert.Equal(4, metricas.TotalApartados);
            Assert.Equal(0, metricas.TotalPagados);
            Assert.Equal(46, metricas.TotalDisponibles);

            // 4. Sorteo con numeros huerfanos (100 numeros, 3 oportunidades = 33 grupos + 1 huerfano = 34 emisiones)
            SorteoPlantilla sorteoHuerfanos = new()
            {
                IdDiasSorteo = 5,
                Descripcion = "Sorteo Viernes 3 Oportunidades",
                CantidadNumeros = 100,
                Oportunidades = 3,
                Costo = 50m,
                IdEstatusSorteo = 1
            };
            int sHuerfanoId = await _dbService.GuardarSorteoAsync(sorteoHuerfanos, []);
            Assert.Equal(34, sorteoHuerfanos.CantidadEmisiones);

            MetricasSorteoDto metricasHuerfanos = await _dbService.ObtenerMetricasSorteoAsync(sHuerfanoId);
            Assert.Equal(34, metricasHuerfanos.TotalNumeros);
            Assert.Equal(34, metricasHuerfanos.TotalDisponibles);
            Assert.Equal(0, metricasHuerfanos.TotalApartados);
        }
    }
}

