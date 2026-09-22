using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using SQLite;
using System.Collections.ObjectModel;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class SorteosSemanaViewModel : BaseViewModel
    {
        private readonly ILocalDatabaseService _databaseService;

        public Func<Task<string?>>? SolicitarCapturaFlyerGanadoresAsync { get; set; }

        public ObservableCollection<DiaSemanaItem> DiasSemana { get; } = [];

        public ObservableCollection<PremioPorSorteo> PremiosDelSorteo { get; } = [];

        public ObservableCollection<PremioAsignacionDto> PremiosParaCierre { get; } = [];

        [ObservableProperty]
        public partial bool MostrarModalGanadoresFlyer { get; set; }

        [ObservableProperty]
        public partial DiaSemanaItem? DiaSeleccionado { get; set; }

        [ObservableProperty]
        public partial SorteoPlantilla? SorteoActual { get; set; }

        [ObservableProperty]
        public partial bool TieneSorteoDiaSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool PuedeCrearSorteoDiaSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool EsDiaPasadoDiaSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool EsSorteoFinalizado { get; set; }

        [ObservableProperty]
        public partial bool MostrarModalCerrarSorteo { get; set; }

        [ObservableProperty]
        public partial bool TodosPremiosAsignados { get; set; }

        [ObservableProperty]
        public partial string TituloModalCierre { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string MensajeEstadoDia { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int TotalNumeros { get; set; }

        [ObservableProperty]
        public partial int TotalDisponibles { get; set; }

        [ObservableProperty]
        public partial int TotalApartados { get; set; }

        [ObservableProperty]
        public partial int TotalPagados { get; set; }

        [ObservableProperty]
        public partial double PorcentajeOcupacion { get; set; }

        [ObservableProperty]
        public partial string PorcentajeOcupacionTexto { get; set; } = "0%";

        [ObservableProperty]
        public partial bool ModoResolucionPendientes { get; set; }

        [ObservableProperty]
        public partial bool EsTarjetaInformativaSeleccionada { get; set; }

        [ObservableProperty]
        public partial string BannerPendienteTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool MostrarBannerPendiente { get; set; }

        [ObservableProperty]
        public partial bool MostrarVisorImagen { get; set; } = false;

        [ObservableProperty]
        public partial string TituloVisorImagen { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? ImagenVisorUrl { get; set; }

        [RelayCommand]
        private void VerImagenCompleta()
        {
            if (SorteoActual != null && SorteoActual.TieneImagen)
            {
                TituloVisorImagen = $"{SorteoActual.Descripcion} (Folio: {SorteoActual.NumeroDeSorteo})";
                ImagenVisorUrl = SorteoActual.RutaImagen;
                MostrarVisorImagen = true;
            }
        }

        [RelayCommand]
        private void CerrarVisorImagen()
        {
            MostrarVisorImagen = false;
        }

        [RelayCommand]
        private void OcultarBannerPendiente()
        {
            Preferences.Default.Set("OcultarBannerSorteoPendiente", true);
            MostrarBannerPendiente = false;
        }

        [ObservableProperty]
        public partial string TituloInformativoReglas { get; set; } = "Reglas de Semana y Sorteos Pospuestos";

        [ObservableProperty]
        public partial string DescripcionInformativaReglas { get; set; } =
            "La aplicación opera por periodos semanales. Si tienes sorteos de semanas o periodos anteriores que aún no culminan, éstos permanecen 100% funcionales (puedes seguir apartando boletos, registrando pagos y enviando mensajes de WhatsApp).\n\nSin embargo, por regla de negocio, no es posible registrar nuevos sorteos para la semana en curso hasta que no culmines (cerrar registrando ganadores o cancelar) todos los sorteos pendientes de periodos anteriores.";

        public SorteosSemanaViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Sorteos de la Semana";

            WeakReferenceMessenger.Default.Register<SorteoCreadoMessage>(this, OnSorteoCreado);
        }

        public async Task InicializarSemanaAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                ResultadoTransicionSemanal transicion = await _databaseService.VerificarTransicionSemanalAsync();

                if (transicion == ResultadoTransicionSemanal.RequiereResolucionPendientes)
                {
                    ModoResolucionPendientes = true;
                    List<DiaSemanaItem> diasPendientes = await _databaseService.ObtenerDiasConSorteosPendientesAsync();

                    DiasSemana.Clear();
                    foreach (DiaSemanaItem d in diasPendientes)
                    {
                        DiasSemana.Add(d);
                    }

                    // Agregar tarjeta informativa al final
                    DiaSemanaItem tarjetaInfo = new()
                    {
                        IdDia = 99,
                        NombreDia = "ATENCIÓN",
                        InicialDia = "!",
                        Fecha = DateTime.Today,
                        TieneSorteo = false,
                        EsTarjetaInformativa = true
                    };
                    DiasSemana.Add(tarjetaInfo);

                    DiaSemanaItem? diaParaSeleccionar = DiasSemana.Count > 0 ? DiasSemana[0] : null;
                    if (diaParaSeleccionar != null)
                    {
                        await SeleccionarDiaInternoAsync(diaParaSeleccionar);
                        SolicitudDesplazarADia?.Invoke(diaParaSeleccionar);
                    }
                }
                else
                {
                    ModoResolucionPendientes = false;
                    List<DiaSemanaItem> dias = await _databaseService.ObtenerDiasSemanaAsync();

                    DiasSemana.Clear();
                    DiaSemanaItem? diaParaSeleccionar = null;

                    foreach (DiaSemanaItem dia in dias)
                    {
                        DiasSemana.Add(dia);
                        if (dia.EsHoy && diaParaSeleccionar == null)
                        {
                            diaParaSeleccionar = dia;
                        }
                    }

                    if (diaParaSeleccionar == null && DiasSemana.Count > 0)
                    {
                        diaParaSeleccionar = DiasSemana[0];
                    }

                    if (diaParaSeleccionar != null)
                    {
                        await SeleccionarDiaInternoAsync(diaParaSeleccionar);
                        SolicitudDesplazarADia?.Invoke(diaParaSeleccionar);
                    }
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible cargar los días de la semana. Por favor, intenta de nuevo.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al consultar los días de la semana.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event Action<DiaSemanaItem>? SolicitudDesplazarADia;

        [RelayCommand]
        private async Task NavegarDiaAnteriorAsync()
        {
            if (DiaSeleccionado == null || DiasSemana.Count == 0) return;

            int indiceActual = DiasSemana.IndexOf(DiaSeleccionado);
            if (indiceActual > 0)
            {
                DiaSemanaItem diaAnterior = DiasSemana[indiceActual - 1];
                await SeleccionarDiaInternoAsync(diaAnterior);
                SolicitudDesplazarADia?.Invoke(diaAnterior);
            }
        }

        [RelayCommand]
        private async Task NavegarDiaSiguienteAsync()
        {
            if (DiaSeleccionado == null || DiasSemana.Count == 0) return;

            int indiceActual = DiasSemana.IndexOf(DiaSeleccionado);
            if (indiceActual >= 0 && indiceActual < DiasSemana.Count - 1)
            {
                DiaSemanaItem diaSiguiente = DiasSemana[indiceActual + 1];
                await SeleccionarDiaInternoAsync(diaSiguiente);
                SolicitudDesplazarADia?.Invoke(diaSiguiente);
            }
        }

        [RelayCommand]
        private async Task SeleccionarDiaAsync(DiaSemanaItem? dia)
        {
            if (dia == null || dia == DiaSeleccionado) return;
            await SeleccionarDiaInternoAsync(dia);
            SolicitudDesplazarADia?.Invoke(dia);
        }

        private async Task SeleccionarDiaInternoAsync(DiaSemanaItem dia)
        {
            foreach (DiaSemanaItem item in DiasSemana)
            {
                item.EsSeleccionado = (item.IdDia == dia.IdDia);
            }

            DiaSeleccionado = dia;
            await CargarDetalleDiaAsync(dia);
        }

        private async Task CargarDetalleDiaAsync(DiaSemanaItem? dia)
        {
            if (dia == null)
            {
                SorteoActual = null;
                PremiosDelSorteo.Clear();
                TieneSorteoDiaSeleccionado = false;
                PuedeCrearSorteoDiaSeleccionado = false;
                EsDiaPasadoDiaSeleccionado = false;
                EsTarjetaInformativaSeleccionada = false;
                BannerPendienteTexto = string.Empty;
                MostrarBannerPendiente = false;
                MensajeEstadoDia = string.Empty;
                return;
            }

            if (dia.EsTarjetaInformativa)
            {
                SorteoActual = null;
                EsSorteoFinalizado = false;
                MostrarModalCerrarSorteo = false;
                PremiosDelSorteo.Clear();
                ResetearMetricas();
                TieneSorteoDiaSeleccionado = false;
                PuedeCrearSorteoDiaSeleccionado = false;
                EsDiaPasadoDiaSeleccionado = false;
                EsTarjetaInformativaSeleccionada = true;
                BannerPendienteTexto = string.Empty;
                MostrarBannerPendiente = false;
                MensajeEstadoDia = string.Empty;
                return;
            }

            EsTarjetaInformativaSeleccionada = false;

            try
            {
                IsBusy = true;
                List<SorteoPlantilla> sorteos = await _databaseService.ObtenerSorteosPorDiaIdAsync(dia.IdDia);

                if (sorteos.Count > 0)
                {
                    SorteoActual = sorteos[0];
                    EsSorteoFinalizado = SorteoActual.EsSorteoFinalizado;
                    TieneSorteoDiaSeleccionado = true;
                    PuedeCrearSorteoDiaSeleccionado = false;
                    EsDiaPasadoDiaSeleccionado = false;
                    dia.TieneSorteo = true;
                    MostrarModalCerrarSorteo = false;

                    if (ModoResolucionPendientes)
                    {
                        bool ocultar = Preferences.Default.Get("OcultarBannerSorteoPendiente", false);
                        MostrarBannerPendiente = !ocultar;
                        BannerPendienteTexto = "Sorteo en curso de periodo anterior. Sus funciones continúan totalmente activas (reservas, pagos y WhatsApp). Para habilitar nuevos sorteos esta semana, concluye o cancela los pendientes.";
                    }
                    else
                    {
                        MostrarBannerPendiente = false;
                        BannerPendienteTexto = string.Empty;
                    }

                    List<PremioPorSorteo> premios = await _databaseService.ObtenerPremiosPorSorteoIdAsync(SorteoActual.Id);
                    PremiosDelSorteo.Clear();
                    foreach (PremioPorSorteo p in premios)
                    {
                        PremiosDelSorteo.Add(p);
                    }

                    await CargarMetricasSorteoAsync(SorteoActual.Id);
                }
                else
                {
                    SorteoActual = null;
                    EsSorteoFinalizado = false;
                    MostrarModalCerrarSorteo = false;
                    BannerPendienteTexto = string.Empty;
                    MostrarBannerPendiente = false;
                    PremiosDelSorteo.Clear();
                    ResetearMetricas();
                    TieneSorteoDiaSeleccionado = false;
                    dia.TieneSorteo = false;

                    if (dia.EsDiaPasado)
                    {
                        EsDiaPasadoDiaSeleccionado = true;
                        PuedeCrearSorteoDiaSeleccionado = false;
                        MensajeEstadoDia = "La fecha seleccionada ya transcurrió. Por regla de negocio no es posible crear sorteos en fechas pasadas.";
                    }
                    else
                    {
                        EsDiaPasadoDiaSeleccionado = false;
                        PuedeCrearSorteoDiaSeleccionado = true;
                        MensajeEstadoDia = "No hay sorteo programado para este día.";
                    }
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar la información del sorteo seleccionado.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al consultar los detalles del sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarMetricasSorteoAsync(int idSorteo)
        {
            try
            {
                MetricasSorteoDto metricas = await _databaseService.ObtenerMetricasSorteoAsync(idSorteo);

                TotalNumeros     = metricas.TotalNumeros;
                TotalDisponibles = metricas.TotalDisponibles;
                TotalApartados   = metricas.TotalApartados;
                TotalPagados     = metricas.TotalPagados;

                int ocupados = TotalApartados + TotalPagados;
                if (TotalNumeros > 0)
                {
                    PorcentajeOcupacion      = (double)ocupados / TotalNumeros;
                    PorcentajeOcupacionTexto = $"{(PorcentajeOcupacion * 100):0.0}%";
                }
                else
                {
                    PorcentajeOcupacion      = 0;
                    PorcentajeOcupacionTexto = "0%";
                }
            }
            catch (SQLiteException)
            {
                ResetearMetricas();
            }
            catch (Exception)
            {
                ResetearMetricas();
            }
        }

        private void ResetearMetricas()
        {
            TotalNumeros             = 0;
            TotalDisponibles         = 0;
            TotalApartados           = 0;
            TotalPagados             = 0;
            PorcentajeOcupacion      = 0;
            PorcentajeOcupacionTexto = "0%";
        }

        [RelayCommand]
        private async Task CrearSorteoAsync()
        {
            if (ModoResolucionPendientes)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Operación no permitida",
                    "No es posible crear sorteos de la semana actual mientras tengas sorteos de periodos anteriores pendientes de culminar.",
                    "Aceptar");
                return;
            }

            if (DiaSeleccionado == null) return;

            if (DiaSeleccionado.EsDiaPasado)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Operación no permitida",
                    "No es posible crear sorteos en fechas pasadas.",
                    "Aceptar");
                return;
            }

            if (DiaSeleccionado.TieneSorteo)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Límite alcanzado",
                    "Ya existe un sorteo programado para este día. Por regla de negocio se admite como máximo un sorteo por día.",
                    "Aceptar");
                return;
            }

            await Shell.Current.GoToAsync($"SorteoCrearPage?idDia={DiaSeleccionado.IdDia}");
        }

        [RelayCommand]
        private async Task VerReservasAsync()
        {
            if (SorteoActual == null) return;
            await Shell.Current.GoToAsync($"ReservasSorteoPage?idSorteo={SorteoActual.Id}");
        }

        [RelayCommand]
        private async Task CancelarSorteoAsync()
        {
            if (SorteoActual == null || DiaSeleccionado == null) return;

            bool confirmar = await Shell.Current.DisplayAlertAsync(
                "Cancelar Sorteo",
                $"¿Deseas cancelar y eliminar definitivamente el sorteo '{SorteoActual.Descripcion}'? Se eliminarán todas las reservas, números y configuraciones asociadas.",
                "Sí, cancelar sorteo",
                "No, conservar");

            if (!confirmar) return;

            try
            {
                IsBusy = true;
                await _databaseService.EliminarSorteoYPremiosAsync(SorteoActual.Id);

                if (ModoResolucionPendientes)
                {
                    List<DiaSemanaItem> restantes = await _databaseService.ObtenerDiasConSorteosPendientesAsync();
                    if (restantes.Count == 0)
                    {
                        await _databaseService.RegenerarDiasSemanaActualAsync();
                        ModoResolucionPendientes = false;
                        await Shell.Current.DisplayAlertAsync(
                            "Semana Habilitada",
                            "Has resuelto todos los sorteos pendientes. La semana actual ha quedado habilitada para registrar nuevos sorteos.",
                            "Aceptar");
                        IsBusy = false;
                        await InicializarSemanaAsync();
                        return;
                    }
                    else
                    {
                        DiaSemanaItem diaAEliminar = DiaSeleccionado;
                        DiasSemana.Remove(diaAEliminar);
                        DiaSemanaItem? siguiente = DiasSemana.Count > 0 ? DiasSemana[0] : null;
                        if (siguiente != null)
                        {
                            await SeleccionarDiaInternoAsync(siguiente);
                            SolicitudDesplazarADia?.Invoke(siguiente);
                        }
                        await Shell.Current.DisplayAlertAsync("Sorteo cancelado", "El sorteo ha sido cancelado y eliminado correctamente.", "Aceptar");
                        return;
                    }
                }

                SorteoActual = null;
                EsSorteoFinalizado = false;
                PremiosDelSorteo.Clear();
                TieneSorteoDiaSeleccionado = false;
                DiaSeleccionado.TieneSorteo = false;

                if (DiaSeleccionado.EsDiaPasado)
                {
                    EsDiaPasadoDiaSeleccionado = true;
                    PuedeCrearSorteoDiaSeleccionado = false;
                    MensajeEstadoDia = "La fecha seleccionada ya transcurrió. Por regla de negocio no es posible crear sorteos en fechas pasadas.";
                }
                else
                {
                    EsDiaPasadoDiaSeleccionado = false;
                    PuedeCrearSorteoDiaSeleccionado = true;
                    MensajeEstadoDia = "No hay sorteo programado para este día.";
                }

                await Shell.Current.DisplayAlertAsync("Sorteo cancelado", "El sorteo ha sido cancelado y eliminado correctamente.", "Aceptar");
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible eliminar el sorteo de la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al cancelar el sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AbrirModalCerrarSorteoAsync()
        {
            if (SorteoActual == null) return;

            try
            {
                IsBusy = true;
                List<PremioPorSorteo> premios = await _databaseService.ObtenerPremiosPorSorteoIdAsync(SorteoActual.Id);
                PremiosParaCierre.Clear();

                for (int i = 0; i < premios.Count; i++)
                {
                    PremioPorSorteo p = premios[i];
                    PremiosParaCierre.Add(new PremioAsignacionDto
                    {
                        IdPremio = p.IdPremio,
                        Lugar = p.Lugar,
                        DescripcionPremio = p.DescripcionPremio,
                        ValorFormateado = p.ValorFormateado,
                        NumeroIngresado = string.Empty,
                        EstaAsignado = false
                    });
                }

                TituloModalCierre = $"Cierre de Sorteo — Folio #{SorteoActual.NumeroDeSorteo}";
                ActualizarEstadoTodosAsignados();
                MostrarModalCerrarSorteo = true;
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar los premios del sorteo para su cierre.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al preparar el cierre del sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void CerrarModalCierre()
        {
            MostrarModalCerrarSorteo = false;
            PremiosParaCierre.Clear();
        }

        [RelayCommand]
        private async Task AsignarNumeroPremioAsync(PremioAsignacionDto? item)
        {
            if (item == null || SorteoActual == null) return;

            if (string.IsNullOrWhiteSpace(item.NumeroIngresado))
            {
                await Shell.Current.DisplayAlertAsync("Dato requerido", "Por favor ingresa el número premiado.", "Aceptar");
                return;
            }

            if (!int.TryParse(item.NumeroIngresado.Trim(), out int numero) || numero < 0 || numero >= SorteoActual.CantidadNumeros)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Número inválido",
                    $"El número premiado debe encontrarse dentro del rango permitido (0 a {SorteoActual.CantidadNumeros - 1}).",
                    "Aceptar");
                return;
            }

            for (int i = 0; i < PremiosParaCierre.Count; i++)
            {
                PremioAsignacionDto otro = PremiosParaCierre[i];
                if (otro != item && otro.EstaAsignado && otro.NumeroGanador == numero)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Número duplicado",
                        $"El número {numero:D2} ya fue asignado al {otro.LugarTexto}. Cada premio debe contar con un número premiado diferente.",
                        "Aceptar");
                    return;
                }
            }

            try
            {
                DetalleNumeroGanadorDto? detalle = await _databaseService.ObtenerDetalleNumeroGanadorAsync(SorteoActual.Id, numero);

                item.NumeroGanador = numero;
                item.EstaAsignado = true;

                if (detalle != null && detalle.ReservaId.HasValue && detalle.ReservaId.Value > 0)
                {
                    item.ReservaId = detalle.ReservaId;
                    item.NombreGanador = !string.IsNullOrWhiteSpace(detalle.NombreCliente) ? detalle.NombreCliente : "Cliente General";
                    item.TelefonoGanador = detalle.Telefono ?? string.Empty;
                    item.EsDesierto = false;
                    item.ResumenResultado = $"Boleto #{numero:D2} — {item.NombreGanador}";
                }
                else
                {
                    item.ReservaId = 0;
                    item.NombreGanador = "Desierto / No vendido";
                    item.TelefonoGanador = string.Empty;
                    item.EsDesierto = true;
                    item.ResumenResultado = $"Boleto #{numero:D2} — Sin asignar (Desierto)";
                }

                ActualizarEstadoTodosAsignados();
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible verificar el boleto en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al verificar el boleto.", "Aceptar");
            }
        }

        [RelayCommand]
        private void DeshacerAsignacionPremio(PremioAsignacionDto? item)
        {
            if (item == null) return;

            item.EstaAsignado = false;
            item.NumeroGanador = null;
            item.NumeroIngresado = string.Empty;
            item.NombreGanador = string.Empty;
            item.TelefonoGanador = string.Empty;
            item.ReservaId = null;
            item.EsDesierto = false;
            item.ResumenResultado = string.Empty;

            ActualizarEstadoTodosAsignados();
        }

        private void ActualizarEstadoTodosAsignados()
        {
            if (PremiosParaCierre.Count == 0)
            {
                TodosPremiosAsignados = false;
                return;
            }

            bool todosCompletos = true;
            for (int i = 0; i < PremiosParaCierre.Count; i++)
            {
                if (!PremiosParaCierre[i].EstaAsignado)
                {
                    todosCompletos = false;
                    break;
                }
            }

            TodosPremiosAsignados = todosCompletos;
        }

        [RelayCommand]
        private async Task ConfirmarCierreSorteoAsync()
        {
            if (SorteoActual == null || !TodosPremiosAsignados) return;

            bool confirmar = await Shell.Current.DisplayAlertAsync(
                "Confirmar Cierre",
                "¿Deseas finalizar el sorteo con los números premiados registrados? Una vez cerrado, el sorteo quedará concluido.",
                "Sí, finalizar",
                "Revisar");

            if (!confirmar) return;

            try
            {
                IsBusy = true;
                List<ReservaPremiada> listaPremiados = [];

                for (int i = 0; i < PremiosParaCierre.Count; i++)
                {
                    PremioAsignacionDto p = PremiosParaCierre[i];
                    listaPremiados.Add(new ReservaPremiada
                    {
                        IdSorteo = SorteoActual.Id,
                        IdPremio = p.IdPremio,
                        IdNumerosReservados = p.ReservaId ?? 0,
                        NumeroGanador = p.NumeroGanador ?? 0,
                        FechaRegistro = DateTime.Now
                    });
                }

                await _databaseService.CerrarSorteoConPremiosAsync(SorteoActual.Id, listaPremiados);

                SorteoActual.IdEstatusSorteo = 3;
                EsSorteoFinalizado = true;
                MostrarModalCerrarSorteo = false;
                PremiosParaCierre.Clear();

                List<PremioPorSorteo> premiosActualizados = await _databaseService.ObtenerPremiosPorSorteoIdAsync(SorteoActual.Id);
                PremiosDelSorteo.Clear();
                foreach (PremioPorSorteo p in premiosActualizados)
                {
                    PremiosDelSorteo.Add(p);
                }

                if (ModoResolucionPendientes)
                {
                    List<DiaSemanaItem> restantes = await _databaseService.ObtenerDiasConSorteosPendientesAsync();
                    if (restantes.Count == 0)
                    {
                        await _databaseService.RegenerarDiasSemanaActualAsync();
                        ModoResolucionPendientes = false;
                        await Shell.Current.DisplayAlertAsync(
                            "Semana Habilitada",
                            "Has finalizado todos los sorteos pendientes. La semana actual ha quedado habilitada para registrar nuevos sorteos.",
                            "Aceptar");
                        IsBusy = false;
                        await InicializarSemanaAsync();
                        return;
                    }
                    else
                    {
                        if (DiaSeleccionado != null)
                        {
                            DiasSemana.Remove(DiaSeleccionado);
                            DiaSemanaItem? siguiente = DiasSemana.Count > 0 ? DiasSemana[0] : null;
                            if (siguiente != null)
                            {
                                await SeleccionarDiaInternoAsync(siguiente);
                                SolicitudDesplazarADia?.Invoke(siguiente);
                            }
                        }

                        await Shell.Current.DisplayAlertAsync(
                            "Sorteo Finalizado",
                            "El sorteo ha sido cerrado y los números ganadores han quedado registrados exitosamente.",
                            "Aceptar");
                        return;
                    }
                }

                await Shell.Current.DisplayAlertAsync(
                    "Sorteo Finalizado",
                    "El sorteo ha sido cerrado y los números ganadores han quedado registrados exitosamente.",
                    "Aceptar");
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible registrar el cierre del sorteo en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al cerrar el sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AbrirModalGanadoresFlyerAsync()
        {
            if (SorteoActual == null || !EsSorteoFinalizado) return;

            try
            {
                IsBusy = true;
                List<PremioPorSorteo> premios = await _databaseService.ObtenerPremiosPorSorteoIdAsync(SorteoActual.Id);
                PremiosDelSorteo.Clear();
                foreach (PremioPorSorteo p in premios)
                {
                    PremiosDelSorteo.Add(p);
                }

                MostrarModalGanadoresFlyer = true;
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar los ganadores del sorteo.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al preparar los ganadores para compartir.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void CerrarModalGanadoresFlyer()
        {
            MostrarModalGanadoresFlyer = false;
        }

        [RelayCommand]
        private async Task DescargarGanadoresFlyerAsync()
        {
            if (SolicitarCapturaFlyerGanadoresAsync == null)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "La captura de la imagen no se encuentra disponible en este momento.", "Aceptar");
                return;
            }

            try
            {
                IsBusy = true;
                string? rutaPng = await SolicitarCapturaFlyerGanadoresAsync();
                if (string.IsNullOrWhiteSpace(rutaPng) || !File.Exists(rutaPng))
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible generar la imagen para descargar.", "Aceptar");
                    return;
                }

                string nombreSugerido = $"Sorteo_{SorteoActual?.NumeroDeSorteo}_Ganadores.png";

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
        private async Task CompartirGanadoresFlyerAsync()
        {
            if (SolicitarCapturaFlyerGanadoresAsync == null)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "La captura del flyer no se encuentra disponible en este momento.", "Aceptar");
                return;
            }

            try
            {
                IsBusy = true;
                string? rutaPng = await SolicitarCapturaFlyerGanadoresAsync();
                if (string.IsNullOrWhiteSpace(rutaPng) || !File.Exists(rutaPng))
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible generar la imagen del flyer.", "Aceptar");
                    return;
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Ganadores - {SorteoActual?.Descripcion}",
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

        private async void OnSorteoCreado(object recipient, SorteoCreadoMessage message)
        {
            SorteoPlantilla nuevoSorteo = message.Value;
            DiaSemanaItem? diaAfectado = null;

            foreach (DiaSemanaItem item in DiasSemana)
            {
                if (item.IdDia == nuevoSorteo.IdDiasSorteo)
                {
                    item.TieneSorteo = true;
                    diaAfectado = item;
                    break;
                }
            }

            if (diaAfectado != null && DiaSeleccionado?.IdDia == diaAfectado.IdDia)
            {
                await CargarDetalleDiaAsync(diaAfectado);
            }
        }
    }
}
