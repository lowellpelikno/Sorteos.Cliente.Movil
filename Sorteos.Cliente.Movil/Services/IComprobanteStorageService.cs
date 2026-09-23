namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato para el almacenamiento y captura de comprobantes de pago fotográficos de boletos apartados.
    /// </summary>
    public interface IComprobanteStorageService
    {
        /// <summary>
        /// Presenta las opciones para capturar una foto con la camara o elegir una imagen existente de la galeria.
        /// </summary>
        /// <param name="idReserva">Identificador de la reserva de boleto a la que se vinculara el comprobante.</param>
        /// <returns>Ruta absoluta en disco del archivo persistido, o <c>null</c> si el usuario cancelo la operacion.</returns>
        Task<string?> CapturarOSeleccionarComprobanteAsync(int idReserva);

        /// <summary>
        /// Guarda de forma segura una copia local del archivo seleccionado en el directorio de datos de la aplicacion.
        /// </summary>
        /// <param name="archivo">Resultado obtenido del selector de archivos o camara.</param>
        /// <param name="idReserva">Identificador de la reserva asociada.</param>
        /// <returns>Ruta local final del archivo copiado.</returns>
        Task<string?> GuardarArchivoComprobanteAsync(FileResult archivo, int idReserva);
    }
}

