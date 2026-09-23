using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
/// <summary>
    /// Pruebas unitarias para <see cref="AsignacionNumerosPlantaService"/>,
    /// verificando algoritmos de generacion de combos teoricos, agrupamiento matematico y asignacion de numeros de planta.
    /// </summary>
    public class AsignacionNumerosPlantaServiceTests
    {
        private readonly AsignacionNumerosPlantaService _service = new();

        [Fact]
        public void GenerarCombosTeoricos_SorteoUnaOportunidad_GeneraNumerosIndividuales()
        {
            // 100 numeros con 1 oportunidad -> 100 combos de 1 numero
            List<List<int>> combos = _service.GenerarCombosTeoricos(100, 1);

            Assert.Equal(100, combos.Count);
            Assert.Contains(combos, c => c.Count == 1 && c[0] == 0);
            Assert.Contains(combos, c => c.Count == 1 && c[0] == 7);
        }

        [Fact]
        public void GenerarCombosTeoricos_SorteoDosOportunidades_GeneraParesConPaso()
        {
            // 100 numeros con 2 oportunidades -> 50 combos de 2 numeros (paso = 50)
            List<List<int>> combos = _service.GenerarCombosTeoricos(100, 2);

            Assert.Equal(50, combos.Count);
            // El primer combo debe ser [1, 51]
            Assert.Equal([1, 51], combos[0]);
            // El combo con el ultimo numero debe contener 0 (mapeo del 100 a 0)
            Assert.Contains(combos, c => c.Contains(0));
        }

        [Fact]
        public void ResolverAsignacionesSorteo_UnaOportunidad_AsignaNumerosExactos()
        {
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 10, Numero = 7, Activo = true },
                new NumeroPlanta { Id = 2, IdCliente = 10, Numero = 42, Activo = true }
            ];

            List<Clientes> clientes = [
                new Clientes { Id = 10, Nombre = "Juan", ApellidoPaterno = "Pérez" }
            ];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(100, 1, plantas, clientes);

            Assert.Single(resultado);
            Assert.Equal(10, resultado[0].IdCliente);
            Assert.Equal("Juan Pérez", resultado[0].NombreCliente);
            Assert.Equal([7, 42], resultado[0].NumerosAsignados);
            Assert.False(resultado[0].EsComboCompleto);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_DosOportunidades_AsignaComboCompleto()
        {
            // Cliente 10 tiene el numero de planta 1. Con 2 oportunidades y 100 numeros, su combo es [1, 51].
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 10, Numero = 1, Activo = true }
            ];

            List<Clientes> clientes = [
                new Clientes { Id = 10, Nombre = "María", ApellidoPaterno = "Gómez" }
            ];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(100, 2, plantas, clientes);

            Assert.Single(resultado);
            Assert.Equal([1, 51], resultado[0].NumerosAsignados);
            Assert.True(resultado[0].EsComboCompleto);
            Assert.Equal("Combo completo", resultado[0].Observaciones);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_ColisionEntreClientes_DivideAsignacion()
        {
            // Cliente 1 tiene el 1 y Cliente 2 tiene el 51. En 100 numeros / 2 ops colisionan en el mismo combo [1, 51].
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 1, Numero = 1, Activo = true },
                new NumeroPlanta { Id = 2, IdCliente = 2, Numero = 51, Activo = true }
            ];

            List<Clientes> clientes = [
                new Clientes { Id = 1, Nombre = "Cliente Uno" },
                new Clientes { Id = 2, Nombre = "Cliente Dos" }
            ];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(100, 2, plantas, clientes);

            Assert.Equal(2, resultado.Count);
            AsignacionClienteSorteo asig1 = resultado.First(r => r.IdCliente == 1);
            AsignacionClienteSorteo asig2 = resultado.First(r => r.IdCliente == 2);

            Assert.Equal([1], asig1.NumerosAsignados);
            Assert.False(asig1.EsComboCompleto);
            Assert.Contains("Colisión", asig1.Observaciones);

            Assert.Equal([51], asig2.NumerosAsignados);
            Assert.False(asig2.EsComboCompleto);
            Assert.Contains("Colisión", asig2.Observaciones);
        }

        [Fact]
        public void GenerarCombosTeoricos_CuatroOportunidades_GeneraGruposDeCuatro()
        {
            // 100 numeros / 4 oportunidades -> 25 combos de 4 numeros cada uno (paso = 25)
            List<List<int>> combos = _service.GenerarCombosTeoricos(100, 4);

            Assert.Equal(25, combos.Count);
            // El combo para el numero 1: 1, 1+25=26, 1+50=51, 1+75=76
            Assert.Equal([1, 26, 51, 76], combos[0]);
            // El combo con el ultimo paso debe terminar en 0 (mapeo del 100)
            Assert.Equal([25, 50, 75, 0], combos[24]);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_NumerosPlantaInactivos_NoSeAsignan()
        {
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 5, Numero = 12, Activo = false }, // Inactivo
                new NumeroPlanta { Id = 2, IdCliente = 5, Numero = 44, Activo = true }   // Activo
            ];

            List<Clientes> clientes = [new Clientes { Id = 5, Nombre = "Lucía" }];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(100, 1, plantas, clientes);

            Assert.Single(resultado);
            Assert.Equal([44], resultado[0].NumerosAsignados);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_ListaVaciaONula_RetornaVacioSinExcepciones()
        {
            List<AsignacionClienteSorteo> resVacio = _service.ResolverAsignacionesSorteo(100, 2, [], []);
            Assert.Empty(resVacio);

            List<AsignacionClienteSorteo> resNulo = _service.ResolverAsignacionesSorteo(100, 2, null!, null!);
            Assert.Empty(resNulo);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_NumeroFueraDeRango_SeDescartaAutomaticamente()
        {
            // Sorteo de solo 10 numeros (numeros validos del 1 al 9 y el 0).
            // Cliente tiene el numero 80 (fuera de rango) y el 5 (dentro de rango).
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 3, Numero = 80, Activo = true },
                new NumeroPlanta { Id = 2, IdCliente = 3, Numero = 5, Activo = true }
            ];

            List<Clientes> clientes = [new Clientes { Id = 3, Nombre = "Roberto" }];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(10, 1, plantas, clientes);

            Assert.Single(resultado);
            Assert.Equal([5], resultado[0].NumerosAsignados);
            Assert.DoesNotContain(80, resultado[0].NumerosAsignados);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_TodosNumerosFueraDeRango_RetornaVacio()
        {
            // Sorteo de 10 numeros y cliente solo tiene el 80
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 4, Numero = 80, Activo = true }
            ];

            List<Clientes> clientes = [new Clientes { Id = 4, Nombre = "Esteban" }];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(10, 1, plantas, clientes);

            Assert.Empty(resultado);
        }

        [Fact]
        public void ResolverAsignacionesSorteo_ColisionDeIntereses_DisuelveComboEnNumerosSueltos()
        {
            // Sorteo de 100 numeros con 2 oportunidades (paso = 50).
            // Combo teorico: [1, 51]. Cliente 10 tiene el 1 y Cliente 20 tiene el 51.
            List<NumeroPlanta> plantas = [
                new NumeroPlanta { Id = 1, IdCliente = 10, Numero = 1, Activo = true },
                new NumeroPlanta { Id = 2, IdCliente = 20, Numero = 51, Activo = true }
            ];

            List<Clientes> clientes = [
                new Clientes { Id = 10, Nombre = "Cliente Diez" },
                new Clientes { Id = 20, Nombre = "Cliente Veinte" }
            ];

            List<AsignacionClienteSorteo> resultado = _service.ResolverAsignacionesSorteo(100, 2, plantas, clientes);

            Assert.Equal(2, resultado.Count);

            AsignacionClienteSorteo asig10 = resultado.First(r => r.IdCliente == 10);
            AsignacionClienteSorteo asig20 = resultado.First(r => r.IdCliente == 20);

            // Cada cliente recibe unicamente su numero suelto
            Assert.Equal([1], asig10.NumerosAsignados);
            Assert.False(asig10.EsComboCompleto);
            Assert.Contains("grupo disuelto", asig10.Observaciones);

            Assert.Equal([51], asig20.NumerosAsignados);
            Assert.False(asig20.EsComboCompleto);
            Assert.Contains("grupo disuelto", asig20.Observaciones);
        }
    }
}

