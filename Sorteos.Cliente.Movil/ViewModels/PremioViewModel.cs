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
    /// <summary>
    /// ViewModel para la captura, edicion y configuracion de premios del catalogo.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Adaptacion Dinamica segun Naturaleza del Premio: Al alternar entre premio monetario y en especie,
    ///   reconfigura automaticamente la etiqueta descriptiva, el texto de sugerencia (placeholder) y el tipo
    ///   de teclado virtual del sistema (numerico vs alfanumerico), minimizando friccion en la captura.
    /// - Sanitizacion en Vivo: Si el premio es de tipo monetario, filtra inmediatamente caracteres incompatibles
    ///   con montos mientras el usuario escribe.
    /// - Mensajes de Validacion Orientativos: En lugar de errores genericos, provee ejemplos claros de montos aceptables
    ///   (ej. "4000 o 4,000.00") y avisa con claridad si el lugar ordinal ya se encuentra ocupado.
    /// - Sincronizacion Reactiva: Emite <see cref="PremioGuardadoMessage"/> para que la lista receptora ordene
    ///   e inserte el registro sin parpadeos visuales ni recargas completas.
    /// </remarks>
    [QueryProperty(nameof(IdPremio), "idPremio")]
    public partial class PremioViewModel : BaseViewModel
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");
        private readonly ILocalDatabaseService _databaseService;

        /// <summary>
        /// Obtiene o establece el identificador del premio a editar (0 para alta de nuevo registro).
        /// </summary>
        [ObservableProperty]
        public partial int IdPremio { get; set; }

        /// <summary>
        /// Obtiene o establece la posicion o lugar ordinal que ocupa el premio (1 para Primer Lugar, etc.).
        /// </summary>
        [ObservableProperty]
        public partial int Lugar { get; set; } = 1;

        /// <summary>
        /// Obtiene o establece el valor o descripcion principal del premio (monto numerico o nombre del bien).
        /// </summary>
        [ObservableProperty]
        public partial string DescripcionPremio { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si el premio corresponde a dinero en efectivo o transferencia bancaria.
        /// </summary>
        [ObservableProperty]
        public partial bool EsMonetario { get; set; } = true;

        /// <summary>
        /// Obtiene o establece el detalle extendido o especificaciones tecnicas del premio (opcional).
        /// </summary>
        [ObservableProperty]
        public partial string DescripcionLarga { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si el premio esta disponible para asignacion en sorteos.
        /// </summary>
        [ObservableProperty]
        public partial bool Activo { get; set; } = true;

        /// <summary>
        /// Indica si el premio es en especie (articulo fisico), calculado inversamente a <see cref="EsMonetario"/>.
        /// </summary>
        public bool EsEspecie => !EsMonetario;

        /// <summary>
        /// Texto de encabezado del campo principal adaptado reactivamente segun la modalidad seleccionada.
        /// </summary>
        public string EtiquetaDescripcion => EsMonetario 
            ? "Monto o Valor del Premio ($):" 
            : "Descripción del Premio (Artículo o Bien):";

        /// <summary>
        /// Texto indicativo de ayuda (placeholder) mostrado cuando el campo de entrada esta vacio.
        /// </summary>
        public string PlaceholderDescripcion => EsMonetario 
            ? "0.00" 
            : "Ej. Automóvil sedán, Motocicleta, Smart TV...";

        /// <summary>
        /// Configura el teclado virtual del dispositivo (numerico para dinero, alfabetico para bienes).
        /// </summary>
        public Keyboard TecladoDescripcion => EsMonetario 
            ? Keyboard.Numeric 
            : Keyboard.Default;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioViewModel"/> configurando el titulo por defecto.
        /// </summary>
        /// <param name="databaseService">Servicio de datos local.</param>
        public PremioViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Nuevo Premio";
        }

        /// <summary>
        /// Sanitiza la entrada de monto monetario en vivo, eliminando caracteres no validos.
        /// </summary>
        partial void OnDescripcionPremioChanged(string value)
        {
            if (!EsMonetario || string.IsNullOrEmpty(value)) return;

            string filtrado = value.FiltrarCaracteresMonto();
            if (filtrado != value)
            {
                DescripcionPremio = filtrado;
            }
        }

        /// <summary>
        /// Notifica la mutacion de etiquetas, placeholders y tipo de teclado cuando cambia la naturaleza del premio.
        /// </summary>
        partial void OnEsMonetarioChanged(bool value)
        {
            OnPropertyChanged(nameof(EsEspecie));
            OnPropertyChanged(nameof(EtiquetaDescripcion));
            OnPropertyChanged(nameof(PlaceholderDescripcion));
            OnPropertyChanged(nameof(TecladoDescripcion));
        }

        /// <summary>
        /// Conmuta la clasificacion del premio (Monetario vs Especie) desde los botones de seleccion segmentada.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Limpia la descripcion previa para evitar inconsistencias de formato entre tipos.
        /// </remarks>
        /// <param name="tipo">Cadena identificadora ("Monetario" o "Especie").</param>
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

        /// <summary>
        /// Carga los datos del premio a editar segun el parametro de navegacion recibido.
        /// </summary>
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

        /// <summary>
        /// Recupera los detalles del premio seleccionado para poblar el formulario de edicion.
        /// </summary>
        /// <param name="idPremio">Identificador del premio en base de datos.</param>
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

        /// <summary>
        /// Valida el lugar ordinal y formato de descripcion/monto antes de persistir en SQLite.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Despliega mensajes claros si el monto monetario no es numerico, formatea a dos decimales
        /// y emite el mensaje global <see cref="PremioGuardadoMessage"/> para refrescar las listas vinculadas.
        /// </remarks>
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

        /// <summary>
        /// Comando para cancelar la operacion y retornar a la vista anterior sin persistir cambios.
        /// </summary>
        [RelayCommand]
        private async Task CancelarAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

