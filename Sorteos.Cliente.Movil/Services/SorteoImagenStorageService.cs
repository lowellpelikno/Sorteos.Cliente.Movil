namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Implementacion concreta para la captura y administracion de artes graficos y portadas de sorteos.
    /// </summary>
    public class SorteoImagenStorageService : ISorteoImagenStorageService
    {
        private const string CarpetaImagenesSorteos = "imagenes_sorteos";

        /// <inheritdoc/>
        public async Task<string?> CapturarOSeleccionarImagenSorteoAsync()
        {
            try
            {
                bool soporteCamara = MediaPicker.Default.IsCaptureSupported;
                string? seleccion;

                if (soporteCamara)
                {
                    seleccion = await Shell.Current.DisplayActionSheet(
                        "Imagen del sorteo",
                        "Cancelar",
                        null,
                        "Tomar foto",
                        "Elegir de la galería");
                }
                else
                {
                    seleccion = await Shell.Current.DisplayActionSheet(
                        "Imagen del sorteo",
                        "Cancelar",
                        null,
                        "Elegir de la galería");
                }

                if (string.IsNullOrWhiteSpace(seleccion) || seleccion == "Cancelar")
                {
                    return null;
                }

                FileResult? resultadoFoto = null;

                if (seleccion == "Tomar foto")
                {
                    resultadoFoto = await MediaPicker.Default.CapturePhotoAsync();
                }
                else if (seleccion == "Elegir de la galería")
                {
                    resultadoFoto = await MediaPicker.Default.PickPhotoAsync();
                }

                if (resultadoFoto == null)
                {
                    return null;
                }

                return await GuardarArchivoImagenSorteoAsync(resultadoFoto);
            }
            catch (PermissionException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permiso denegado",
                    "Se requiere permiso para acceder a la cámara o galería para adjuntar la imagen del sorteo.",
                    "Aceptar");
                return null;
            }
            catch (FeatureNotSupportedException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Función no disponible",
                    "La captura o selección de imágenes no se encuentra soportada en este dispositivo.",
                    "Aceptar");
                return null;
            }
            catch (IOException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error de almacenamiento",
                    "No fue posible guardar la imagen del sorteo en el dispositivo.",
                    "Aceptar");
                return null;
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Aviso",
                    "Ocurrió un problema inesperado al procesar la imagen del sorteo.",
                    "Aceptar");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<string?> GuardarArchivoImagenSorteoAsync(FileResult archivo)
        {
            try
            {
                string rutaCarpeta = Path.Combine(FileSystem.AppDataDirectory, CarpetaImagenesSorteos);
                if (!Directory.Exists(rutaCarpeta))
                {
                    Directory.CreateDirectory(rutaCarpeta);
                }

                string extension = Path.GetExtension(archivo.FileName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".jpg";
                }

                string nombreArchivo = $"sorteo_{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                string rutaDestino = Path.Combine(rutaCarpeta, nombreArchivo);

                using Stream streamOrigen = await archivo.OpenReadAsync();
                using FileStream streamDestino = File.Create(rutaDestino);
                await streamOrigen.CopyToAsync(streamDestino);

                return rutaDestino;
            }
            catch (IOException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error de archivo",
                    "No se pudo completar el guardado de la imagen del sorteo en el almacenamiento local.",
                    "Aceptar");
                return null;
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Aviso",
                    "Ocurrió un problema al transferir la imagen del sorteo.",
                    "Aceptar");
                return null;
            }
        }

        /// <inheritdoc/>
        public void EliminarImagenSorteo(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return;

            try
            {
                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch (Exception)
            {
                // Limpieza silenciosa sin interrumpir flujo
            }
        }
    }
}

