using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;
using Sorteos.Cliente.Movil.Helpers;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    /// <summary>
    /// ViewModel principal para la administracion interactiva de boletos, reservas, pagos y comunicacion de un sorteo.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Cuadricula Visual Ergonomica: Presenta la matriz de boletos con adaptacion dinamica de columnas segun el numero
    ///   de oportunidades por boleto, permitiendo lectura clara y toques comodos sin saturacion visual.
    /// - Codificacion por Estados de Cobro: Distingue de inmediato boletos disponibles, apartados y liquidados,
    ///   permitiendo al usuario alternar entre la cuadricula general y vistas agrupadas por cliente (Apartados y Pagados).
    /// - Seleccion Multiple con Chips Visuales: Soporta seleccion simultanea de boletos, calculando en tiempo real
    ///   el costo acumulado y exhibiendo chips interactivos para deseleccionar elementos con un solo toque.
    /// - Apartado Agil al Azar: Proporciona la herramienta <see cref="PuedeGenerarAlAzar"/> para seleccionar boletos
    ///   aleatorios en segundos, ideal para transmisiones en vivo o peticiones rapidas de clientes.
    /// - Integracion Directa con WhatsApp: Genera automaticamente mensajes formateados con las cuentas bancarias
    ///   del negocio y el desglose de boletos para envio inmediato sin tener que redactar a mano.
    /// - Visor de Comprobantes Integrado: Permite adjuntar y visualizar imagenes de bauches o transferencias dentro
    ///   de la misma aplicacion.
    /// - Rendimiento de Desplazamiento Fluido: Carga los boletos mediante paginacion incremental por cursor para
    ///   soportar sorteos masivos (miles de numeros) sin congelar el hilo principal de la interfaz.
    /// </remarks>
    [QueryProperty(nameof(IdSorteoQuery), "idSorteo")]
    public partial class ReservasSorteoViewModel : ObservableObject
    {
        private readonly ILocalDatabaseService _databaseService;
        private readonly IComprobanteStorageService _comprobanteStorageService;

        private const int TamanoLote = 50;
        private int _cursorActual = -1;
        private bool _hayMasPaginas = true;
        private bool _actualizandoMetricas = false;

        /// <summary>
        /// Obtiene o establece la cantidad de boletos que el usuario desea generar aleatoriamente.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Se restringe automaticamente a digitos y se acota al maximo de boletos disponibles.
        /// </remarks>
        [ObservableProperty]
        public partial string CantidadAzarTexto { get; set; } = "1";

        partial void OnCantidadAzarTextoChanged(string value)
        {
            string filtrado = value.SoloDigitos(4);
            if (filtrado != value)
            {
                CantidadAzarTexto = filtrado;
                return;
            }

            if (int.TryParse(filtrado, out int cant))
            {
                int maximo = Math.Max(1, TotalDisponibles > 0 ? TotalDisponibles : TotalNumeros);
                if (cant > maximo)
                {
                    CantidadAzarTexto = maximo.ToString();
                }
            }
        }

        /// <summary>
        /// Identificador de sorteo recibido por navegacion Shell para inicializar la vista.
        /// </summary>
        [ObservableProperty]
        public partial string IdSorteoQuery { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la vista esta realizando una operacion de carga o procesamiento en segundo plano.
        /// </summary>
        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        /// <summary>
        /// Indica si la cuadricula esta recuperando el siguiente bloque de boletos al hacer scroll.
        /// </summary>
        [ObservableProperty]
        public partial bool IsCargandoMas { get; set; }

        /// <summary>
        /// Obtiene o establece la entidad del sorteo actualmente cargado.
        /// </summary>
        [ObservableProperty]
        public partial SorteoPlantilla? SorteoActual { get; set; }

        /// <summary>
        /// Titulo visible del sorteo en la cabecera.
        /// </summary>
        [ObservableProperty]
        public partial string TituloSorteo { get; set; } = string.Empty;

        /// <summary>
        /// Subtitulo informativo con desglose de folios, cantidad de numeros y oportunidades por boleto.
        /// </summary>
        [ObservableProperty]
        public partial string SubtituloSorteo { get; set; } = string.Empty;

        /// <summary>
        /// Modalidad o loteria con la que se define el sorteo.
        /// </summary>
        [ObservableProperty]
        public partial string ModalidadJuego { get; set; } = string.Empty;

        /// <summary>
        /// Texto formateado con el costo por boleto individual.
        /// </summary>
        [ObservableProperty]
        public partial string CostoBoletoTexto { get; set; } = "$0.00";

        /// <summary>
        /// Total absoluto de numeros que componen el sorteo.
        /// </summary>
        [ObservableProperty]
        public partial int TotalNumeros { get; set; }

        /// <summary>
        /// Cantidad de numeros que permanecen disponibles para seleccion.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PuedeGenerarAlAzar))]
        public partial int TotalDisponibles { get; set; }

        /// <summary>
        /// Cantidad de numeros actualmente en estado de apartado.
        /// </summary>
        [ObservableProperty]
        public partial int TotalApartados { get; set; }

        /// <summary>
        /// Cantidad de numeros liquidados y marcados como pagados.
        /// </summary>
        [ObservableProperty]
        public partial int TotalPagados { get; set; }

        /// <summary>
        /// Porcentaje de boletos colocados (apartados + pagados) entre 0.0 y 1.0 para barras de progreso.
        /// </summary>
        [ObservableProperty]
        public partial double PorcentajeOcupacion { get; set; }

        /// <summary>
        /// Texto porcentual legible para mostrar en la interfaz (ej. "75%").
        /// </summary>
        [ObservableProperty]
        public partial string PorcentajeOcupacionTexto { get; set; } = "0%";

        /// <summary>
        /// Numero de columnas de la cuadricula adaptado ergonomicamente segun las oportunidades del boleto.
        /// </summary>
        [ObservableProperty]
        public partial int ColumnasCuadricula { get; set; } = 5;

        /// <summary>
        /// Indica si el sorteo ya cuenta con ganadores registrados.
        /// </summary>
        [ObservableProperty]
        public partial bool TieneGanadores { get; set; }

        /// <summary>
        /// Resumen legible de los ganadores premiados en el sorteo.
        /// </summary>
        [ObservableProperty]
        public partial string ResumenGanadoresTexto { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el sorteo ya concluyo y se encuentra cerrado para nuevas modificaciones operativas.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PuedeGenerarAlAzar))]
        public partial bool EsSorteoFinalizado { get; set; }

        /// <summary>
        /// Determina si la herramienta de asignacion aleatoria de boletos debe estar habilitada.
        /// </summary>
        public bool PuedeGenerarAlAzar => !EsSorteoFinalizado && FiltroActual == "Disponibles" && TotalDisponibles > 0;

        /// <summary>
        /// Criterio de filtro activo ("Disponibles", "Apartados", "Pagados", "Todos").
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsVistaNumeros))]
        [NotifyPropertyChangedFor(nameof(EsVistaApartados))]
        [NotifyPropertyChangedFor(nameof(EsVistaPagados))]
        [NotifyPropertyChangedFor(nameof(PuedeGenerarAlAzar))]
        public partial string FiltroActual { get; set; } = "Disponibles";

        /// <summary>
        /// Termino de busqueda para localizar boletos o clientes especificos en la cuadricula o listados.
        /// </summary>
        [ObservableProperty]
        public partial string TextoBusqueda { get; set; } = string.Empty;

        partial void OnTextoBusquedaChanged(string value)
        {
            DesmarcarSeleccionados();
            TieneNumeroSeleccionado = false;
            if (SorteoActual != null)
            {
                _ = CargarCuadriculaAsync(SorteoActual.Id, resetCursor: true);
                if (EsVistaApartados)
                {
                    FiltrarClientesApartados(value);
                }
                else if (EsVistaPagados)
                {
                    FiltrarClientesPagados(value);
                }
            }
        }

        [ObservableProperty]
        public partial NumeroGridItem? NumeroSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool TieneNumeroSeleccionado { get; set; }

        [ObservableProperty]
        public partial string DetalleEstadoTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string DetalleClienteTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool PuedeApartarSeleccionado { get; set; }

        [ObservableProperty]
        public partial string TextoBotonApartar { get; set; } = "Apartar";

        [ObservableProperty]
        public partial bool PuedeCambiarPagoSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool PuedeLiberarSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool PuedeCompartirWhatsApp { get; set; }

        [ObservableProperty]
        public partial bool MostrarVisorComprobante { get; set; }

        [ObservableProperty]
        public partial string ImagenComprobanteVisor { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string TituloComprobanteVisor { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool MostrarModalApartado { get; set; }

        [ObservableProperty]
        public partial string NombreClienteApartado { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string RutaComprobanteApartado { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NoTieneComprobanteApartado))]
        public partial bool TieneComprobanteApartado { get; set; }

        public bool NoTieneComprobanteApartado => !TieneComprobanteApartado;

        [ObservableProperty]
        public partial string ResumenNumerosApartado { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool MostrarModalFlyer { get; set; }

        [ObservableProperty]
        public partial string TextoDisponiblesFlyer { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int CantidadDisponiblesFlyer { get; set; }

        // Delegado asignado por la vista para capturar la tarjeta del flyer
        public Func<Task<string?>>? SolicitarCapturaFlyerAsync { get; set; }

        [ObservableProperty]
        public partial bool TieneApartadosSeleccionados { get; set; }

        [ObservableProperty]
        public partial int CantidadApartadosSeleccionados { get; set; }

        [ObservableProperty]
        public partial string TotalSeleccionadosTexto { get; set; } = string.Empty;

        // Propiedades computadas para controlar que vista se muestra en Fila 2
        public bool EsVistaNumeros   => FiltroActual != "Apartados" && FiltroActual != "Pagados";
        public bool EsVistaApartados => FiltroActual == "Apartados";
        public bool EsVistaPagados   => FiltroActual == "Pagados";

        // Coleccion de clientes con sus numeros apartados — visible cuando FiltroActual == "Apartados"
        public ObservableCollection<ClienteConApartadosDto> ClientesConApartados { get; } = [];

        // Coleccion de clientes con sus numeros pagados — visible cuando FiltroActual == "Pagados"
        public ObservableCollection<ClienteConApartadosDto> ClientesPagados { get; } = [];

        [ObservableProperty]
        public partial string TextoOrdenPagados { get; set; } = "⇅ Ordenar";

        private string _criterioOrdenPagados = "NombreAsc";

        // Coleccion observable de numeros en seleccion activa — expuesta al XAML para los chips del panel inferior
        public ObservableCollection<NumeroGridItem> NumerosEnSeleccion { get; } = [];

        public ObservableCollection<NumeroGridItem> NumerosVisibles { get; } = [];

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ReservasSorteoViewModel"/> con servicios de base de datos y comprobantes.
        /// </summary>
        /// <param name="databaseService">Servicio de operaciones SQLite.</param>
        /// <param name="comprobanteStorageService">Servicio de gestion de archivos multimedia de comprobantes.</param>
        public ReservasSorteoViewModel(
            ILocalDatabaseService databaseService,
            IComprobanteStorageService comprobanteStorageService)
        {
            _databaseService = databaseService;
            _comprobanteStorageService = comprobanteStorageService;
        }

        /// <summary>
        /// Responde a la recepcion del parametro de navegacion cargando el sorteo especificado.
        /// </summary>
        async partial void OnIdSorteoQueryChanged(string value)
        {
            if (int.TryParse(value, out int idSorteo) && idSorteo > 0)
            {
                await CargarDatosSorteoAsync(idSorteo);
            }
        }

        /// <summary>
        /// Recupera y estructura toda la informacion inicial del sorteo, ajustando las columnas segun oportunidades.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Configura el numero optimo de columnas (5 para 1 oportunidad, 3 para 2, 2 para 3 o mas)
        /// para que las combinaciones numéricas se lean sin apiñamiento en la pantalla del dispositivo movil.
        /// </remarks>
        /// <param name="idSorteo">Identificador del sorteo a consultar.</param>
        public async Task CargarDatosSorteoAsync(int idSorteo)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                SorteoPlantilla? sorteo = await _databaseService.ObtenerSorteoPorIdAsync(idSorteo);
                if (sorteo == null)
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No se encontró la información del sorteo especificado.", "Aceptar");
                    return;
                }

                SorteoActual = sorteo;
                EsSorteoFinalizado = sorteo.EsSorteoFinalizado;
                TituloSorteo = sorteo.Descripcion;
                int totalBoletos = sorteo.Oportunidades > 1
                    ? sorteo.CantidadNumeros / sorteo.Oportunidades
                    : sorteo.CantidadNumeros;
                SubtituloSorteo = $"Folio #{sorteo.NumeroDeSorteo} • {sorteo.CantidadNumeros} números • {sorteo.Oportunidades} oportun. • {totalBoletos} boletos";
                ModalidadJuego = sorteo.SeJuegaCon;
                CostoBoletoTexto = $"${sorteo.Costo:N2}";

                ColumnasCuadricula = sorteo.Oportunidades switch
                {
                    <= 1 => 5,
                    2 => 3,
                    _ => 2
                };

                DesmarcarSeleccionados();
                TieneNumeroSeleccionado = false;

                Task cuadriculaTask = CargarCuadriculaAsync(idSorteo, resetCursor: true);
                Task metricasTask   = CargarMetricasAsync(idSorteo);
                await Task.WhenAll(cuadriculaTask, metricasTask);
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Error de base de datos", "Ocurrió un problema al leer la cuadrícula de números.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible cargar las reservas del sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarCuadriculaAsync(int idSorteo, bool resetCursor)
        {
            if (resetCursor)
            {
                NumerosVisibles.Clear();
                _cursorActual  = -1;
                _hayMasPaginas = true;
            }

            if (!_hayMasPaginas) return;

            List<NumeroGridItem> lote = await _databaseService.ObtenerNumerosPaginadosAsync(
                idSorteo, FiltroActual, _cursorActual, TamanoLote, TextoBusqueda);

            for (int i = 0; i < lote.Count; i++)
            {
                NumerosVisibles.Add(lote[i]);
            }

            if (lote.Count > 0)
            {
                int ultimoNumeroValor = lote[^1].NumeroValor;
                _cursorActual = ultimoNumeroValor == 0 ? 99999 : ultimoNumeroValor;
            }

            if (lote.Count < TamanoLote)
                _hayMasPaginas = false;
        }

        private async Task CargarMetricasAsync(int idSorteo)
        {
            MetricasSorteoDto metricas = await _databaseService.ObtenerMetricasSorteoAsync(idSorteo);

            TotalNumeros     = metricas.TotalNumeros;
            TotalDisponibles = metricas.TotalDisponibles;
            TotalApartados   = metricas.TotalApartados;
            TotalPagados     = metricas.TotalPagados;

            int ocupados = TotalApartados + TotalPagados;
            if (TotalNumeros > 0)
            {
                PorcentajeOcupacion     = (double)ocupados / TotalNumeros;
                PorcentajeOcupacionTexto = $"{(PorcentajeOcupacion * 100):0.0}%";
            }
            else
            {
                PorcentajeOcupacion      = 0;
                PorcentajeOcupacionTexto = "0%";
            }

            List<ReservaPremiada> premiosGanadores = await _databaseService.ObtenerPremiosGanadoresPorSorteoAsync(idSorteo);
            if (premiosGanadores.Count > 0)
            {
                TieneGanadores = true;
                ResumenGanadoresTexto = string.Join("  •  ", premiosGanadores.Select(p => $"Boleto #{p.NumeroGanador:D2} ({p.DescripcionPremio}) — {p.NombreGanador}"));
            }
            else
            {
                TieneGanadores = false;
                ResumenGanadoresTexto = string.Empty;
            }
        }

        private async Task CargarMetricasEnSegundoPlanoAsync()
        {
            if (_actualizandoMetricas || SorteoActual == null) return;
            _actualizandoMetricas = true;
            try
            {
                await CargarMetricasAsync(SorteoActual.Id);
            }
            catch (SQLiteException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Aviso no critico al actualizar metricas en segundo plano: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error no critico al actualizar metricas en segundo plano: {ex.Message}");
            }
            finally
            {
                _actualizandoMetricas = false;
            }
        }

        [RelayCommand]
        private async Task CambiarFiltro(string nuevoFiltro)
        {
            if (string.IsNullOrWhiteSpace(nuevoFiltro) || SorteoActual == null) return;
            FiltroActual = nuevoFiltro;
            DesmarcarSeleccionados();
            TieneNumeroSeleccionado = false;
            LimpiarSeleccionApartados();

            if (nuevoFiltro == "Apartados")
            {
                ClientesPagados.Clear();
                await Task.WhenAll(
                    CargarCuadriculaAsync(SorteoActual.Id, resetCursor: true),
                    CargarClientesConApartadosAsync(SorteoActual.Id));
            }
            else if (nuevoFiltro == "Pagados")
            {
                ClientesConApartados.Clear();
                await Task.WhenAll(
                    CargarCuadriculaAsync(SorteoActual.Id, resetCursor: true),
                    CargarClientesPagadosAsync(SorteoActual.Id));
            }
            else
            {
                ClientesConApartados.Clear();
                ClientesPagados.Clear();
                await CargarCuadriculaAsync(SorteoActual.Id, resetCursor: true);
            }
        }

        [RelayCommand]
        private void AlternarSeleccionApartado(ClienteConApartadosDto apartado)
        {
            if (apartado == null) return;
            apartado.EstaSeleccionado = !apartado.EstaSeleccionado;
            ActualizarEstadoSeleccionApartados();
        }

        [RelayCommand]
        private void AlternarSeleccionTodosApartados()
        {
            bool haySinSeleccionar = false;
            for (int i = 0; i < ClientesConApartados.Count; i++)
            {
                if (!ClientesConApartados[i].TieneComprobante && !ClientesConApartados[i].EstaSeleccionado)
                {
                    haySinSeleccionar = true;
                    break;
                }
            }

            for (int i = 0; i < ClientesConApartados.Count; i++)
            {
                if (!ClientesConApartados[i].TieneComprobante)
                {
                    ClientesConApartados[i].EstaSeleccionado = haySinSeleccionar;
                }
            }

            ActualizarEstadoSeleccionApartados();
        }

        [RelayCommand]
        private void LimpiarSeleccionApartados()
        {
            for (int i = 0; i < ClientesConApartados.Count; i++)
            {
                ClientesConApartados[i].EstaSeleccionado = false;
            }
            ActualizarEstadoSeleccionApartados();
        }

        private void ActualizarEstadoSeleccionApartados()
        {
            if (EsSorteoFinalizado)
            {
                CantidadApartadosSeleccionados = 0;
                TieneApartadosSeleccionados = false;
                TotalSeleccionadosTexto = string.Empty;
                return;
            }

            int conteo = 0;
            for (int i = 0; i < ClientesConApartados.Count; i++)
            {
                if (ClientesConApartados[i].EstaSeleccionado)
                {
                    conteo++;
                }
            }

            CantidadApartadosSeleccionados = conteo;
            TieneApartadosSeleccionados = conteo > 0;
            TotalSeleccionadosTexto = $"{conteo} {(conteo == 1 ? "apartado seleccionado" : "apartados seleccionados")}";
        }

        [RelayCommand]
        private async Task PagarSeleccionadosEfectivoAsync()
        {
            List<ClienteConApartadosDto> seleccionados = [];
            for (int i = 0; i < ClientesConApartados.Count; i++)
            {
                if (ClientesConApartados[i].EstaSeleccionado)
                {
                    seleccionados.Add(ClientesConApartados[i]);
                }
            }

            if (seleccionados.Count == 0) return;

            bool confirmar = await Shell.Current.DisplayAlertAsync(
                "Cobro en Efectivo",
                $"Se registrarán como Pagados en Efectivo {seleccionados.Count} apartados. ¿Deseas continuar?",
                "Sí, registrar pago",
                "Cancelar");

            if (!confirmar) return;

            List<int> reservaIds = seleccionados.Select(s => s.ReservaId).ToList();

            try
            {
                // Actualización masiva en un solo comando SQL Batch (cero N+1)
                await _databaseService.RegistrarPagoEfectivoMasivoAsync(reservaIds);

                // Mutación atómica en memoria
                for (int i = 0; i < seleccionados.Count; i++)
                {
                    ClienteConApartadosDto item = seleccionados[i];
                    item.EstaSeleccionado = false;
                    item.IdEstatus = 2;
                    item.FormaDePago = "Efectivo";

                    ClientesConApartados.Remove(item);
                    ClientesPagados.Insert(0, item);
                }

                ActualizarEstadoSeleccionApartados();
                _ = CargarMetricasEnSegundoPlanoAsync();

                await Shell.Current.DisplayAlertAsync(
                    "Pago Registrado",
                    $"Se registraron exitosamente {reservaIds.Count} apartados como Pagados en Efectivo.",
                    "Aceptar");
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible actualizar los pagos en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al registrar el pago en efectivo.", "Aceptar");
            }
        }

        private List<ClienteConApartadosDto> _todosApartados = [];
        private List<ClienteConApartadosDto> _todosPagados = [];

        private async Task CargarClientesConApartadosAsync(int idSorteo)
        {
            _todosApartados = await _databaseService.ObtenerClientesConApartadosAsync(idSorteo);
            FiltrarClientesApartados(TextoBusqueda);
        }

        private void FiltrarClientesApartados(string? filtro)
        {
            ClientesConApartados.Clear();
            string busqueda = (filtro ?? string.Empty).Trim().ToLowerInvariant();
            for (int i = 0; i < _todosApartados.Count; i++)
            {
                ClienteConApartadosDto item = _todosApartados[i];
                if (string.IsNullOrWhiteSpace(busqueda) ||
                    item.NombreCliente.ToLowerInvariant().Contains(busqueda) ||
                    item.NumerosTexto.Contains(busqueda))
                {
                    item.AlCambiarSeleccion = ActualizarEstadoSeleccionApartados;
                    ClientesConApartados.Add(item);
                }
            }
            ActualizarEstadoSeleccionApartados();
        }

        private async Task CargarClientesPagadosAsync(int idSorteo)
        {
            _todosPagados = await _databaseService.ObtenerClientesPorEstatusAsync(idSorteo, idEstatus: 2);
            FiltrarClientesPagados(TextoBusqueda);
            AplicarOrdenPagados();
        }

        private void FiltrarClientesPagados(string? filtro)
        {
            ClientesPagados.Clear();
            string busqueda = (filtro ?? string.Empty).Trim().ToLowerInvariant();
            for (int i = 0; i < _todosPagados.Count; i++)
            {
                ClienteConApartadosDto item = _todosPagados[i];
                if (string.IsNullOrWhiteSpace(busqueda) ||
                    item.NombreCliente.ToLowerInvariant().Contains(busqueda) ||
                    item.NumerosTexto.Contains(busqueda))
                {
                    ClientesPagados.Add(item);
                }
            }
        }

        [RelayCommand]
        private async Task CambiarOrdenPagadosAsync()
        {
            if (ClientesPagados.Count == 0) return;

            string? seleccion = await Shell.Current.DisplayActionSheet(
                "Ordenar lista de pagados",
                "Cancelar",
                null,
                "Nombre (A - Z)",
                "Nombre (Z - A)",
                "Número (Menor a Mayor)",
                "Número (Mayor a Menor)");

            if (string.IsNullOrWhiteSpace(seleccion) || seleccion == "Cancelar") return;

            switch (seleccion)
            {
                case "Nombre (A - Z)":
                    _criterioOrdenPagados = "NombreAsc";
                    TextoOrdenPagados = "⇅ Nombre (A-Z)";
                    break;
                case "Nombre (Z - A)":
                    _criterioOrdenPagados = "NombreDesc";
                    TextoOrdenPagados = "⇅ Nombre (Z-A)";
                    break;
                case "Número (Menor a Mayor)":
                    _criterioOrdenPagados = "NumeroAsc";
                    TextoOrdenPagados = "⇅ Número (Menor)";
                    break;
                case "Número (Mayor a Menor)":
                    _criterioOrdenPagados = "NumeroDesc";
                    TextoOrdenPagados = "⇅ Número (Mayor)";
                    break;
            }

            AplicarOrdenPagados();
        }

        private void AplicarOrdenPagados()
        {
            if (ClientesPagados.Count <= 1) return;

            List<ClienteConApartadosDto> ordenados = _criterioOrdenPagados switch
            {
                "NombreDesc" => ClientesPagados.OrderByDescending(x => x.NombreCliente).ToList(),
                "NumeroAsc" => ClientesPagados.OrderBy(x => x.PrimerNumero).ToList(),
                "NumeroDesc" => ClientesPagados.OrderByDescending(x => x.PrimerNumero).ToList(),
                _ => ClientesPagados.OrderBy(x => x.NombreCliente).ToList()
            };

            for (int i = 0; i < ordenados.Count; i++)
            {
                int indiceActual = ClientesPagados.IndexOf(ordenados[i]);
                if (indiceActual != i && indiceActual >= 0)
                {
                    ClientesPagados.Move(indiceActual, i);
                }
            }
        }

        [RelayCommand]
        private async Task VerComprobante(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No se encontro el archivo del comprobante en el dispositivo.", "Aceptar");
                return;
            }

            ImagenComprobanteVisor = ruta;
            TituloComprobanteVisor = "Comprobante de Pago";
            MostrarVisorComprobante = true;
        }

        [RelayCommand]
        private void CerrarVisorComprobante()
        {
            MostrarVisorComprobante = false;
            ImagenComprobanteVisor = string.Empty;
            TituloComprobanteVisor = string.Empty;
        }

        [RelayCommand]
        private async Task SubirComprobante(ClienteConApartadosDto? reserva)
        {
            if (reserva == null) return;

            string? rutaFoto = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(reserva.ReservaId);
            if (string.IsNullOrWhiteSpace(rutaFoto)) return;

            try
            {
                string formaPago = reserva.EsPagoElectronico ? reserva.FormaDePago : "Transferencia";

                await _databaseService.ActualizarPagoReservaAsync(
                    reserva.ReservaId, reserva.IdEstatus, formaPago, rutaFoto);

                reserva.FormaDePago = formaPago;
                reserva.ComprobanteUrl = rutaFoto;

                await Shell.Current.DisplayAlertAsync("Comprobante guardado", "El comprobante de pago se ha vinculado correctamente.", "Aceptar");
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible registrar el comprobante en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrio un problema inesperado al guardar el comprobante.", "Aceptar");
            }
        }

        [RelayCommand]
        private async Task GestionarReserva(ClienteConApartadosDto? reserva)
        {
            if (reserva == null || SorteoActual == null) return;

            string etiquetaPago = reserva.IdEstatus == 2
                ? "Marcar como pendiente de pago"
                : "Marcar como pagado";

            List<string> listaOpciones = [];

            if (!EsSorteoFinalizado)
            {
                listaOpciones.Add("Liberar reserva");
                listaOpciones.Add(etiquetaPago);

                if (reserva.IdEstatus == 2)
                {
                    listaOpciones.Add(reserva.TieneComprobante ? "Cambiar comprobante" : "Subir comprobante");
                    if (reserva.TieneComprobante)
                    {
                        listaOpciones.Add("Ver comprobante de pago");
                    }
                }
            }
            else
            {
                if (reserva.IdEstatus == 2 && reserva.TieneComprobante)
                {
                    listaOpciones.Add("Ver comprobante de pago");
                }
            }

            listaOpciones.Add("Enviar por WhatsApp");

            string? accion = await Shell.Current.DisplayActionSheet(
                $"{reserva.NombreCliente}  —  {reserva.NumerosTexto}",
                "Cerrar",
                null,
                [.. listaOpciones]);

            if (string.IsNullOrWhiteSpace(accion) || accion == "Cerrar") return;

            if (accion == "Liberar reserva")
            {
                bool confirmar = await Shell.Current.DisplayAlertAsync(
                    "Liberar reserva",
                    $"Se liberaran los numeros {reserva.NumerosTexto} reservados a nombre de {reserva.NombreCliente}. Esta accion no puede deshacerse.",
                    "Si, liberar",
                    "Cancelar");

                if (!confirmar) return;

                try
                {
                    await _databaseService.LiberarReservaCompletaAsync(reserva.ReservaId);
                    ClientesConApartados.Remove(reserva);
                    ClientesPagados.Remove(reserva);
                    _ = CargarMetricasEnSegundoPlanoAsync();
                }
                catch (SQLiteException)
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible liberar la reserva en la base de datos.", "Aceptar");
                }
                catch (Exception)
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrio un error inesperado al liberar la reserva.", "Aceptar");
                }
            }
            else if (accion == "Subir comprobante" || accion == "Cambiar comprobante")
            {
                await SubirComprobante(reserva);
            }
            else if (accion == "Ver comprobante de pago")
            {
                await VerComprobante(reserva.ComprobanteUrl);
            }
            else if (accion == etiquetaPago)
            {
                if (reserva.IdEstatus == 2)
                {
                    // Pasar de Pagado a Pendiente de pago
                    try
                    {
                        await _databaseService.ActualizarPagoReservaAsync(
                            reserva.ReservaId, 1, reserva.FormaDePago, reserva.ComprobanteUrl);
                        reserva.IdEstatus = 1;
                        ClientesPagados.Remove(reserva);
                        if (FiltroActual == "Apartados")
                        {
                            ClientesConApartados.Insert(0, reserva);
                        }
                        _ = CargarMetricasEnSegundoPlanoAsync();
                    }
                    catch (SQLiteException)
                    {
                        await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible actualizar el estatus de pago en la base de datos.", "Aceptar");
                    }
                    catch (Exception)
                    {
                        await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrio un error inesperado al cambiar el estatus de pago.", "Aceptar");
                    }
                }
                else
                {
                    // Pasar de Apartado a Pagado
                    string? metodoPago = await Shell.Current.DisplayActionSheet(
                        "Selecciona la forma de pago",
                        "Cancelar",
                        null,
                        "Efectivo",
                        "Pago Electronico (Transferencia / Tarjeta)");

                    if (string.IsNullOrWhiteSpace(metodoPago) || metodoPago == "Cancelar") return;

                    string formaPagoPersistir = "Efectivo";
                    string comprobantePersistir = string.Empty;

                    if (metodoPago == "Pago Electronico (Transferencia / Tarjeta)")
                    {
                        formaPagoPersistir = "Transferencia";
                        bool adjuntar = await Shell.Current.DisplayAlertAsync(
                            "Comprobante de pago",
                            "Deseas adjuntar una foto o imagen del comprobante?",
                            "Si, adjuntar",
                            "Omitir por ahora");

                        if (adjuntar)
                        {
                            string? rutaFoto = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(reserva.ReservaId);
                            if (!string.IsNullOrWhiteSpace(rutaFoto))
                            {
                                comprobantePersistir = rutaFoto;
                            }
                        }
                    }

                    try
                    {
                        await _databaseService.ActualizarPagoReservaAsync(
                            reserva.ReservaId, 2, formaPagoPersistir, comprobantePersistir);
                        reserva.IdEstatus = 2;
                        reserva.FormaDePago = formaPagoPersistir;
                        reserva.ComprobanteUrl = comprobantePersistir;

                        ClientesConApartados.Remove(reserva);
                        if (FiltroActual == "Pagados")
                        {
                            ClientesPagados.Insert(0, reserva);
                        }
                        _ = CargarMetricasEnSegundoPlanoAsync();
                    }
                    catch (SQLiteException)
                    {
                        await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible registrar el pago en la base de datos.", "Aceptar");
                    }
                    catch (Exception)
                    {
                        await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrio un error inesperado al registrar el pago.", "Aceptar");
                    }
                }
            }
            else if (accion == "Enviar por WhatsApp")
            {
                string estadoTexto = reserva.IdEstatus == 2 ? "Pagado" : "Apartado pendiente de pago";
                string mensaje = $"Hola {reserva.NombreCliente}, te compartimos el detalle de tu reserva para el sorteo " +
                                 $"'{SorteoActual.Descripcion}' (Folio #{SorteoActual.NumeroDeSorteo}):\n" +
                                 $"- Numeros: {reserva.NumerosTexto}\n" +
                                 $"- Estatus: {estadoTexto}";

                string url = $"https://wa.me/?text={Uri.EscapeDataString(mensaje)}";
                try
                {
                    await Launcher.OpenAsync(new Uri(url));
                }
                catch (Exception)
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No se pudo abrir WhatsApp en este dispositivo.", "Aceptar");
                }
            }
        }

        private void EliminarDeVistaItems(List<NumeroGridItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                NumerosVisibles.Remove(items[i]);
            }
        }

        [RelayCommand]
        private async Task CargarMasNumeros()
        {
            if (IsCargandoMas || !_hayMasPaginas || SorteoActual == null) return;
            IsCargandoMas = true;
            try
            {
                await CargarCuadriculaAsync(SorteoActual.Id, resetCursor: false);
            }
            finally
            {
                IsCargandoMas = false;
            }
        }

        [RelayCommand]
        private void SeleccionarNumero(NumeroGridItem? item)
        {
            if (item == null) return;

            // Si ya esta seleccionado, lo quita de la seleccion (toggle off)
            if (item.EstaSeleccionado)
            {
                QuitarDeSeleccion(item);
                return;
            }

            // Agrega a la seleccion activa (no reemplaza)
            item.EstaSeleccionado = true;
            NumerosEnSeleccion.Add(item);
            NumeroSeleccionado    = item;
            TieneNumeroSeleccionado = true;
            ActualizarDetalleMultiples([.. NumerosEnSeleccion]);
        }

        [RelayCommand]
        private void QuitarDeSeleccion(NumeroGridItem? item)
        {
            if (item == null) return;

            item.EstaSeleccionado = false;
            NumerosEnSeleccion.Remove(item);

            if (NumerosEnSeleccion.Count == 0)
            {
                NumeroSeleccionado      = null;
                TieneNumeroSeleccionado = false;
            }
            else
            {
                NumeroSeleccionado = NumerosEnSeleccion[0];
                ActualizarDetalleMultiples([.. NumerosEnSeleccion]);
            }
        }

        private void DesmarcarSeleccionados()
        {
            for (int i = 0; i < NumerosVisibles.Count; i++)
            {
                if (NumerosVisibles[i].EstaSeleccionado)
                    NumerosVisibles[i].EstaSeleccionado = false;
            }

            NumerosEnSeleccion.Clear();
            NumeroSeleccionado = null;
        }

        private void ActualizarDetalleMultiples(List<NumeroGridItem> items)
        {
            if (items.Count == 0)
            {
                TieneNumeroSeleccionado = false;
                return;
            }

            bool todosDisponibles = true;
            bool algunOcupado    = false;
            string cliente       = string.Empty;

            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i].EstaDisponible)
                {
                    todosDisponibles = false;
                    algunOcupado     = true;
                }

                if (string.IsNullOrWhiteSpace(cliente) && !string.IsNullOrWhiteSpace(items[i].NombreCliente))
                    cliente = items[i].NombreCliente;
            }

            if (todosDisponibles)
            {
                DetalleEstadoTexto           = EsSorteoFinalizado
                    ? (items.Count == 1 ? "Boleto Desierto / No Vendido" : $"{items.Count} boletos no vendidos")
                    : (items.Count == 1 ? "Disponible" : $"{items.Count} números disponibles al azar");
                DetalleClienteTexto          = "Sin asignar";
                TextoBotonApartar            = "Apartar";
                PuedeApartarSeleccionado     = !EsSorteoFinalizado;
                PuedeCambiarPagoSeleccionado = !EsSorteoFinalizado;
                PuedeLiberarSeleccionado     = false;
                PuedeCompartirWhatsApp       = false;
            }
            else
            {
                DetalleEstadoTexto           = items.Count == 1
                    ? items[0].Estado switch
                    {
                        EstadoNumeroSorteo.Apartado => "Apartado (Pendiente de pago)",
                        EstadoNumeroSorteo.Pagado   => "Pagado",
                        EstadoNumeroSorteo.Ganador  => "Número Ganador",
                        EstadoNumeroSorteo.Planta   => "Número de Planta",
                        _                           => "Sin definir"
                    }
                    : $"{items.Count} números seleccionados";
                DetalleClienteTexto          = !string.IsNullOrWhiteSpace(cliente) ? cliente : "Cliente General";
                TextoBotonApartar            = algunOcupado ? "Gestionar Reserva" : "Apartar";
                PuedeApartarSeleccionado     = !EsSorteoFinalizado;
                PuedeCambiarPagoSeleccionado = !EsSorteoFinalizado;
                PuedeLiberarSeleccionado     = algunOcupado && !EsSorteoFinalizado;
                PuedeCompartirWhatsApp       = !string.IsNullOrWhiteSpace(cliente);
            }

            TieneNumeroSeleccionado = true;
        }

        [RelayCommand]
        private async Task ApartarSeleccionadoAsync()
        {
            if (NumerosEnSeleccion.Count == 0 || SorteoActual == null) return;

            if (EsSorteoFinalizado)
            {
                await Shell.Current.DisplayAlertAsync("Sorteo Cerrado", "El sorteo se encuentra finalizado o cancelado. No es posible realizar modificaciones.", "Aceptar");
                return;
            }

            bool todosDisponibles = true;
            bool hayApartados = false;
            HashSet<int> reservasIds = [];

            for (int i = 0; i < NumerosEnSeleccion.Count; i++)
            {
                NumeroGridItem item = NumerosEnSeleccion[i];
                if (!item.EstaDisponible)
                {
                    todosDisponibles = false;
                }
                if (item.Estado == EstadoNumeroSorteo.Apartado)
                {
                    hayApartados = true;
                }
                if (item.ReservaId.HasValue)
                {
                    reservasIds.Add(item.ReservaId.Value);
                }
            }

            try
            {
                // Caso 1: Los números están Disponibles -> Abrir Formulario/Modal de Apartado
                if (todosDisponibles)
                {
                    NombreClienteApartado = string.Empty;
                    RutaComprobanteApartado = string.Empty;
                    TieneComprobanteApartado = false;

                    if (NumerosEnSeleccion.Count == 1)
                    {
                        ResumenNumerosApartado = $"Boleto: #{NumerosEnSeleccion[0].NumeroFormateado} - Costo: {SorteoActual.CostoFormateado}";
                    }
                    else
                    {
                        decimal costoTotal = SorteoActual.Costo * NumerosEnSeleccion.Count;
                        ResumenNumerosApartado = $"{NumerosEnSeleccion.Count} boletos seleccionados - Total: ${costoTotal:N2}";
                    }

                    MostrarModalApartado = true;
                    return;
                }

                // Caso 2: Los números ya están Apartados -> Gestión directa
                if (hayApartados && reservasIds.Count > 0)
                {
                    string? accion = await Shell.Current.DisplayActionSheet(
                        "Gestionar reserva",
                        "Cancelar",
                        null,
                        "Marcar como Pagado",
                        "Subir o cambiar comprobante");

                    if (string.IsNullOrWhiteSpace(accion) || accion == "Cancelar") return;

                    if (accion == "Marcar como Pagado")
                    {
                        string? metodo = await Shell.Current.DisplayActionSheet(
                            "Forma de pago",
                            "Cancelar",
                            null,
                            "Efectivo",
                            "Pago Electronico (Transferencia / Tarjeta)");

                        if (string.IsNullOrWhiteSpace(metodo) || metodo == "Cancelar") return;

                        string forma = (metodo == "Efectivo") ? "Efectivo" : "Transferencia";
                        string fotoComp = string.Empty;

                        if (forma == "Transferencia")
                        {
                            bool adjuntar = await Shell.Current.DisplayAlertAsync(
                                "Comprobante",
                                "¿Deseas adjuntar el comprobante ahora?",
                                "Sí, adjuntar",
                                "Omitir");

                            if (adjuntar)
                            {
                                int idUnica = reservasIds.First();
                                string? f = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(idUnica);
                                if (!string.IsNullOrWhiteSpace(f)) fotoComp = f;
                            }
                        }

                        foreach (int idRes in reservasIds)
                        {
                            await _databaseService.ActualizarPagoReservaAsync(idRes, 2, forma, fotoComp);
                        }

                        for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                        {
                            if (NumerosEnSeleccion[i].Estado == EstadoNumeroSorteo.Apartado)
                                NumerosEnSeleccion[i].Estado = EstadoNumeroSorteo.Pagado;
                        }

                        _ = CargarMetricasEnSegundoPlanoAsync();
                        ProcesarCambioDeFiltroEnSeleccion();
                    }
                    else if (accion == "Subir o cambiar comprobante")
                    {
                        int idRes = reservasIds.First();
                        string? f = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(idRes);
                        if (!string.IsNullOrWhiteSpace(f))
                        {
                            foreach (int id in reservasIds)
                            {
                                await _databaseService.ActualizarPagoReservaAsync(id, 1, "Transferencia", f);
                            }
                            await Shell.Current.DisplayAlertAsync("Comprobante", "Comprobante adjuntado a la reserva.", "Aceptar");
                        }
                    }
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible registrar la reserva en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al procesar la reserva.", "Aceptar");
            }
        }

        [RelayCommand]
        private async Task AdjuntarComprobanteApartadoAsync()
        {
            string? ruta = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(0);
            if (!string.IsNullOrWhiteSpace(ruta))
            {
                RutaComprobanteApartado = ruta;
                TieneComprobanteApartado = true;
            }
        }

        [RelayCommand]
        private void QuitarComprobanteApartado()
        {
            RutaComprobanteApartado = string.Empty;
            TieneComprobanteApartado = false;
        }

        [RelayCommand]
        private void CerrarModalApartado()
        {
            MostrarModalApartado = false;
        }

        [RelayCommand]
        private async Task ConfirmarApartadoAsync()
        {
            if (NumerosEnSeleccion.Count == 0 || SorteoActual == null) return;

            string nombreLimpio = TextoHelper.Clean(NombreClienteApartado);
            if (string.IsNullOrWhiteSpace(nombreLimpio))
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Por favor ingresa el nombre del cliente para registrar la reserva.", "Aceptar");
                return;
            }

            try
            {
                // Si el usuario subió comprobante, se registra como Pagado con transferencia.
                // Si no subió nada, se registra como Apartado (Pendiente de pago) con efectivo.
                int idEstatusFinal = TieneComprobanteApartado ? 2 : 1;
                string formaPago = TieneComprobanteApartado ? "Transferencia" : "Efectivo";
                string comprobanteUrl = TieneComprobanteApartado ? RutaComprobanteApartado : string.Empty;

                List<int> listaValores = [];
                for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                {
                    NumeroGridItem it = NumerosEnSeleccion[i];
                    if (it.NumerosCombo.Count > 0)
                    {
                        listaValores.AddRange(it.NumerosCombo);
                    }
                    else
                    {
                        listaValores.Add(it.NumeroValor);
                    }
                }

                decimal costoTotal = SorteoActual.Costo * NumerosEnSeleccion.Count;

                int reservaId = await _databaseService.RegistrarPagoNumerosDirectoAsync(
                    SorteoActual.Id,
                    idCliente: 0,
                    numeros: listaValores,
                    costo: costoTotal,
                    formaPago: formaPago,
                    nombre: nombreLimpio,
                    comprobanteUrl: comprobanteUrl,
                    idEstatus: idEstatusFinal);

                EstadoNumeroSorteo nuevoEstado = (idEstatusFinal == 2)
                    ? EstadoNumeroSorteo.Pagado
                    : EstadoNumeroSorteo.Apartado;

                for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                {
                    NumeroGridItem item = NumerosEnSeleccion[i];
                    item.ReservaId     = reservaId;
                    item.NombreCliente = nombreLimpio;
                    item.Estado        = nuevoEstado;
                }

                MostrarModalApartado = false;
                _ = CargarMetricasEnSegundoPlanoAsync();
                ProcesarCambioDeFiltroEnSeleccion();
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible registrar la reserva en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al procesar la reserva.", "Aceptar");
            }
        }

        [RelayCommand]
        private async Task AlternarPagoSeleccionadoAsync()
        {
            if (NumerosEnSeleccion.Count == 0 || SorteoActual == null) return;

            try
            {
                bool todosDisponibles = true;
                bool todosPagados = true;
                bool hayApartados = false;
                HashSet<int> reservasIds = [];

                for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                {
                    NumeroGridItem item = NumerosEnSeleccion[i];
                    if (item.EstaDisponible)
                    {
                        todosPagados = false;
                    }
                    else
                    {
                        todosDisponibles = false;
                        if (item.Estado != EstadoNumeroSorteo.Pagado)
                        {
                            todosPagados = false;
                        }
                    }

                    if (item.ReservaId.HasValue)
                    {
                        reservasIds.Add(item.ReservaId.Value);
                    }

                    if (item.Estado == EstadoNumeroSorteo.Apartado)
                    {
                        hayApartados = true;
                    }
                }

                // Caso 1: Los números seleccionados están Disponibles -> Pagar directamente
                if (todosDisponibles)
                {
                    string mensajePrompt = NumerosEnSeleccion.Count == 1
                        ? $"Ingresa el nombre del cliente para registrar el pago del número {NumerosEnSeleccion[0].NumeroFormateado}:"
                        : $"Ingresa el nombre del cliente para registrar el pago de los {NumerosEnSeleccion.Count} números seleccionados:";

                    string nombreCliente = await Shell.Current.DisplayPromptAsync(
                        "Pagar Número",
                        mensajePrompt,
                        "Continuar",
                        "Cancelar",
                        "Ej. Juan Pérez",
                        maxLength: 60);

                    nombreCliente = TextoHelper.Clean(nombreCliente);
                    if (string.IsNullOrWhiteSpace(nombreCliente)) return;

                    string? metodoPago = await Shell.Current.DisplayActionSheet(
                        "Selecciona la forma de pago",
                        "Cancelar",
                        null,
                        "Efectivo",
                        "Pago Electronico (Transferencia / Tarjeta)");

                    if (string.IsNullOrWhiteSpace(metodoPago) || metodoPago == "Cancelar") return;

                    string formaPagoPersistir = "Efectivo";
                    string comprobantePersistir = string.Empty;

                    List<int> listaValores = [];
                    for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                    {
                        NumeroGridItem it = NumerosEnSeleccion[i];
                        if (it.NumerosCombo.Count > 0)
                        {
                            listaValores.AddRange(it.NumerosCombo);
                        }
                        else
                        {
                            listaValores.Add(it.NumeroValor);
                        }
                    }
                    decimal costoTotal = SorteoActual.Costo * NumerosEnSeleccion.Count;
                    string nombreLimpio = nombreCliente.Trim();

                    if (metodoPago == "Pago Electronico (Transferencia / Tarjeta)")
                    {
                        formaPagoPersistir = "Transferencia";
                    }

                    int nuevaReservaId = await _databaseService.RegistrarPagoNumerosDirectoAsync(
                        SorteoActual.Id,
                        idCliente: 0,
                        numeros: listaValores,
                        costo: costoTotal,
                        formaPago: formaPagoPersistir,
                        nombre: nombreLimpio,
                        comprobanteUrl: string.Empty);

                    if (formaPagoPersistir == "Transferencia")
                    {
                        bool adjuntar = await Shell.Current.DisplayAlertAsync(
                            "Comprobante de pago",
                            "Deseas adjuntar una foto o imagen del comprobante?",
                            "Si, adjuntar",
                            "Omitir por ahora");

                        if (adjuntar)
                        {
                            string? rutaFoto = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(nuevaReservaId);
                            if (!string.IsNullOrWhiteSpace(rutaFoto))
                            {
                                comprobantePersistir = rutaFoto;
                                await _databaseService.ActualizarPagoReservaAsync(nuevaReservaId, 2, formaPagoPersistir, comprobantePersistir);
                            }
                        }
                    }

                    for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                    {
                        NumeroGridItem it = NumerosEnSeleccion[i];
                        it.ReservaId = nuevaReservaId;
                        it.NombreCliente = nombreLimpio;
                        it.Estado = EstadoNumeroSorteo.Pagado;
                    }

                    _ = CargarMetricasEnSegundoPlanoAsync();
                    ProcesarCambioDeFiltroEnSeleccion();
                    return;
                }

                // Caso 2: Números en estado Apartado (o selección con apartados) -> Registrar pago
                if (hayApartados)
                {
                    string? metodoPago = await Shell.Current.DisplayActionSheet(
                        "Selecciona la forma de pago",
                        "Cancelar",
                        null,
                        "Efectivo",
                        "Pago Electronico (Transferencia / Tarjeta)");

                    if (string.IsNullOrWhiteSpace(metodoPago) || metodoPago == "Cancelar") return;

                    string formaPagoPersistir = "Efectivo";
                    string comprobantePersistir = string.Empty;

                    if (metodoPago == "Pago Electronico (Transferencia / Tarjeta)")
                    {
                        formaPagoPersistir = "Transferencia";
                        if (reservasIds.Count == 1)
                        {
                            bool adjuntar = await Shell.Current.DisplayAlertAsync(
                                "Comprobante de pago",
                                "Deseas adjuntar una foto o imagen del comprobante?",
                                "Si, adjuntar",
                                "Omitir por ahora");

                            if (adjuntar)
                            {
                                int idUnicaReserva = reservasIds.First();
                                string? rutaFoto = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(idUnicaReserva);
                                if (!string.IsNullOrWhiteSpace(rutaFoto))
                                {
                                    comprobantePersistir = rutaFoto;
                                }
                            }
                        }
                    }

                    foreach (int idReserva in reservasIds)
                    {
                        await _databaseService.ActualizarPagoReservaAsync(idReserva, 2, formaPagoPersistir, comprobantePersistir);
                    }

                    for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                    {
                        NumeroGridItem it = NumerosEnSeleccion[i];
                        if (it.Estado == EstadoNumeroSorteo.Apartado)
                        {
                            it.Estado = EstadoNumeroSorteo.Pagado;
                        }
                    }

                    _ = CargarMetricasEnSegundoPlanoAsync();
                    ProcesarCambioDeFiltroEnSeleccion();
                    return;
                }

                // Caso 3: Todos los números seleccionados ya están Pagados
                if (todosPagados && reservasIds.Count > 0)
                {
                    string? accion = await Shell.Current.DisplayActionSheet(
                        "Gestionar números pagados",
                        "Cancelar",
                        null,
                        "Subir o cambiar comprobante",
                        "Marcar como Apartado (Pendiente de pago)");

                    if (string.IsNullOrWhiteSpace(accion) || accion == "Cancelar") return;

                    if (accion == "Subir o cambiar comprobante")
                    {
                        int idRes = reservasIds.First();
                        string? ruta = await _comprobanteStorageService.CapturarOSeleccionarComprobanteAsync(idRes);
                        if (!string.IsNullOrWhiteSpace(ruta))
                        {
                            foreach (int id in reservasIds)
                            {
                                await _databaseService.ActualizarPagoReservaAsync(id, 2, "Transferencia", ruta);
                            }
                            await Shell.Current.DisplayAlertAsync("Comprobante", "Comprobante guardado correctamente.", "Aceptar");
                        }
                    }
                    else if (accion == "Marcar como Apartado (Pendiente de pago)")
                    {
                        foreach (int idReserva in reservasIds)
                        {
                            await _databaseService.ActualizarPagoReservaAsync(idReserva, 1, "Efectivo", string.Empty);
                        }

                        for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                        {
                            NumerosEnSeleccion[i].Estado = EstadoNumeroSorteo.Apartado;
                        }

                        _ = CargarMetricasEnSegundoPlanoAsync();
                        ProcesarCambioDeFiltroEnSeleccion();
                    }
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible actualizar el estatus de pago en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al gestionar el estado de pago.", "Aceptar");
            }
        }

        private void ProcesarCambioDeFiltroEnSeleccion()
        {
            List<NumeroGridItem> itemsAEliminar = [];
            for (int i = 0; i < NumerosEnSeleccion.Count; i++)
            {
                NumeroGridItem item = NumerosEnSeleccion[i];
                bool perteneceAlFiltro = FiltroActual switch
                {
                    "Disponibles" => item.Estado == EstadoNumeroSorteo.Disponible,
                    "Pagados"     => item.Estado == EstadoNumeroSorteo.Pagado,
                    "Ganadores"   => item.Estado == EstadoNumeroSorteo.Ganador,
                    "Apartados"   => item.Estado == EstadoNumeroSorteo.Apartado,
                    _             => true
                };

                if (!perteneceAlFiltro)
                    itemsAEliminar.Add(item);
            }

            if (itemsAEliminar.Count > 0)
            {
                EliminarDeVistaItems(itemsAEliminar);
                DesmarcarSeleccionados();
                TieneNumeroSeleccionado = false;
            }
            else
            {
                ActualizarDetalleMultiples([.. NumerosEnSeleccion]);
            }
        }

        [RelayCommand]
        private async Task LiberarSeleccionadoAsync()
        {
            if (NumerosEnSeleccion.Count == 0 || SorteoActual == null) return;

            string mensajeConfirmacion = NumerosEnSeleccion.Count == 1
                ? $"¿Deseas liberar el número {NumerosEnSeleccion[0].NumeroFormateado}? Quedará disponible nuevamente."
                : $"¿Deseas liberar los {NumerosEnSeleccion.Count} números seleccionados? Quedarán disponibles nuevamente.";

            bool confirmar = await Shell.Current.DisplayAlertAsync(
                "Liberar Número",
                mensajeConfirmacion,
                "Sí, Liberar",
                "Cancelar");

            if (!confirmar) return;

            try
            {
                HashSet<int> reservasIds = [];
                for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                {
                    if (NumerosEnSeleccion[i].ReservaId.HasValue)
                        reservasIds.Add(NumerosEnSeleccion[i].ReservaId!.Value);
                    else
                    {
                        List<int> valoresALiberar = NumerosEnSeleccion[i].NumerosCombo.Count > 0
                            ? NumerosEnSeleccion[i].NumerosCombo
                            : [NumerosEnSeleccion[i].NumeroValor];
                        for (int v = 0; v < valoresALiberar.Count; v++)
                        {
                            await _databaseService.LiberarNumeroAsync(SorteoActual.Id, valoresALiberar[v]);
                        }
                    }
                }

                foreach (int idReserva in reservasIds)
                {
                    await _databaseService.LiberarReservaCompletaAsync(idReserva);
                }

                for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                {
                    NumeroGridItem item = NumerosEnSeleccion[i];
                    item.ReservaId     = null;
                    item.ClienteId     = null;
                    item.NombreCliente = string.Empty;
                    item.Estado        = EstadoNumeroSorteo.Disponible;
                }

                _ = CargarMetricasEnSegundoPlanoAsync();

                List<NumeroGridItem> itemsAEliminar = [];
                for (int i = 0; i < NumerosEnSeleccion.Count; i++)
                {
                    NumeroGridItem item = NumerosEnSeleccion[i];
                    bool perteneceAlFiltro = FiltroActual switch
                    {
                        "Disponibles" => item.Estado == EstadoNumeroSorteo.Disponible,
                        "Pagados"     => item.Estado == EstadoNumeroSorteo.Pagado,
                        "Ganadores"   => item.Estado == EstadoNumeroSorteo.Ganador,
                        "Apartados"   => item.Estado == EstadoNumeroSorteo.Apartado,
                        _             => true
                    };

                    if (!perteneceAlFiltro)
                        itemsAEliminar.Add(item);
                }

                if (itemsAEliminar.Count > 0)
                {
                    EliminarDeVistaItems(itemsAEliminar);
                    DesmarcarSeleccionados();
                    TieneNumeroSeleccionado = false;
                }
                else
                {
                    ActualizarDetalleMultiples([.. NumerosEnSeleccion]);
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible liberar los números en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al liberar los números.", "Aceptar");
            }
        }

        [RelayCommand]
        private async Task CompartirWhatsAppAsync()
        {
            if (NumerosEnSeleccion.Count == 0 || SorteoActual == null) return;

            string nombreCliente   = NumerosEnSeleccion[0].NombreCliente;
            string listaFormateada = string.Join(", ", NumerosEnSeleccion.Select(s => s.NumeroFormateado));
            decimal totalCosto     = SorteoActual.Costo * NumerosEnSeleccion.Count;
            bool todosPagados      = true;
            for (int i = 0; i < NumerosEnSeleccion.Count; i++)
            {
                if (!NumerosEnSeleccion[i].EstaPagado)
                {
                    todosPagados = false;
                    break;
                }
            }

            string mensaje = NumerosEnSeleccion.Count == 1
                ? $"Hola {nombreCliente}, te compartimos el detalle de tu apartado para el sorteo '{SorteoActual.Descripcion}' (Folio #{SorteoActual.NumeroDeSorteo}):\n" +
                  $"- Número: {NumerosEnSeleccion[0].NumeroFormateado}\n" +
                  $"- Modalidad: {SorteoActual.SeJuegaCon}\n" +
                  $"- Costo: ${SorteoActual.Costo:N2}\n" +
                  $"- Estatus: {(NumerosEnSeleccion[0].EstaPagado ? "Pagado" : "Apartado pendiente de pago")}"
                : $"Hola {nombreCliente}, te compartimos el detalle de tus {NumerosEnSeleccion.Count} números para el sorteo '{SorteoActual.Descripcion}' (Folio #{SorteoActual.NumeroDeSorteo}):\n" +
                  $"- Números: {listaFormateada}\n" +
                  $"- Modalidad: {SorteoActual.SeJuegaCon}\n" +
                  $"- Total: ${totalCosto:N2}\n" +
                  $"- Estatus: {(todosPagados ? "Pagados" : "Apartados pendientes de pago")}";

            string url = $"https://wa.me/?text={Uri.EscapeDataString(mensaje)}";

            try
            {
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No se pudo abrir WhatsApp en este dispositivo.", "Aceptar");
            }
        }

        [RelayCommand]
        private async Task ExportarDisponiblesAsync()
        {
            if (SorteoActual == null) return;

            try
            {
                List<string> disponibles = await _databaseService.ObtenerNumerosDisponiblesFormateadosAsync(SorteoActual.Id);
                if (disponibles.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No hay números disponibles en este sorteo.", "Aceptar");
                    return;
                }

                TextoDisponiblesFlyer = string.Join(", ", disponibles);
                CantidadDisponiblesFlyer = disponibles.Count;
                MostrarModalFlyer = true;
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar los números disponibles en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al preparar la exportación.", "Aceptar");
            }
        }

        [RelayCommand]
        private void CerrarModalFlyer()
        {
            MostrarModalFlyer = false;
        }

        [RelayCommand]
        private async Task DescargarFlyerAsync()
        {
            if (SolicitarCapturaFlyerAsync == null)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "La captura de la imagen no se encuentra disponible en este momento.", "Aceptar");
                return;
            }

            try
            {
                IsBusy = true;
                string? rutaPng = await SolicitarCapturaFlyerAsync();
                if (string.IsNullOrWhiteSpace(rutaPng) || !File.Exists(rutaPng))
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible generar la imagen para descargar.", "Aceptar");
                    return;
                }

                string nombreSugerido = $"Sorteo_{SorteoActual?.NumeroDeSorteo}_Disponibles.png";

                using FileStream stream = File.OpenRead(rutaPng);
                FileSaverResult resultado = await FileSaver.Default.SaveAsync(nombreSugerido, stream, CancellationToken.None);

                if (resultado.IsSuccessful)
                {
                    await Shell.Current.DisplayAlertAsync("Descarga exitosa", "La imagen se guardó correctamente en tu dispositivo.", "Aceptar");
                }
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un problema al guardar la imagen en el dispositivo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CompartirFlyerAsync()
        {
            if (SolicitarCapturaFlyerAsync == null)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "La captura del flyer no se encuentra disponible en este momento.", "Aceptar");
                return;
            }

            try
            {
                IsBusy = true;
                string? rutaPng = await SolicitarCapturaFlyerAsync();
                if (string.IsNullOrWhiteSpace(rutaPng) || !File.Exists(rutaPng))
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible generar la imagen del flyer.", "Aceptar");
                    return;
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Flyer - {SorteoActual?.Descripcion}",
                    File = new ShareFile(rutaPng)
                });
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un problema al compartir la imagen del flyer.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void DisminuirCantidadAzar()
        {
            if (int.TryParse(CantidadAzarTexto, out int cantidad) && cantidad > 1)
                CantidadAzarTexto = (cantidad - 1).ToString();
            else
                CantidadAzarTexto = "1";
        }

        [RelayCommand]
        private void AumentarCantidadAzar()
        {
            int maximo = Math.Max(1, TotalDisponibles > 0 ? TotalDisponibles : TotalNumeros);
            if (int.TryParse(CantidadAzarTexto, out int cantidad))
            {
                if (cantidad < maximo)
                    CantidadAzarTexto = (cantidad + 1).ToString();
            }
            else
            {
                CantidadAzarTexto = "1";
            }
        }

        [RelayCommand]
        private async Task GenerarAzarAsync()
        {
            if (SorteoActual == null) return;

            if (!int.TryParse(CantidadAzarTexto, out int cantidad) || cantidad <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Cantidad inválida", "Por favor ingresa una cantidad mayor a 0 para generar números al azar.", "Aceptar");
                return;
            }

            List<NumeroGridItem> disponibles = await _databaseService.ObtenerNumerosPaginadosAsync(
                SorteoActual.Id, "Disponibles", cursor: -1, limite: int.MaxValue, textoBusqueda: string.Empty);

            if (disponibles.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Sin disponibilidad", "No hay números disponibles en este sorteo.", "Aceptar");
                return;
            }

            int cantidadAReservar = Math.Min(cantidad, disponibles.Count);

            Random rnd = Random.Shared;
            List<NumeroGridItem> seleccionados = disponibles.OrderBy(_ => rnd.Next()).Take(cantidadAReservar).ToList();

            DesmarcarSeleccionados();

            for (int i = 0; i < seleccionados.Count; i++)
            {
                seleccionados[i].EstaSeleccionado = true;
                NumerosEnSeleccion.Add(seleccionados[i]);
            }

            NumeroSeleccionado = seleccionados[0];
            ActualizarDetalleMultiples([.. NumerosEnSeleccion]);

            NumerosVisibles.Clear();
            _hayMasPaginas = false;
            for (int i = 0; i < seleccionados.Count; i++)
            {
                NumerosVisibles.Add(seleccionados[i]);
            }
        }

        [RelayCommand]
        private void CerrarDetalle()
        {
            DesmarcarSeleccionados();
            TieneNumeroSeleccionado = false;
        }
    }
}
