using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using Sorteos.Cliente.Movil.Helpers;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using SQLite;
using System.Collections.ObjectModel;

namespace Sorteos.Cliente.Movil.ViewModels
{
    [QueryProperty(nameof(IdDiaQuery), "idDia")]
    public partial class SorteoCrearViewModel : BaseViewModel
    {
        private readonly ILocalDatabaseService _databaseService;
        private readonly ISorteoImagenStorageService _imagenStorageService;

        [ObservableProperty]
        public partial string IdDiaQuery { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int IdDia { get; set; }

        [ObservableProperty]
        public partial string NombreDiaSeleccionado { get; set; } = string.Empty;

        [ObservableProperty]
        public partial DateTime FechaDiaSeleccionado { get; set; } = DateTime.Today;

        [ObservableProperty]
        public partial string Descripcion { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NumeroDeSorteo { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? RutaImagen { get; set; }

        [ObservableProperty]
        public partial bool TieneImagen { get; set; }

        [ObservableProperty]
        public partial string SeJuegaCon { get; set; } = "Lotería Nacional";

        [ObservableProperty]
        public partial string CostoTexto { get; set; } = "100";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsCantidad100))]
        [NotifyPropertyChangedFor(nameof(EsCantidad200))]
        [NotifyPropertyChangedFor(nameof(EsCantidad500))]
        [NotifyPropertyChangedFor(nameof(EsCantidad1000))]
        public partial int CantidadNumeros { get; set; } = 100;

        [ObservableProperty]
        public partial double CantidadNumerosSlider { get; set; } = 100;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsCantidad100))]
        [NotifyPropertyChangedFor(nameof(EsCantidad200))]
        [NotifyPropertyChangedFor(nameof(EsCantidad500))]
        [NotifyPropertyChangedFor(nameof(EsCantidad1000))]
        public partial bool EsOtroCantidad { get; set; } = false;

        [ObservableProperty]
        public partial string CantidadPersonalizadaTexto { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsOportunidad1))]
        [NotifyPropertyChangedFor(nameof(EsOportunidad2))]
        [NotifyPropertyChangedFor(nameof(EsOportunidad3))]
        [NotifyPropertyChangedFor(nameof(EsOportunidad4))]
        public partial int Oportunidades { get; set; } = 1;

        [ObservableProperty]
        public partial double OportunidadesSlider { get; set; } = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsOportunidad1))]
        [NotifyPropertyChangedFor(nameof(EsOportunidad2))]
        [NotifyPropertyChangedFor(nameof(EsOportunidad3))]
        [NotifyPropertyChangedFor(nameof(EsOportunidad4))]
        public partial bool EsOtroOportunidad { get; set; } = false;

        [ObservableProperty]
        public partial string OportunidadPersonalizadaTexto { get; set; } = string.Empty;

        public bool EsCantidad100 => !EsOtroCantidad && CantidadNumeros == 100;
        public bool EsCantidad200 => !EsOtroCantidad && CantidadNumeros == 200;
        public bool EsCantidad500 => !EsOtroCantidad && CantidadNumeros == 500;
        public bool EsCantidad1000 => !EsOtroCantidad && CantidadNumeros == 1000;

        public bool EsOportunidad1 => !EsOtroOportunidad && Oportunidades == 1;
        public bool EsOportunidad2 => !EsOtroOportunidad && Oportunidades == 2;
        public bool EsOportunidad3 => !EsOtroOportunidad && Oportunidades == 3;
        public bool EsOportunidad4 => !EsOtroOportunidad && Oportunidades == 4;

        [ObservableProperty]
        public partial bool RepetirCadaSemana { get; set; } = false;

        public ObservableCollection<PremioSeleccionableItem> PremiosDisponibles { get; } = [];

        public SorteoCrearViewModel(
            ILocalDatabaseService databaseService,
            ISorteoImagenStorageService imagenStorageService)
        {
            _databaseService = databaseService;
            _imagenStorageService = imagenStorageService;
            Title = "Crear Sorteo";

            WeakReferenceMessenger.Default.Register<PremioGuardadoMessage>(this, OnPremioGuardado);
        }

        async partial void OnIdDiaQueryChanged(string value)
        {
            if (int.TryParse(value, out int diaId) && diaId > 0)
            {
                IdDia = diaId;
                await CargarDatosDiaYPremiosAsync(diaId);
            }
        }

        partial void OnCostoTextoChanged(string value)
        {
            string filtrado = value.FiltrarCaracteresMonto(12);
            if (filtrado != value)
            {
                CostoTexto = filtrado;
            }
        }

        private async Task CargarDatosDiaYPremiosAsync(int diaId)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                List<DiaSemanaItem> dias = await _databaseService.ObtenerDiasSemanaAsync();
                DiaSemanaItem? diaEncontrado = null;

                foreach (DiaSemanaItem d in dias)
                {
                    if (d.IdDia == diaId)
                    {
                        diaEncontrado = d;
                        break;
                    }
                }

                if (diaEncontrado != null)
                {
                    NombreDiaSeleccionado = diaEncontrado.FechaFormateada;
                    FechaDiaSeleccionado = diaEncontrado.Fecha.Date;

                    if (diaEncontrado.EsDiaPasado)
                    {
                        await Shell.Current.DisplayAlertAsync(
                            "Fecha Vencida",
                            "Por regla de negocio no es posible crear sorteos en fechas pasadas.",
                            "Aceptar");
                        await Shell.Current.GoToAsync("..");
                        return;
                    }

                    if (diaEncontrado.TieneSorteo)
                    {
                        await Shell.Current.DisplayAlertAsync(
                            "Día Ocupado",
                            "Este día ya cuenta con un sorteo asignado. Solo se permite un sorteo por día.",
                            "Aceptar");
                        await Shell.Current.GoToAsync("..");
                        return;
                    }
                }

                List<PremioLocal> premios = await _databaseService.ObtenerPremiosAsync();
                PremiosDisponibles.Clear();

                foreach (PremioLocal p in premios)
                {
                    PremiosDisponibles.Add(new PremioSeleccionableItem(p, false));
                }

                if (string.IsNullOrWhiteSpace(NumeroDeSorteo))
                {
                    string siguienteFolio = await _databaseService.ObtenerSiguienteFolioSorteoAsync();
                    NumeroDeSorteo = siguienteFolio;
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar la información del día o del catálogo de premios.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado al preparar el formulario de sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnCantidadNumerosSliderChanged(double value)
        {
            int entero = (int)Math.Round(value);
            if (entero < 1) entero = 1;
            if (entero > 1000) entero = 1000;

            if (CantidadNumeros != entero)
            {
                CantidadNumeros = entero;
                if (EsOtroCantidad)
                {
                    CantidadPersonalizadaTexto = entero.ToString();
                }
            }
        }

        partial void OnCantidadPersonalizadaTextoChanged(string value)
        {
            if (!EsOtroCantidad) return;

            string filtrado = value.FiltrarCaracteresMonto(4);
            if (filtrado != value)
            {
                CantidadPersonalizadaTexto = filtrado;
                return;
            }

            if (int.TryParse(filtrado, out int parsed))
            {
                if (parsed > 1000) parsed = 1000;
                if (parsed < 1) parsed = 1;
                CantidadNumeros = parsed;
                if (Math.Abs(CantidadNumerosSlider - parsed) >= 1)
                {
                    CantidadNumerosSlider = parsed;
                }
            }
        }

        [RelayCommand]
        private void SeleccionarCantidadNumeros(object? parametro)
        {
            int valor = 0;
            if (parametro is int cantidad)
            {
                valor = cantidad;
            }
            else if (parametro != null && int.TryParse(parametro.ToString(), out int parsed))
            {
                valor = parsed;
            }

            if (valor > 0 && valor <= 1000)
            {
                EsOtroCantidad = false;
                CantidadPersonalizadaTexto = string.Empty;
                CantidadNumeros = valor;
                CantidadNumerosSlider = valor;
            }
        }

        [RelayCommand]
        private void SeleccionarOtroCantidad()
        {
            EsOtroCantidad = true;
            CantidadPersonalizadaTexto = CantidadNumeros.ToString();
        }

        partial void OnOportunidadesSliderChanged(double value)
        {
            int entero = (int)Math.Round(value);
            if (entero < 1) entero = 1;
            if (entero > 10) entero = 10;

            if (Oportunidades != entero)
            {
                Oportunidades = entero;
                if (EsOtroOportunidad)
                {
                    OportunidadPersonalizadaTexto = entero.ToString();
                }
            }
        }

        partial void OnOportunidadPersonalizadaTextoChanged(string value)
        {
            if (!EsOtroOportunidad) return;

            string filtrado = value.FiltrarCaracteresMonto(2);
            if (filtrado != value)
            {
                OportunidadPersonalizadaTexto = filtrado;
                return;
            }

            if (int.TryParse(filtrado, out int parsed))
            {
                if (parsed > 10) parsed = 10;
                if (parsed < 1) parsed = 1;
                Oportunidades = parsed;
                if (Math.Abs(OportunidadesSlider - parsed) >= 1)
                {
                    OportunidadesSlider = parsed;
                }
            }
        }

        [RelayCommand]
        private void SeleccionarOportunidad(object? parametro)
        {
            int valor = 0;
            if (parametro is int ops)
            {
                valor = ops;
            }
            else if (parametro != null && int.TryParse(parametro.ToString(), out int parsed))
            {
                valor = parsed;
            }

            if (valor >= 1 && valor <= 10)
            {
                EsOtroOportunidad = false;
                OportunidadPersonalizadaTexto = string.Empty;
                Oportunidades = valor;
                OportunidadesSlider = valor;
            }
        }

        [RelayCommand]
        private void SeleccionarOtroOportunidad()
        {
            EsOtroOportunidad = true;
            OportunidadPersonalizadaTexto = Oportunidades.ToString();
        }

        [RelayCommand]
        private async Task GuardarSorteoAsync()
        {
            if (IsBusy) return;

            // 1. Validacion de fecha contra dias pasados
            if (FechaDiaSeleccionado.Date < DateTime.Today)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Operación no permitida",
                    "No es posible crear sorteos en fechas pasadas.",
                    "Aceptar");
                return;
            }

            // 1.1 Validacion contra sorteos pendientes de periodos anteriores
            List<DiaSemanaItem> pendientesAnteriores = await _databaseService.ObtenerDiasConSorteosPendientesAsync();
            if (pendientesAnteriores.Count > 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Operación no permitida",
                    "No es posible crear sorteos de la semana actual mientras tengas sorteos de periodos anteriores pendientes de culminar.",
                    "Aceptar");
                return;
            }

            // 2. Validacion de campos requeridos
            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                await Shell.Current.DisplayAlertAsync("Dato requerido", "Por favor, ingresa el nombre o descripción del sorteo.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(NumeroDeSorteo))
            {
                await Shell.Current.DisplayAlertAsync("Dato requerido", "Por favor, ingresa el número o folio oficial del sorteo.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(SeJuegaCon))
            {
                await Shell.Current.DisplayAlertAsync("Dato requerido", "Por favor, indica con qué modalidad o sorteo oficial se juega.", "Aceptar");
                return;
            }

            // 3. Validacion de costo monetario
            if (!CostoTexto.EsMontoMonetarioValido(out decimal costoDecimal) || costoDecimal <= 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Costo inválido",
                    "Ingresa un costo de boleto válido y mayor a cero.",
                    "Aceptar");
                return;
            }

            // 4. Regla de negocio: Maximo 1,000 numeros
            if (CantidadNumeros <= 0 || CantidadNumeros > 1000)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Límite de emisión",
                    "La cantidad de números debe ser mayor a cero y no puede exceder el límite máximo de 1,000 números.",
                    "Aceptar");
                return;
            }

            // 5. Validacion de oportunidades
            if (Oportunidades < 1 || Oportunidades > 10)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Oportunidades no válidas",
                    "Las oportunidades por boleto deben ubicarse en un rango de 1 a 10.",
                    "Aceptar");
                return;
            }

            // 6. Validacion de premios seleccionados y unicidad de lugar
            List<int> idsPremiosSeleccionados = [];
            HashSet<int> lugaresSeleccionados = [];
            foreach (PremioSeleccionableItem item in PremiosDisponibles)
            {
                if (item.EstaSeleccionado)
                {
                    if (!lugaresSeleccionados.Add(item.Premio.Lugar))
                    {
                        await Shell.Current.DisplayAlertAsync(
                            "Lugares duplicados",
                            $"No es posible asignar más de un premio para el {item.Premio.LugarTexto}. Cada lugar premiado debe ser único dentro del sorteo.",
                            "Aceptar");
                        return;
                    }

                    idsPremiosSeleccionados.Add(item.Premio.IdPremio);
                }
            }

            if (idsPremiosSeleccionados.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Premios requeridos",
                    "Debes seleccionar al menos un premio del catálogo para este sorteo.",
                    "Aceptar");
                return;
            }

            // 7. Validacion de unicidad de sorteo por dia
            List<SorteoPlantilla> sorteosExistentes = await _databaseService.ObtenerSorteosPorDiaIdAsync(IdDia);
            if (sorteosExistentes.Count > 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Límite alcanzado",
                    "Ya existe un sorteo registrado para este día. Por regla de negocio solo se permite un sorteo por día.",
                    "Aceptar");
                return;
            }

            try
            {
                IsBusy = true;

                SorteoPlantilla nuevoSorteo = new()
                {
                    IdDiasSorteo = IdDia,
                    Descripcion = Descripcion.Clean(),
                    NumeroDeSorteo = NumeroDeSorteo.Clean(),
                    SeJuegaCon = SeJuegaCon.Clean(),
                    Costo = costoDecimal,
                    CantidadNumeros = CantidadNumeros,
                    Oportunidades = Oportunidades,
                    FechaInicio = FechaDiaSeleccionado.Date,
                    FechaFin = FechaDiaSeleccionado.Date,
                    RepetirCadaSemana = RepetirCadaSemana,
                    IdEstatusSorteo = 1,
                    Activo = true,
                    RutaImagen = RutaImagen
                };

                int nuevoId = await _databaseService.GuardarSorteoAsync(nuevoSorteo, idsPremiosSeleccionados);
                nuevoSorteo.Id = nuevoId;

                WeakReferenceMessenger.Default.Send(new SorteoCreadoMessage(nuevoSorteo));

                await Shell.Current.DisplayAlertAsync(
                    "Sorteo Creado",
                    $"El sorteo '{nuevoSorteo.Descripcion}' ha sido creado exitosamente con {nuevoSorteo.CantidadNumeros} números.",
                    "Aceptar");

                await Shell.Current.GoToAsync("..");
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un problema al guardar el sorteo en la base de datos local.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un error inesperado durante la creación del sorteo.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelarAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task SeleccionarImagenAsync()
        {
            string? ruta = await _imagenStorageService.CapturarOSeleccionarImagenSorteoAsync();
            if (!string.IsNullOrWhiteSpace(ruta))
            {
                RutaImagen = ruta;
                TieneImagen = true;
            }
        }

        [ObservableProperty]
        public partial bool MostrarVisorImagen { get; set; } = false;

        [ObservableProperty]
        public partial string TituloVisorImagen { get; set; } = "Vista previa de la imagen";

        [RelayCommand]
        private void VerImagenCompleta()
        {
            if (TieneImagen && !string.IsNullOrWhiteSpace(RutaImagen))
            {
                TituloVisorImagen = !string.IsNullOrWhiteSpace(Descripcion)
                    ? $"{Descripcion} (Folio: {NumeroDeSorteo})"
                    : "Imagen del Sorteo";
                MostrarVisorImagen = true;
            }
        }

        [RelayCommand]
        private void CerrarVisorImagen()
        {
            MostrarVisorImagen = false;
        }

        [RelayCommand]
        private void QuitarImagen()
        {
            if (!string.IsNullOrWhiteSpace(RutaImagen))
            {
                _imagenStorageService.EliminarImagenSorteo(RutaImagen);
            }
            RutaImagen = null;
            TieneImagen = false;
            MostrarVisorImagen = false;
        }

        [RelayCommand]
        private async Task NuevoPremioAsync()
        {
            await Shell.Current.GoToAsync("PremioPage");
        }

        private void OnPremioGuardado(object recipient, PremioGuardadoMessage message)
        {
            PremioLocal premio = message.Value;
            if (message.EsNuevo)
            {
                PremiosDisponibles.Add(new PremioSeleccionableItem(premio, false));
            }
            else
            {
                foreach (PremioSeleccionableItem item in PremiosDisponibles)
                {
                    if (item.Premio.IdPremio == premio.IdPremio)
                    {
                        item.Premio = premio;
                        break;
                    }
                }
            }
        }
    }
}

