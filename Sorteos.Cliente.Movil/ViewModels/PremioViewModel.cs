using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;
using CommunityToolkit.Mvvm.Messaging;
using Sorteos.Cliente.Movil.Helpers;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    [QueryProperty(nameof(IdPremio), "idPremio")]
    public partial class PremioViewModel : BaseViewModel
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");
        private readonly ILocalDatabaseService _databaseService;

        [ObservableProperty]
        public partial int IdPremio { get; set; }

        [ObservableProperty]
        public partial int Lugar { get; set; } = 1;

        [ObservableProperty]
        public partial string DescripcionPremio { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool EsMonetario { get; set; } = true;

        [ObservableProperty]
        public partial string DescripcionLarga { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool Activo { get; set; } = true;

        public bool EsEspecie => !EsMonetario;

        public string EtiquetaDescripcion => EsMonetario 
            ? "Monto o Valor del Premio ($):" 
            : "Descripción del Premio (Artículo o Bien):";

        public string PlaceholderDescripcion => EsMonetario 
            ? "0.00" 
            : "Ej. Automóvil sedán, Motocicleta, Smart TV...";

        public Keyboard TecladoDescripcion => EsMonetario 
            ? Keyboard.Numeric 
            : Keyboard.Default;

        public PremioViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Nuevo Premio";
        }

        partial void OnDescripcionPremioChanged(string value)
        {
            if (!EsMonetario || string.IsNullOrEmpty(value)) return;

            string filtrado = value.FiltrarCaracteresMonto();
            if (filtrado != value)
            {
                DescripcionPremio = filtrado;
            }
        }

        partial void OnEsMonetarioChanged(bool value)
        {
            OnPropertyChanged(nameof(EsEspecie));
            OnPropertyChanged(nameof(EtiquetaDescripcion));
            OnPropertyChanged(nameof(PlaceholderDescripcion));
            OnPropertyChanged(nameof(TecladoDescripcion));
        }

        [RelayCommand]
        private void SeleccionarTipoPremio(string tipo)
        {
            bool nuevoEsMonetario = tipo == "Monetario";
            if (EsMonetario != nuevoEsMonetario)
            {
                EsMonetario = nuevoEsMonetario;
                DescripcionPremio = string.Empty;
            }
        }

        async partial void OnIdPremioChanged(int value)
        {
            if (value > 0)
            {
                Title = "Editar Premio";
                await CargarPremioAsync(value);
            }
            else
            {
                Title = "Nuevo Premio";
            }
        }

        private async Task CargarPremioAsync(int idPremio)
        {
            try
            {
                IsBusy = true;
                PremioLocal? premio = await _databaseService.ObtenerPremioPorIdAsync(idPremio);
                if (premio != null)
                {
                    Lugar = premio.Lugar;
                    DescripcionPremio = premio.DescripcionPremio;
                    EsMonetario = premio.EsMonetario;
                    DescripcionLarga = premio.DescripcionLarga;
                    Activo = premio.Activo;
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible cargar el premio debido a un problema con el almacenamiento local.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al obtener los datos del premio.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GuardarAsync()
        {
            if (Lugar <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Validación", "El lugar debe ser mayor a 0.", "Aceptar");
                return;
            }

            string descTrim = DescripcionPremio?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(descTrim))
            {
                string mensajeError = EsMonetario
                    ? "El monto o valor del premio es requerido."
                    : "La descripción o nombre del premio es requerida.";
                await Shell.Current.DisplayAlertAsync("Validación", mensajeError, "Aceptar");
                return;
            }

            string valorAGuardar = descTrim;

            if (EsMonetario)
            {
                if (!descTrim.EsMontoMonetarioValido(out decimal monto))
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Validación", 
                        "El monto debe ser un valor numérico válido mayor a 0 (ejemplo: 4000 o 4,000.00), sin letras ni caracteres especiales.", 
                        "Aceptar");
                    return;
                }

                valorAGuardar = monto.ToString("F2", CultureInfo.InvariantCulture);
            }

            try
            {
                IsBusy = true;
                PremioLocal premio = new()
                {
                    IdPremio = IdPremio,
                    Lugar = Lugar,
                    DescripcionPremio = valorAGuardar,
                    EsMonetario = EsMonetario,
                    DescripcionLarga = DescripcionLarga?.Trim() ?? string.Empty,
                    Activo = Activo
                };

                bool esNuevo = IdPremio == 0;
                int resultado = await _databaseService.GuardarPremioAsync(premio);
                if (resultado == -1)
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible guardar el premio. Verifica que el lugar asignado no se encuentre duplicado.", "Aceptar");
                    return;
                }

                premio.IdPremio = resultado;
                WeakReferenceMessenger.Default.Send(new PremioGuardadoMessage(premio, esNuevo));
                await Shell.Current.GoToAsync("..");
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible guardar los datos del premio en el almacenamiento local. Verifica que el lugar asignado no se encuentre duplicado.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al registrar el premio. Por favor, inténtalo nuevamente.", "Aceptar");
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
    }
}

