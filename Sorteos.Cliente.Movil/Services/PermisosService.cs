namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Implementacion concreta para la consulta y gestion de permisos nativos del dispositivo mediante Microsoft.Maui.ApplicationModel.
    /// </summary>
    public class PermisosService : IPermisosService
    {
        /// <inheritdoc/>
        public async Task<PermissionStatus> VerificarPermisoCamaraAsync()
        {
            try
            {
                return await Permissions.CheckStatusAsync<Permissions.Camera>();
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<PermissionStatus> SolicitarPermisoCamaraAsync()
        {
            try
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                }
                return status;
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<PermissionStatus> VerificarPermisoGaleriaAsync()
        {
            try
            {
                return await Permissions.CheckStatusAsync<Permissions.Photos>();
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<PermissionStatus> SolicitarPermisoGaleriaAsync()
        {
            try
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Photos>();
                }
                return status;
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<PermissionStatus> VerificarPermisoAlmacenamientoAsync()
        {
            try
            {
                return await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<PermissionStatus> SolicitarPermisoAlmacenamientoAsync()
        {
            try
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }
                return status;
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public void AbrirConfiguracionSistema()
        {
            try
            {
                AppInfo.Current.ShowSettingsUI();
            }
            catch (Exception)
            {
                // Tolerancia en plataformas donde no esté soportado
            }
        }
    }
}

