namespace Sorteos.Cliente.Movil.Services
{
    public interface IPermisosService
    {
        Task<PermissionStatus> VerificarPermisoCamaraAsync();
        Task<PermissionStatus> SolicitarPermisoCamaraAsync();
        Task<PermissionStatus> VerificarPermisoGaleriaAsync();
        Task<PermissionStatus> SolicitarPermisoGaleriaAsync();
        Task<PermissionStatus> VerificarPermisoAlmacenamientoAsync();
        Task<PermissionStatus> SolicitarPermisoAlmacenamientoAsync();
        void AbrirConfiguracionSistema();
    }
}

