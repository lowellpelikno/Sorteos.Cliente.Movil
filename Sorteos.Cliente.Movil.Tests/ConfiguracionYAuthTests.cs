using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
/// <summary>
    /// Pruebas unitarias para <see cref="ConfiguracionNegocioService"/> y <see cref="AuthService"/>,
    /// validando persistencia en preferencias locales, hash criptografico de PIN y verificacion de credenciales.
    /// </summary>
    public class ConfiguracionYAuthTests
    {
        [Fact]
        public async Task ConfiguracionNegocio_GuardarYRecuperar_FuncionaCorrectamente()
        {
            ConfiguracionNegocioService service = new();
            DatosNegocioDto datos = new()
            {
                NombreNegocio = "Sorteos El Trébol",
                TelefonoContacto = "5512345678",
                BancoNombre = "BBVA",
                CuentaClabe = "012345678901234567",
                NumeroTarjeta = "4152313188992211",
                TitularCuenta = "Juan Pérez González",
                NotasApartado = "Favor de enviar captura del comprobante dentro de 24 horas."
            };

            await service.GuardarDatosNegocioAsync(datos);
            DatosNegocioDto recuperados = await service.ObtenerDatosNegocioAsync();

            Assert.Equal("Sorteos El Trébol", recuperados.NombreNegocio);
            Assert.Equal("5512345678", recuperados.TelefonoContacto);
            Assert.Equal("BBVA", recuperados.BancoNombre);
            Assert.Equal("012345678901234567", recuperados.CuentaClabe);
            Assert.Equal("4152313188992211", recuperados.NumeroTarjeta);
            Assert.Equal("Juan Pérez González", recuperados.TitularCuenta);
            Assert.Equal("Favor de enviar captura del comprobante dentro de 24 horas.", recuperados.NotasApartado);

            string mensajeBancario = await service.GenerarMensajeBancarioAsync();
            Assert.Contains("Sorteos El Trébol", mensajeBancario);
            Assert.Contains("BBVA", mensajeBancario);
            Assert.Contains("012345678901234567", mensajeBancario);
            Assert.Contains("Juan Pérez González", mensajeBancario);
        }

        [Fact]
        public async Task AuthService_CicloDeVidaPin_ValidaCorrectamente()
        {
            AuthService auth = new();

            // 1. Establecer PIN
            await auth.EstablecerPinAsync("4589");
            bool tienePin = await auth.TienePinConfiguradoAsync();
            Assert.True(tienePin);

            bool requiereAuth = await auth.RequiereAutenticacionAsync();
            Assert.True(requiereAuth);

            // 2. Validación con PIN incorrecto
            bool esInvalido = await auth.ValidarPinAsync("1234");
            Assert.False(esInvalido);

            // 3. Validación con PIN correcto
            bool esValido = await auth.ValidarPinAsync("4589");
            Assert.True(esValido);
            Assert.True(auth.EstaAutenticado);

            // 4. Desactivar PIN
            await auth.DesactivarPinAsync();
            bool sigueRequerido = await auth.RequiereAutenticacionAsync();
            Assert.False(sigueRequerido);
        }

        [Fact]
        public void AuthService_AceptacionTerminos_CicloDeVidaCorrecto()
        {
            AuthService auth = new();

            // Estado inicial sin aceptar
            bool inicial = auth.HaAceptadoTerminos();
            Assert.False(inicial);

            // Aceptar términos
            auth.AceptarTerminos();
            bool aceptado = auth.HaAceptadoTerminos();
            Assert.True(aceptado);
        }

        [Fact]
        public async Task TerminosCondicionesViewModel_PrimerArranque_ComportamientoCorrecto()
        {
            AuthService auth = new();
            // Aseguramos estado inicial
            ViewModels.TerminosCondicionesViewModel vm = new(auth, null);

            Assert.True(vm.EsPrimerArranque);
            Assert.False(vm.EsConsulta);
            Assert.False(vm.AceptoTerminos);
            Assert.False(vm.PuedeAceptar);
            Assert.False(vm.AceptarYContinuarCommand.CanExecute(null));

            // Alternar aceptación con comando o propiedad
            vm.AlternarAceptoTerminosCommand.Execute(null);
            Assert.True(vm.AceptoTerminos);
            Assert.True(vm.PuedeAceptar);
            Assert.True(vm.AceptarYContinuarCommand.CanExecute(null));

            // Ejecutar aceptación
            await vm.AceptarYContinuarCommand.ExecuteAsync(null);
            Assert.True(auth.HaAceptadoTerminos());
        }
    }
}

