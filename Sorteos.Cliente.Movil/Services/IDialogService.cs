namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato de abstraccion para el despliegue desacoplado de dialogos emergentes y confirmaciones en la UI.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Muestra una alerta informativa con un unico boton de cierre.
        /// </summary>
        /// <param name="title">Titulo sobrio del dialogo.</param>
        /// <param name="message">Mensaje explicativo y amigable para el usuario.</param>
        /// <param name="cancel">Texto del boton de aceptacion o cierre.</param>
        Task DisplayAlertAsync(string title, string message, string cancel);

        /// <summary>
        /// Muestra una ventana de confirmacion con dos opciones de decision (Aceptar o Cancelar).
        /// </summary>
        /// <param name="title">Titulo de la confirmacion.</param>
        /// <param name="message">Pregunta o explicacion clara sobre las consecuencias de la accion.</param>
        /// <param name="accept">Texto del boton de confirmacion afirmativa.</param>
        /// <param name="cancel">Texto del boton de cancelacion o descarte.</param>
        /// <returns><c>true</c> si el usuario pulso la opcion afirmativa; de lo contrario, <c>false</c>.</returns>
        Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
    }
}

