using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.ViewModels
{
    /// <summary>
    /// Clase base abstracta conceptual para todos los ViewModels de la aplicacion.
    /// Centraliza la gestion de estados de ocupacion y titulo de pantalla para garantizar
    /// una experiencia de usuario (UX) coherente, accesible y predecible.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Retroalimentacion Visual Inmediata: Permite reflejar estados de carga asincrona para informar al usuario que su solicitud esta en proceso.
    /// - Prevencion de Errores: Facilita la deshabilitacion de botones y elementos interactivos mediante <see cref="IsNotBusy"/>, evitando pulsaciones accidentales duplicadas.
    /// - Orientacion y Contexto: Asegura que cada pantalla disponga de un titulo descriptivo visible en la barra de navegacion superior.
    /// </remarks>
    public partial class BaseViewModel : ObservableObject
    {
        /// <summary>
        /// Obtiene o establece un valor que indica si la interfaz de usuario esta procesando una operacion en segundo plano.
        /// </summary>
        /// <remarks>
        /// Impacto en Usabilidad:
        /// Al activarse (<c>true</c>), se vincula a controles como <c>ActivityIndicator</c> para mostrar loaders visuales.
        /// Notifica automaticamente la actualizacion de <see cref="IsNotBusy"/> para alternar la interactividad de la vista.
        /// </remarks>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        public partial bool IsBusy { get; set; }

        /// <summary>
        /// Obtiene o establece el texto que encabeza la pantalla actual en la barra de navegacion o barra superior.
        /// </summary>
        /// <remarks>
        /// Impacto en Usabilidad:
        /// Proporciona orientacion inmediata al usuario sobre el modulo en el que se encuentra ubicado,
        /// adaptandose dinamicamente a flujos de creacion o edicion segun el contexto.
        /// </remarks>
        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;

        /// <summary>
        /// Propiedad calculada que representa el estado inverso de <see cref="IsBusy"/>.
        /// </summary>
        /// <remarks>
        /// Impacto en Usabilidad:
        /// Se utiliza como enlace de habilitacion (<c>IsEnabled</c>) en botones de accion (Guardar, Eliminar, Consultar),
        /// asegurando que el usuario no pueda enviar multiples solicitudes simultaneas durante operaciones de red o base de datos.
        /// </remarks>
        public bool IsNotBusy => !IsBusy;
    }
}

