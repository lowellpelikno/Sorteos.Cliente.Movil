using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
/// <summary>
    /// Representa el resultado de la evaluacion y ejecucion de la transicion semanal de sorteos.
    /// </summary>
    public enum ResultadoTransicionSemanal
    {
        /// <summary>
        /// Indica que la fecha actual se encuentra dentro del rango de la semana activa; no se requiere transicion.
        /// </summary>
        SinCambioDeSemana = 0,

        /// <summary>
        /// Indica que se avanzo a una nueva semana de operacion y no quedaron sorteos previos con estado pendiente.
        /// </summary>
        SemanaActualizadaSinPendientes = 1,

        /// <summary>
        /// Indica que se detecto un cambio de semana pero existen sorteos previos pendientes de cierre o premiacion.
        /// </summary>
        RequiereResolucionPendientes = 2
    }

    /// <summary>
    /// Define el contrato de persistencia, sincronizacion y logica transaccional en la base de datos local SQLite.
    /// Administra entidades de sorteos, dias operativos, premios, clientes, numeros de planta, reservaciones y reportes ejecutivos.
    /// </summary>
    public interface ILocalDatabaseService
    {
        /// <summary>
        /// Inicializa de manera asincrona la conexion a la base de datos local SQLite, aplicando pragmas de alto rendimiento,
        /// control de concurrencia WAL, claves foraneas y asegurando la creacion del esquema de tablas y datos semilla.
        /// </summary>
        /// <returns>Una tarea asincrona que representa la inicializacion completa de la base de datos.</returns>
        Task InitAsync();

        /// <summary>
        /// Cierra de forma ordenada la conexion activa a SQLite y libera los recursos asincronos del motor local.
        /// </summary>
        /// <returns>Una tarea asincrona que representa el cierre y liberacion de la conexion.</returns>
        Task CerrarConexionAsync();

        // Dias de la semana, Transición Semanal y Semillas de Estatus

        /// <summary>
        /// Realiza la siembra inicial de los dias de la semana en la tabla de catalogo si no existen registros previos.
        /// </summary>
        /// <returns>Una tarea asincrona que representa la siembra de dias operativos.</returns>
        Task SembrarDiasSemanaAsync();

        /// <summary>
        /// Obtiene el listado completo de los dias de la semana registrados, ordenados cronologicamente segun su ciclo operativo.
        /// </summary>
        /// <returns>Coleccion de objetos <see cref="DiaSemanaItem"/> con los datos operativos de cada dia.</returns>
        Task<List<DiaSemanaItem>> ObtenerDiasSemanaAsync();

        /// <summary>
        /// Persiste la configuracion y estado actualizado de la lista de dias de la semana en el motor local.
        /// </summary>
        /// <param name="dias">Coleccion de elementos <see cref="DiaSemanaItem"/> a persistir.</param>
        /// <returns>Una tarea asincrona que representa la persistencia de los dias.</returns>
        Task GuardarDiasSemanaAsync(List<DiaSemanaItem> dias);

        /// <summary>
        /// Evalua si la fecha actual corresponde a un nuevo ciclo semanal respecto al registro guardado,
        /// determinando si se requiere actualizar las fechas o resolver sorteos pendientes.
        /// </summary>
        /// <returns>El resultado de la evaluacion tipificado como <see cref="ResultadoTransicionSemanal"/>.</returns>
        Task<ResultadoTransicionSemanal> VerificarTransicionSemanalAsync();

        /// <summary>
        /// Obtiene los dias del ciclo previo que contienen sorteos activos o apartados sin resolver.
        /// </summary>
        /// <returns>Coleccion de dias con inconsistencias o pendientes por procesar.</returns>
        Task<List<DiaSemanaItem>> ObtenerDiasConSorteosPendientesAsync();

        /// <summary>
        /// Recalcula y actualiza las fechas fisicas de los dias de la semana para alinearse con la semana natural vigente.
        /// </summary>
        /// <returns>Una tarea asincrona que representa la regeneracion del calendario semanal.</returns>
        Task RegenerarDiasSemanaActualAsync();

        /// <summary>
        /// Obtiene el catalogo de estatus aplicables a los sorteos (ej. Activo, Finalizado, Cancelado).
        /// </summary>
        /// <returns>Lista de elementos <see cref="EstatusSorteoItem"/> almacenados o cacheados.</returns>
        Task<List<EstatusSorteoItem>> ObtenerEstatusSorteoAsync();

        /// <summary>
        /// Obtiene el catalogo de estatus aplicables a los apartados y reservas (ej. Apartado, Pagado, Liberado).
        /// </summary>
        /// <returns>Lista de elementos <see cref="EstatusApartadoItem"/> almacenados o cacheados.</returns>
        Task<List<EstatusApartadoItem>> ObtenerEstatusApartadoAsync();

        // Premios

        /// <summary>
        /// Inserta o actualiza un registro de premio en la base de datos local.
        /// </summary>
        /// <param name="premio">Instancia de <see cref="PremioLocal"/> con la informacion del premio a persistir.</param>
        /// <returns>Identificador unico generado o actualizado del premio.</returns>
        Task<int> GuardarPremioAsync(PremioLocal premio);

        /// <summary>
        /// Obtiene la lista completa de premios registrados localmente, ordenados de forma descendente por identificador.
        /// </summary>
        /// <returns>Coleccion de <see cref="PremioLocal"/> registrados en el sistema.</returns>
        Task<List<PremioLocal>> ObtenerPremiosAsync();

        /// <summary>
        /// Busca y recupera la informacion de un premio especifico mediante su identificador primario.
        /// </summary>
        /// <param name="idPremio">Identificador unico del premio.</param>
        /// <returns>Instancia de <see cref="PremioLocal"/> si se localiza; de lo contrario, null.</returns>
        Task<PremioLocal?> ObtenerPremioPorIdAsync(int idPremio);

        /// <summary>
        /// Elimina un premio de la base de datos local, verificando previamente que no se encuentre vinculado a sorteos historicos.
        /// </summary>
        /// <param name="idPremio">Identificador del premio a eliminar.</param>
        /// <returns>Numero de filas afectadas en la base de datos.</returns>
        Task<int> EliminarPremioAsync(int idPremio);

        // Sorteos

        /// <summary>
        /// Genera y reserva de forma atomica el siguiente folio correlativo con formato comercial para un sorteo nuevo.
        /// </summary>
        /// <returns>Cadena alfanumerica representativa del siguiente folio de sorteo.</returns>
        Task<string> ObtenerSiguienteFolioSorteoAsync();

        /// <summary>
        /// Actualiza el ultimo folio registrado en la secuencia de sorteos para mantener sincronizada la correlatividad.
        /// </summary>
        /// <param name="folio">Numero entero representativo del ultimo folio emitido.</param>
        /// <returns>Una tarea asincrona que representa la actualizacion de la secuencia.</returns>
        Task ActualizarUltimoFolioSorteoAsync(int folio);

        /// <summary>
        /// Persiste o actualiza un sorteo dentro de una transaccion atomica, asociando los premios vinculados y generando su cuadricula.
        /// </summary>
        /// <param name="sorteo">Instancia de <see cref="SorteoPlantilla"/> a registrar.</param>
        /// <param name="idsPremiosSeleccionados">Coleccion de identificadores de premios asignados al sorteo.</param>
        /// <returns>Identificador primario asignado al sorteo guardado.</returns>
        Task<int> GuardarSorteoAsync(SorteoPlantilla sorteo, List<int> idsPremiosSeleccionados);

        /// <summary>
        /// Recupera todos los sorteos registrados en la base de datos con su informacion basica y agregaciones.
        /// </summary>
        /// <returns>Lista completa de registros <see cref="SorteoPlantilla"/>.</returns>
        Task<List<SorteoPlantilla>> ObtenerSorteosAllAsync();

        /// <summary>
        /// Obtiene los sorteos programados para un dia especifico de la semana operativa.
        /// </summary>
        /// <param name="idDia">Identificador del dia de la semana consultado.</param>
        /// <returns>Lista de sorteos vinculados al dia indicado.</returns>
        Task<List<SorteoPlantilla>> ObtenerSorteosPorDiaIdAsync(int idDia);

        /// <summary>
        /// Recupera un sorteo mediante su identificador primario.
        /// </summary>
        /// <param name="idSorteo">Identificador unico del sorteo.</param>
        /// <returns>Instancia de <see cref="SorteoPlantilla"/> si existe; de lo contrario, null.</returns>
        Task<SorteoPlantilla?> ObtenerSorteoPorIdAsync(int idSorteo);

        /// <summary>
        /// Elimina un sorteo de forma atomica junto con sus relaciones de premios, numeros y reservas asociadas.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo a remover.</param>
        /// <returns>Numero total de registros afectados.</returns>
        Task<int> EliminarSorteoYPremiosAsync(int idSorteo);

        // Relacion Sorteo - Premio

        /// <summary>
        /// Obtiene los premios asignados a un sorteo especifico, ordenados por jerarquia o posicion de asignacion.
        /// </summary>
        /// <param name="idSorteo">Identificador unico del sorteo.</param>
        /// <returns>Lista de objetos <see cref="PremioPorSorteo"/> con el detalle del premio.</returns>
        Task<List<PremioPorSorteo>> ObtenerPremiosPorSorteoIdAsync(int idSorteo);

        // Clientes

        /// <summary>
        /// Registra o modifica la informacion de un cliente en la base de datos local.
        /// </summary>
        /// <param name="cliente">Entidad de cliente a guardar.</param>
        /// <returns>Identificador primario asignado al cliente.</returns>
        Task<int> GuardarClienteAsync(Clientes cliente);

        /// <summary>
        /// Obtiene el listado completo de clientes registrados, ordenados alfabeticamente por nombre.
        /// </summary>
        /// <returns>Coleccion con todos los clientes del sistema.</returns>
        Task<List<Clientes>> ObtenerClientesAsync();

        /// <summary>
        /// Realiza una busqueda optimizada de clientes por coincidencia de texto en nombre, telefono o notas.
        /// </summary>
        /// <param name="texto">Texto o termino de busqueda.</param>
        /// <param name="limite">Cantidad maxima de registros a devolver.</param>
        /// <returns>Coleccion de clientes que coinciden con el criterio de busqueda.</returns>
        Task<List<Clientes>> BuscarClientesAsync(string texto, int limite = 10);

        /// <summary>
        /// Recupera los datos de un cliente especifico a partir de su identificador primario.
        /// </summary>
        /// <param name="idCliente">Identificador unico del cliente.</param>
        /// <returns>Instancia de <see cref="Clientes"/> si se localiza; de lo contrario, null.</returns>
        Task<Clientes?> ObtenerClientePorIdAsync(int idCliente);

        /// <summary>
        /// Elimina un cliente de la base de datos local, liberando de forma atomica sus numeros de planta vinculados.
        /// </summary>
        /// <param name="idCliente">Identificador del cliente a eliminar.</param>
        /// <returns>Numero de filas modificadas o eliminadas.</returns>
        Task<int> EliminarClienteAsync(int idCliente);

        // Numeros de planta

        /// <summary>
        /// Obtiene la lista de numeros de planta fijos asignados a un cliente especifico.
        /// </summary>
        /// <param name="idCliente">Identificador primario del cliente.</param>
        /// <returns>Coleccion de numeros de planta del cliente.</returns>
        Task<List<NumeroPlanta>> ObtenerNumerosPlantaPorClienteAsync(int idCliente);

        /// <summary>
        /// Obtiene el universo completo de numeros de planta registrados en el sistema con su cliente asociado.
        /// </summary>
        /// <returns>Coleccion de todos los numeros de planta registrados.</returns>
        Task<List<NumeroPlanta>> ObtenerTodosNumerosPlantaAsync();

        /// <summary>
        /// Comprueba si un valor numerico ya se encuentra asignado como numero de planta a cualquier cliente activo.
        /// </summary>
        /// <param name="numero">Digito o valor numerico a verificar.</param>
        /// <param name="idClienteExcluir">Identificador de cliente a ignorar en la comprobacion (util en edicion).</param>
        /// <returns>Verdadero si el numero ya esta ocupado; falso en caso contrario.</returns>
        Task<bool> ExisteNumeroPlantaAsync(int numero, int idClienteExcluir = 0);

        /// <summary>
        /// Reemplaza o asigna de forma atomica la lista de numeros de planta pertenecientes a un cliente.
        /// </summary>
        /// <param name="idCliente">Identificador del cliente.</param>
        /// <param name="numeros">Coleccion de valores enteros a registrar como fijos.</param>
        /// <returns>Una tarea asincrona que representa la persistencia de numeros de planta.</returns>
        Task GuardarNumerosPlantaClienteAsync(int idCliente, List<int> numeros);

        /// <summary>
        /// Elimina un registro individual de numero de planta por su identificador primario.
        /// </summary>
        /// <param name="idNumeroPlanta">Identificador del registro de numero de planta.</param>
        /// <returns>Cantidad de filas afectadas.</returns>
        Task<int> EliminarNumeroPlantaAsync(int idNumeroPlanta);

        // Reservas y Cuadricula

        /// <summary>
        /// Genera de forma masiva y atomica todos los registros de boletos/numeros que componen la cuadricula de un nuevo sorteo.
        /// </summary>
        /// <param name="sorteoId">Identificador del sorteo.</param>
        /// <param name="cantidadNumeros">Cantidad total de casillas o numeros que conformaran el sorteo.</param>
        /// <returns>Una tarea asincrona que representa la generacion inicial de casillas.</returns>
        Task InicializarNumerosSorteoAsync(int sorteoId, int cantidadNumeros);

        /// <summary>
        /// Recupera el estado de toda la cuadricula de numeros de un sorteo para su renderizado visual o gestion en lote.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo consultado.</param>
        /// <returns>Coleccion de casillas tipificadas como <see cref="NumeroGridItem"/>.</returns>
        Task<List<NumeroGridItem>> ObtenerCuadriculaNumerosSorteoAsync(int idSorteo);

        /// <summary>
        /// Obtiene un segmento paginado de numeros de la cuadricula basado en cursor, filtro de estado y texto de busqueda.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="estado">Estado del boleto por filtrar (Todos, Disponible, Apartado, Pagado).</param>
        /// <param name="cursor">Numero o indice a partir del cual continuar la paginacion.</param>
        /// <param name="limite">Cantidad de elementos a recuperar.</param>
        /// <param name="textoBusqueda">Texto o digito de filtrado opcional.</param>
        /// <returns>Segmento de elementos <see cref="NumeroGridItem"/>.</returns>
        Task<List<NumeroGridItem>> ObtenerNumerosPaginadosAsync(int idSorteo, string estado, int cursor, int limite, string textoBusqueda);

        /// <summary>
        /// Obtiene los numeros disponibles en el sorteo formateados con ceros a la izquierda segun la escala configurada.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <returns>Lista de cadenas con los numeros disponibles.</returns>
        Task<List<string>> ObtenerNumerosDisponiblesFormateadosAsync(int idSorteo);

        /// <summary>
        /// Calcula de manera directa y optimizada en SQLite las metricas comerciales de un sorteo (recaudacion, ocupacion, porcentajes).
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo a auditar.</param>
        /// <returns>DTO <see cref="MetricasSorteoDto"/> con los calculos financieros y de disponibilidad.</returns>
        Task<MetricasSorteoDto> ObtenerMetricasSorteoAsync(int idSorteo);

        /// <summary>
        /// Recupera la lista agrupada de clientes que poseen reservas o apartados vigentes dentro de un sorteo.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <returns>Coleccion de <see cref="ClienteConApartadosDto"/> con los apartados agrupados.</returns>
        Task<List<ClienteConApartadosDto>> ObtenerClientesConApartadosAsync(int idSorteo);

        /// <summary>
        /// Recupera la lista de clientes filtrada por el estatus de sus reservas en un sorteo especifico.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="idEstatus">Identificador del estatus de reserva.</param>
        /// <returns>Coleccion de <see cref="ClienteConApartadosDto"/> segun el filtro aplicado.</returns>
        Task<List<ClienteConApartadosDto>> ObtenerClientesPorEstatusAsync(int idSorteo, int idEstatus);

        /// <summary>
        /// Genera una reserva atomica para un unico numero en un sorteo determinado.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="idCliente">Identificador del cliente.</param>
        /// <param name="numero">Numero a apartar.</param>
        /// <param name="costo">Monto del boleto.</param>
        /// <param name="formaPago">Metodo de pago convenido.</param>
        /// <returns>Identificador de la reserva generada.</returns>
        Task<int> ApartarNumeroAsync(int idSorteo, int idCliente, int numero, decimal costo, string formaPago);

        /// <summary>
        /// Realiza el apartado masivo de un conjunto de numeros bajo una sola reserva transaccional.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="idCliente">Identificador del cliente.</param>
        /// <param name="numeros">Coleccion de numeros a apartar.</param>
        /// <param name="costo">Monto total o acumulado de la operacion.</param>
        /// <param name="formaPago">Metodo de pago convenido.</param>
        /// <returns>Identificador de la reserva creada.</returns>
        Task<int> ApartarNumerosMasivoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago);

        /// <summary>
        /// Persiste una reserva masiva asignando un nombre personalizado proporcionado por el operador.
        /// </summary>
        /// <remarks>
        /// Regla de negocio: una llamada = una reserva (NumeroReservado). Si el mismo cliente reserva en momentos
        /// distintos, se generan filas independientes que pueden liberarse o pagarse por separado.
        /// </remarks>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="idCliente">Identificador del cliente.</param>
        /// <param name="numeros">Coleccion de numeros a apartar.</param>
        /// <param name="costo">Monto total de los numeros apartados.</param>
        /// <param name="formaPago">Forma de pago convenida.</param>
        /// <param name="nombre">Nombre personalizado del cliente o titular de la reserva.</param>
        /// <returns>Identificador de la reserva creada.</returns>
        Task<int> ApartarNumerosMasivoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago, string nombre);

        /// <summary>
        /// Registra un conjunto de numeros directamente como pagados, adjuntando soporte de pago y comprobante de manera atomica.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="idCliente">Identificador del cliente titular.</param>
        /// <param name="numeros">Coleccion de numeros por pagar.</param>
        /// <param name="costo">Monto economico total pagado.</param>
        /// <param name="formaPago">Metodo o via de liquidacion.</param>
        /// <param name="nombre">Nombre del titular registrado.</param>
        /// <param name="comprobanteUrl">Ruta o referencia del comprobante digital asociado.</param>
        /// <param name="idEstatus">Estatus final asignado (por defecto Pagado = 2).</param>
        /// <returns>Identificador de la reserva registrada.</returns>
        Task<int> RegistrarPagoNumerosDirectoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago, string nombre, string comprobanteUrl = "", int idEstatus = 2);

        /// <summary>
        /// Libera un numero especifico dentro de un sorteo, removiendo su asignacion y recalculando la reserva correspondiente.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="numero">Numero a devolver a disponibilidad.</param>
        /// <returns>Numero de registros modificados.</returns>
        Task<int> LiberarNumeroAsync(int idSorteo, int numero);

        /// <summary>
        /// Cancela y libera la totalidad de numeros vinculados a una reserva especifica.
        /// </summary>
        /// <param name="idReserva">Identificador de la reserva a liquidar.</param>
        /// <returns>Total de casillas liberadas.</returns>
        Task<int> LiberarReservaCompletaAsync(int idReserva);

        /// <summary>
        /// Alterna el estatus de pago de una reserva entre Apartado y Pagado dentro de una transaccion.
        /// </summary>
        /// <param name="idReserva">Identificador de la reserva.</param>
        /// <returns>Nuevo identificador de estatus asignado.</returns>
        Task<int> AlternarEstatusPagoAsync(int idReserva);

        /// <summary>
        /// Modifica explicitamente el estatus operativo de una reserva (ej. Pagado, Cancelado, Pendiente).
        /// </summary>
        /// <param name="idReserva">Identificador de la reserva a actualizar.</param>
        /// <param name="idEstatus">Nuevo estatus por asignar.</param>
        /// <returns>Cantidad de registros afectados.</returns>
        Task<int> CambiarEstatusReservaAsync(int idReserva, int idEstatus);

        /// <summary>
        /// Actualiza los metadatos de pago de una reserva, incluyendo estatus, metodo de pago y ruta de comprobante.
        /// </summary>
        /// <param name="idReserva">Identificador de la reserva.</param>
        /// <param name="idEstatus">Nuevo estado financiero de la reserva.</param>
        /// <param name="formaPago">Forma de pago registrada.</param>
        /// <param name="comprobanteUrl">Ruta digital al comprobante de pago.</param>
        /// <returns>Numero de filas modificadas.</returns>
        Task<int> ActualizarPagoReservaAsync(int idReserva, int idEstatus, string formaPago, string comprobanteUrl);

        /// <summary>
        /// Aplica la liquidacion masiva en efectivo para un conjunto de reservas seleccionadas de manera simultanea.
        /// </summary>
        /// <param name="reservaIds">Coleccion de identificadores de reservas a marcar como pagadas.</param>
        /// <returns>Total de reservas liquidadas satisfactoriamente.</returns>
        Task<int> RegistrarPagoEfectivoMasivoAsync(List<int> reservaIds);

        /// <summary>
        /// Aplica automaticamente los numeros de planta vigentes al sorteo indicado, reservandolos a sus clientes titulares.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo recien creado o configurado.</param>
        /// <returns>Una tarea asincrona que representa la asignacion automatica de planta.</returns>
        Task AplicarNumerosPlantaASorteoAsync(int idSorteo);

        // Premiacion

        /// <summary>
        /// Registra la asignacion de un premio a un numero o reserva ganadora dentro de un sorteo.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="idPremio">Identificador del premio obtenido.</param>
        /// <param name="idReserva">Identificador de la reserva premiada.</param>
        /// <param name="numeroGanador">Numero favorecido.</param>
        /// <returns>Identificador de la relacion de premiacion generada.</returns>
        Task<int> RegistrarPremioGanadorAsync(int idSorteo, int idPremio, int idReserva, int numeroGanador);

        /// <summary>
        /// Obtiene la lista de premios que ya han sido asignados como ganadores dentro de un sorteo.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <returns>Coleccion de <see cref="ReservaPremiada"/> con el detalle de los ganadores.</returns>
        Task<List<ReservaPremiada>> ObtenerPremiosGanadoresPorSorteoAsync(int idSorteo);

        /// <summary>
        /// Desvincula y remueve la asignacion ganadora de un premio sobre una reserva o numero.
        /// </summary>
        /// <param name="idReservaPremiada">Identificador unico del registro de premiacion.</param>
        /// <returns>Cantidad de filas eliminadas.</returns>
        Task<int> QuitarPremioGanadorAsync(int idReservaPremiada);

        /// <summary>
        /// Obtiene el detalle comercial y de cliente asociado al numero ganador para validacion de reclamos de premios.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo.</param>
        /// <param name="numero">Numero ganador consultado.</param>
        /// <returns>Instancia de <see cref="DetalleNumeroGanadorDto"/> si el numero fue reservado; de lo contrario, null.</returns>
        Task<DetalleNumeroGanadorDto?> ObtenerDetalleNumeroGanadorAsync(int idSorteo, int numero);

        /// <summary>
        /// Ejecuta el cierre definitivo de un sorteo registrando sus ganadores y actualizando el estatus del sorteo a Finalizado.
        /// </summary>
        /// <param name="idSorteo">Identificador del sorteo por concluir.</param>
        /// <param name="premiosGanadores">Coleccion de premios asignados con sus ganadores definitivos.</param>
        /// <returns>Total de registros modificados en la transaccion de cierre.</returns>
        Task<int> CerrarSorteoConPremiosAsync(int idSorteo, List<ReservaPremiada> premiosGanadores);

        // Dashboard Ejecutivo

        /// <summary>
        /// Consolida y calcula los indicadores globales del negocio (recaudacion, sorteos activos, boletos pendientes y clientes)
        /// mediante consultas SQL optimizadas para el panel de inicio.
        /// </summary>
        /// <returns>Instancia de <see cref="DashboardResumenDto"/> con las metricas operativas globales.</returns>
        Task<DashboardResumenDto> ObtenerResumenDashboardAsync();
    }
}

