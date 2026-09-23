using Sorteos.Cliente.Movil.Helpers;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
/// <summary>
    /// Pruebas unitarias para las utilidades de texto en <see cref="TextoHelper"/>,
    /// evaluando saneamiento de cadenas, deteccion de caracteres especiales, validacion de emails y montos numericos.
    /// </summary>
    public class TextoHelperTests
    {
        [Theory]
        [InlineData("Juan Carlos", false)]
        [InlineData("María José Peña", false)]
        [InlineData("López-García", true)] // Guion es especial
        [InlineData("Carlos123", true)]     // Numeros no permitidos en nombre
        [InlineData("Pedro@Home", true)]   // Arroba no permitida en nombre
        public void ContieneCaracteresEspeciales_ValidaNombres(string input, bool esperado)
        {
            bool resultado = input.ContieneCaracteresEspeciales();
            Assert.Equal(esperado, resultado);
        }

        [Theory]
        [InlineData("5512345678", true)]
        [InlineData("551234567", false)]
        [InlineData("55123456789", false)]
        [InlineData("55123A5678", false)]
        [InlineData("", false)]
        public void ValidacionTelefono_DiezDigitos(string telefono, bool esperado)
        {
            string limpio = telefono.Clean();
            bool esValido = limpio.Length == 10 && limpio.EsSoloNumeros();
            Assert.Equal(esperado, esValido);
        }

        [Theory]
        [InlineData("usuario@dominio.com", true)]
        [InlineData("juan.perez@empresa.com.mx", true)]
        [InlineData("invalido@", false)]
        [InlineData("@dominio.com", false)]
        [InlineData("sin-arroba", false)]
        [InlineData("", false)]
        public void EsEmailValido_ValidaFormatos(string email, bool esperado)
        {
            bool resultado = email.EsEmailValido();
            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public void SoloDigitos_ExtraeNumerosCorrectamente()
        {
            string entrada = "(55) 1234-5678";
            string resultado = entrada.SoloDigitos(10);
            Assert.Equal("5512345678", resultado);
        }

        [Theory]
        [InlineData("Juan   Carlos", true)]   // 3 espacios
        [InlineData("Juan  Carlos", false)]   // 2 espacios
        [InlineData("Juan Carlos", false)]    // 1 espacio
        [InlineData("", false)]
        public void TieneMasDeDosEspaciosConsecutivos_DetectaEspaciadoExcesivo(string input, bool esperado)
        {
            bool resultado = input.TieneMasDeDosEspaciosConsecutivos();
            Assert.Equal(esperado, resultado);
        }

        [Theory]
        [InlineData("  P é r e z  ", "Pérez")]
        [InlineData(" L o p e z ", "Lopez")]
        [InlineData("", "")]
        public void CleanAllWhiteSpace_RemueveTodoEspacioEnBlanco(string input, string esperado)
        {
            string resultado = input.CleanAllWhiteSpace();
            Assert.Equal(esperado, resultado);
        }

        [Theory]
        [InlineData("100", true, 100)]
        [InlineData("1500.50", true, 1500.50)]
        [InlineData("0", false, 0)]          // Monto 0 no es valido
        [InlineData("-50", false, 0)]        // Negativos no validos
        [InlineData("abc", false, 0)]
        public void EsMontoMonetarioValido_ValidaCantidades(string input, bool esperadoValido, decimal esperadoMonto)
        {
            bool resultado = input.EsMontoMonetarioValido(out decimal monto);
            Assert.Equal(esperadoValido, resultado);
            if (esperadoValido)
            {
                Assert.Equal(esperadoMonto, monto);
            }
        }
    }
}

