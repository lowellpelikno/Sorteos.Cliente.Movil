namespace Sorteos.Cliente.Movil.Services
{
    public interface IComprobanteStorageService
    {
        Task<string?> CapturarOSeleccionarComprobanteAsync(int idReserva);
        Task<string?> GuardarArchivoComprobanteAsync(FileResult archivo, int idReserva);
    }
}

