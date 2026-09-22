namespace Sorteos.Cliente.Movil.Services
{
    public interface IAuthService
    {
        Task<bool> TienePinConfiguradoAsync();
        Task<bool> RequiereAutenticacionAsync();
        Task<bool> ValidarPinAsync(string pin);
        Task EstablecerPinAsync(string pin);
        Task DesactivarPinAsync();
        bool EstaAutenticado { get; set; }
        bool HaAceptadoTerminos();
        void AceptarTerminos();
    }
}

