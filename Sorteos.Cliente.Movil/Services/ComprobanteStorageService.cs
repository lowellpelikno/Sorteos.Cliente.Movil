namespace Sorteos.Cliente.Movil.Services
{
    public class ComprobanteStorageService : IComprobanteStorageService
    {
        private const string CarpetaComprobantes = "comprobantes";

        public async Task<string?> CapturarOSeleccionarComprobanteAsync(int idReserva)
        {
            try
            {
                bool soporteCamara = MediaPicker.Default.IsCaptureSupported;
                string? seleccion;

                if (soporteCamara)
                {
                    seleccion = await Shell.Current.DisplayActionSheet(
                        "Comprobante de pago",
                        "Cancelar",
                        null,
                        "Tomar foto",
                        "Elegir de la galeria");
                }
                else
                {
                    seleccion = await Shell.Current.DisplayActionSheet(
                        "Comprobante de pago",
                        "Cancelar",
                        null,
                        "Elegir de la galeria");
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
                else if (seleccion == "Elegir de la galeria")
                {
                    resultadoFoto = await MediaPicker.Default.PickPhotoAsync();
                }

                if (resultadoFoto == null)
                {
                    return null;
                }

                return await GuardarArchivoComprobanteAsync(resultadoFoto, idReserva);
            }
            catch (PermissionException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permiso denegado",
                    "Se requiere permiso para acceder a la camara o galeria para adjuntar el comprobante.",
                    "Aceptar");
                return null;
            }
            catch (FeatureNotSupportedException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Funcion no disponible",
                    "La captura o seleccion de imagenes no se encuentra soportada en este dispositivo.",
                    "Aceptar");
                return null;
            }
            catch (IOException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error de almacenamiento",
                    "No fue posible guardar la imagen del comprobante en el dispositivo.",
                    "Aceptar");
                return null;
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Aviso",
                    "Ocurrio un problema inesperado al procesar el comprobante.",
                    "Aceptar");
                return null;
            }
        }

        public async Task<string?> GuardarArchivoComprobanteAsync(FileResult archivo, int idReserva)
        {
            try
            {
                string rutaCarpeta = Path.Combine(FileSystem.AppDataDirectory, CarpetaComprobantes);
                if (!Directory.Exists(rutaCarpeta))
                {
                    Directory.CreateDirectory(rutaCarpeta);
                }

                string extension = Path.GetExtension(archivo.FileName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".jpg";
                }

                string nombreArchivo = $"comprobante_{idReserva}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
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
                    "No se pudo completar el guardado del comprobante en el almacenamiento local.",
                    "Aceptar");
                return null;
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Aviso",
                    "Ocurrio un problema al transferir la imagen del comprobante.",
                    "Aceptar");
                return null;
            }
        }
    }
}

