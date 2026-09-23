namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Contrato para la captura, persistencia y eliminacion de imagenes promocionales asociadas a los sorteos.
    /// </summary>
    public interface ISorteoImagenStorageService
    {
        /// <summary>
        /// Muestra el dialogo de seleccion para capturar con la camara o elegir una imagen de portada desde la galeria.
        /// </summary>
        /// <returns>Ruta absoluta en disco de la imagen copiada, o <c>null</c> si se cancela la operacion.</returns>
        Task<string?> CapturarOSeleccionarImagenSorteoAsync();

        /// <summary>
        /// Guarda una copia del archivo seleccionado en el directorio de imagenes de sorteos de la aplicacion.
        /// </summary>
        /// <param name="archivo">Archivo recuperado de la camara o selector del sistema.</param>
        /// <returns>Ruta local final del archivo almacenado.</returns>
        Task<string?> GuardarArchivoImagenSorteoAsync(FileResult archivo);

        /// <summary>
        /// Elimina físicamente del almacenamiento del dispositivo el archivo de imagen especificado para evitar archivos huerfanos.
        /// </summary>
        /// <param name="ruta">Ruta en disco del archivo a suprimir.</param>
        void EliminarImagenSorteo(string? ruta);
    }
}

