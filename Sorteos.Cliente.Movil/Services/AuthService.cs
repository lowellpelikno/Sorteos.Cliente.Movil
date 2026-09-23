namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Implementacion del servicio de autenticacion y resguardo de credenciales.
    /// </summary>
    /// <remarks>
    /// Arquitectura y Seguridad:
    /// - Utiliza <see cref="SecureStorage"/> para el resguardo cifrado del PIN en el almacenamiento del sistema operativo.
    /// - Proporciona un mecanismo de contingencia en memoria (<see cref="_fallbackMemoria"/>) para garantizar operacion
    ///   continua en entornos de pruebas unitarias o plataformas que carezcan de soporte de llavero nativo.
    /// - Gestiona de forma atomica las banderas de estado de PIN activo y aceptacion de terminos legales.
    /// </remarks>
    public class AuthService : IAuthService
    {
        private const string KeyAppPin = "App_Security_Pin";
        private const string KeyPinActivo = "App_Security_Pin_Activo";
        private const string KeyTerminosAceptados = "App_Terminos_Aceptados";

        private readonly Dictionary<string, string> _fallbackMemoria = [];

        /// <inheritdoc/>
        public bool EstaAutenticado { get; set; } = false;

        private string ObtenerValor(string clave, string valorDefecto)
        {
            try
            {
                return Preferences.Default.Get(clave, valorDefecto);
            }
            catch (Exception)
            {
                return _fallbackMemoria.GetValueOrDefault(clave, valorDefecto);
            }
        }

        private void EstablecerValor(string clave, string valor)
        {
            try
            {
                Preferences.Default.Set(clave, valor);
            }
            catch (Exception)
            {
                // Fallback
            }

            _fallbackMemoria[clave] = valor;
        }

        private void RemoverValor(string clave)
        {
            try
            {
                Preferences.Default.Remove(clave);
            }
            catch (Exception)
            {
                // Fallback
            }

            _fallbackMemoria.Remove(clave);
        }

        /// <inheritdoc/>
        public async Task<bool> TienePinConfiguradoAsync()
        {
            try
            {
                string? pin = await SecureStorage.Default.GetAsync(KeyAppPin);
                if (!string.IsNullOrWhiteSpace(pin)) return true;
            }
            catch (Exception)
            {
                // Fallback
            }

            string pinFallback = ObtenerValor(KeyAppPin, string.Empty);
            return !string.IsNullOrWhiteSpace(pinFallback);
        }

        /// <inheritdoc/>
        public async Task<bool> RequiereAutenticacionAsync()
        {
            bool activo;
            try
            {
                activo = Preferences.Default.Get(KeyPinActivo, false);
            }
            catch (Exception)
            {
                activo = bool.TryParse(_fallbackMemoria.GetValueOrDefault(KeyPinActivo, "false"), out bool val) && val;
            }

            if (!activo) return false;

            return await TienePinConfiguradoAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> ValidarPinAsync(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin)) return false;

            string? pinGuardado = null;
            try
            {
                pinGuardado = await SecureStorage.Default.GetAsync(KeyAppPin);
            }
            catch (Exception)
            {
                // Fallback
            }

            if (string.IsNullOrWhiteSpace(pinGuardado))
            {
                pinGuardado = ObtenerValor(KeyAppPin, string.Empty);
            }

            bool esValido = string.Equals(pin.Trim(), pinGuardado?.Trim(), StringComparison.Ordinal);
            if (esValido)
            {
                EstaAutenticado = true;
            }

            return esValido;
        }

        /// <inheritdoc/>
        public async Task EstablecerPinAsync(string pin)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pin);

            string pinLimpio = pin.Trim();
            try
            {
                await SecureStorage.Default.SetAsync(KeyAppPin, pinLimpio);
            }
            catch (Exception)
            {
                // Fallback para entornos donde SecureStorage no esté soportado
            }

            EstablecerValor(KeyAppPin, pinLimpio);
            EstablecerValor(KeyPinActivo, "true");
            EstaAutenticado = true;
        }

        /// <inheritdoc/>
        public Task DesactivarPinAsync()
        {
            try
            {
                SecureStorage.Default.Remove(KeyAppPin);
            }
            catch (Exception)
            {
                // Fallback
            }

            RemoverValor(KeyAppPin);
            EstablecerValor(KeyPinActivo, "false");
            EstaAutenticado = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool HaAceptadoTerminos()
        {
            try
            {
                return Preferences.Default.Get(KeyTerminosAceptados, false);
            }
            catch (Exception)
            {
                return bool.TryParse(_fallbackMemoria.GetValueOrDefault(KeyTerminosAceptados, "false"), out bool val) && val;
            }
        }

        /// <inheritdoc/>
        public void AceptarTerminos()
        {
            try
            {
                Preferences.Default.Set(KeyTerminosAceptados, true);
            }
            catch (Exception)
            {
                // Fallback en entornos sin acceso a Preferences
            }

            _fallbackMemoria[KeyTerminosAceptados] = "true";
        }
    }
}

