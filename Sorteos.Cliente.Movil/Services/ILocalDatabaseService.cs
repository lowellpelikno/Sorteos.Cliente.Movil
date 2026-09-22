using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    public enum ResultadoTransicionSemanal
    {
        SinCambioDeSemana = 0,
        SemanaActualizadaSinPendientes = 1,
        RequiereResolucionPendientes = 2
    }

    public interface ILocalDatabaseService
    {
        Task InitAsync();
        Task CerrarConexionAsync();

        // Dias de la semana, Transición Semanal y Semillas de Estatus
        Task SembrarDiasSemanaAsync();
        Task<List<DiaSemanaItem>> ObtenerDiasSemanaAsync();
        Task GuardarDiasSemanaAsync(List<DiaSemanaItem> dias);
        Task<ResultadoTransicionSemanal> VerificarTransicionSemanalAsync();
        Task<List<DiaSemanaItem>> ObtenerDiasConSorteosPendientesAsync();
        Task RegenerarDiasSemanaActualAsync();
        Task<List<EstatusSorteoItem>> ObtenerEstatusSorteoAsync();
        Task<List<EstatusApartadoItem>> ObtenerEstatusApartadoAsync();

        // Premios
        Task<int> GuardarPremioAsync(PremioLocal premio);
        Task<List<PremioLocal>> ObtenerPremiosAsync();
        Task<PremioLocal?> ObtenerPremioPorIdAsync(int idPremio);
        Task<int> EliminarPremioAsync(int idPremio);

        // Sorteos
        Task<string> ObtenerSiguienteFolioSorteoAsync();
        Task ActualizarUltimoFolioSorteoAsync(int folio);
        Task<int> GuardarSorteoAsync(SorteoPlantilla sorteo, List<int> idsPremiosSeleccionados);
        Task<List<SorteoPlantilla>> ObtenerSorteosAllAsync();
        Task<List<SorteoPlantilla>> ObtenerSorteosPorDiaIdAsync(int idDia);
        Task<SorteoPlantilla?> ObtenerSorteoPorIdAsync(int idSorteo);
        Task<int> EliminarSorteoYPremiosAsync(int idSorteo);

        // Relacion Sorteo - Premio
        Task<List<PremioPorSorteo>> ObtenerPremiosPorSorteoIdAsync(int idSorteo);

        // Clientes
        Task<int> GuardarClienteAsync(Clientes cliente);
        Task<List<Clientes>> ObtenerClientesAsync();
        Task<List<Clientes>> BuscarClientesAsync(string texto, int limite = 10);
        Task<Clientes?> ObtenerClientePorIdAsync(int idCliente);
        Task<int> EliminarClienteAsync(int idCliente);

        // Numeros de planta
        Task<List<NumeroPlanta>> ObtenerNumerosPlantaPorClienteAsync(int idCliente);
        Task<List<NumeroPlanta>> ObtenerTodosNumerosPlantaAsync();
        Task<bool> ExisteNumeroPlantaAsync(int numero, int idClienteExcluir = 0);
        Task GuardarNumerosPlantaClienteAsync(int idCliente, List<int> numeros);
        Task<int> EliminarNumeroPlantaAsync(int idNumeroPlanta);

        // Reservas y Cuadricula
        Task InicializarNumerosSorteoAsync(int sorteoId, int cantidadNumeros);
        Task<List<NumeroGridItem>> ObtenerCuadriculaNumerosSorteoAsync(int idSorteo);
        Task<List<NumeroGridItem>> ObtenerNumerosPaginadosAsync(int idSorteo, string estado, int cursor, int limite, string textoBusqueda);
        Task<List<string>> ObtenerNumerosDisponiblesFormateadosAsync(int idSorteo);
        Task<MetricasSorteoDto> ObtenerMetricasSorteoAsync(int idSorteo);
        Task<List<ClienteConApartadosDto>> ObtenerClientesConApartadosAsync(int idSorteo);
        Task<List<ClienteConApartadosDto>> ObtenerClientesPorEstatusAsync(int idSorteo, int idEstatus);
        Task<int> ApartarNumeroAsync(int idSorteo, int idCliente, int numero, decimal costo, string formaPago);
        Task<int> ApartarNumerosMasivoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago);
        // Sobrecarga que persiste el nombre indicado por el operador en lugar de buscarlo en la tabla Clientes.
        // Regla de negocio: una llamada = una reserva (NumeroReservado). Si el mismo cliente reserva en momentos
        // distintos, se generan filas independientes que pueden liberarse o pagarse por separado.
        Task<int> ApartarNumerosMasivoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago, string nombre);
        Task<int> RegistrarPagoNumerosDirectoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago, string nombre, string comprobanteUrl = "", int idEstatus = 2);
        Task<int> LiberarNumeroAsync(int idSorteo, int numero);
        Task<int> LiberarReservaCompletaAsync(int idReserva);
        Task<int> AlternarEstatusPagoAsync(int idReserva);
        Task<int> CambiarEstatusReservaAsync(int idReserva, int idEstatus);
        Task<int> ActualizarPagoReservaAsync(int idReserva, int idEstatus, string formaPago, string comprobanteUrl);
        Task<int> RegistrarPagoEfectivoMasivoAsync(List<int> reservaIds);
        Task AplicarNumerosPlantaASorteoAsync(int idSorteo);

        // Premiacion
        Task<int> RegistrarPremioGanadorAsync(int idSorteo, int idPremio, int idReserva, int numeroGanador);
        Task<List<ReservaPremiada>> ObtenerPremiosGanadoresPorSorteoAsync(int idSorteo);
        Task<int> QuitarPremioGanadorAsync(int idReservaPremiada);
        Task<DetalleNumeroGanadorDto?> ObtenerDetalleNumeroGanadorAsync(int idSorteo, int numero);
        Task<int> CerrarSorteoConPremiosAsync(int idSorteo, List<ReservaPremiada> premiosGanadores);

        // Dashboard Ejecutivo
        Task<DashboardResumenDto> ObtenerResumenDashboardAsync();
    }
}

