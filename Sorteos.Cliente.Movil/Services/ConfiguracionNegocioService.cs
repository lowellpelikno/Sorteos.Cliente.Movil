using System.Text;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Implementacion concreta para la persistencia de datos comerciales del negocio y armado de textos bancarios.
    /// </summary>
    public class ConfiguracionNegocioService : IConfiguracionNegocioService
    {
        private const string KeyNombreNegocio = "Negocio_Nombre";
        private const string KeyTelefonoContacto = "Negocio_Telefono";
        private const string KeyBancoNombre = "Negocio_Banco";
        private const string KeyCuentaClabe = "Negocio_Clabe";
        private const string KeyNumeroTarjeta = "Negocio_Tarjeta";
        private const string KeyTitularCuenta = "Negocio_Titular";
        private const string KeyNotasApartado = "Negocio_NotasApartado";

        private readonly IPreferences? _preferences;
        private readonly Dictionary<string, string> _fallbackMemoria = [];

        /// <summary>
        /// Inicializa el servicio permitiendo inyectar una abstraccion de preferencias para pruebas unitarias.
        /// </summary>
        /// <param name="preferences">Instancia de preferencias nativas o simuladas.</param>
        public ConfiguracionNegocioService(IPreferences? preferences = null)
        {
            try
            {
                _preferences = preferences ?? Preferences.Default;
            }
            catch (Exception)
            {
                _preferences = null;
            }
        }

        private string ObtenerValor(string clave, string valorDefecto)
        {
            if (_preferences != null)
            {
                try
                {
                    return _preferences.Get(clave, valorDefecto);
                }
                catch (Exception)
                {
                    // Fallback
                }
            }

            return _fallbackMemoria.GetValueOrDefault(clave, valorDefecto);
        }

        private void EstablecerValor(string clave, string valor)
        {
            if (_preferences != null)
            {
                try
                {
                    _preferences.Set(clave, valor);
                    return;
                }
                catch (Exception)
                {
                    // Fallback
                }
            }

            _fallbackMemoria[clave] = valor;
        }

        /// <inheritdoc/>
        public Task<DatosNegocioDto> ObtenerDatosNegocioAsync()
        {
            DatosNegocioDto datos = new()
            {
                NombreNegocio = ObtenerValor(KeyNombreNegocio, string.Empty),
                TelefonoContacto = ObtenerValor(KeyTelefonoContacto, string.Empty),
                BancoNombre = ObtenerValor(KeyBancoNombre, string.Empty),
                CuentaClabe = ObtenerValor(KeyCuentaClabe, string.Empty),
                NumeroTarjeta = ObtenerValor(KeyNumeroTarjeta, string.Empty),
                TitularCuenta = ObtenerValor(KeyTitularCuenta, string.Empty),
                NotasApartado = ObtenerValor(KeyNotasApartado, string.Empty)
            };

            return Task.FromResult(datos);
        }

        /// <inheritdoc/>
        public Task GuardarDatosNegocioAsync(DatosNegocioDto datos)
        {
            ArgumentNullException.ThrowIfNull(datos);

            EstablecerValor(KeyNombreNegocio, datos.NombreNegocio?.Trim() ?? string.Empty);
            EstablecerValor(KeyTelefonoContacto, datos.TelefonoContacto?.Trim() ?? string.Empty);
            EstablecerValor(KeyBancoNombre, datos.BancoNombre?.Trim() ?? string.Empty);
            EstablecerValor(KeyCuentaClabe, datos.CuentaClabe?.Trim() ?? string.Empty);
            EstablecerValor(KeyNumeroTarjeta, datos.NumeroTarjeta?.Trim() ?? string.Empty);
            EstablecerValor(KeyTitularCuenta, datos.TitularCuenta?.Trim() ?? string.Empty);
            EstablecerValor(KeyNotasApartado, datos.NotasApartado?.Trim() ?? string.Empty);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<string> GenerarMensajeBancarioAsync()
        {
            DatosNegocioDto datos = await ObtenerDatosNegocioAsync();

            StringBuilder sb = new();
            if (!string.IsNullOrWhiteSpace(datos.NombreNegocio))
            {
                sb.AppendLine($"*{datos.NombreNegocio}*");
            }

            if (!string.IsNullOrWhiteSpace(datos.BancoNombre))
            {
                sb.AppendLine($"Banco: {datos.BancoNombre}");
            }

            if (!string.IsNullOrWhiteSpace(datos.CuentaClabe))
            {
                sb.AppendLine($"CLABE: {datos.CuentaClabe}");
            }

            if (!string.IsNullOrWhiteSpace(datos.NumeroTarjeta))
            {
                sb.AppendLine($"Tarjeta: {datos.NumeroTarjeta}");
            }

            if (!string.IsNullOrWhiteSpace(datos.TitularCuenta))
            {
                sb.AppendLine($"Titular: {datos.TitularCuenta}");
            }

            if (!string.IsNullOrWhiteSpace(datos.NotasApartado))
            {
                sb.AppendLine();
                sb.AppendLine(datos.NotasApartado);
            }

            return sb.ToString().TrimEnd();
        }
    }
}

