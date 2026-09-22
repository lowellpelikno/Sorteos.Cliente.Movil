namespace Sorteos.Cliente.Movil.Services
{
    public interface ISorteoImagenStorageService
    {
        Task<string?> CapturarOSeleccionarImagenSorteoAsync();
        Task<string?> GuardarArchivoImagenSorteoAsync(FileResult archivo);
        void EliminarImagenSorteo(string? ruta);
    }
}

