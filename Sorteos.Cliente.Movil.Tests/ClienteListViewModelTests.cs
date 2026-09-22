using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;
using Sorteos.Cliente.Movil.ViewModels;
using Xunit;

namespace Sorteos.Cliente.Movil.Tests
{
    public class ClienteListViewModelTests : IAsyncLifetime
    {
        private readonly string _dbPath;
        private readonly LocalDatabaseService _dbService;

        public ClienteListViewModelTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"sorteos_viewmodel_test_{Guid.NewGuid():N}.db3");
            AsignacionNumerosPlantaService asignacionService = new();
            _dbService = new LocalDatabaseService(_dbPath, asignacionService);
        }

        public async Task InitializeAsync()
        {
            await _dbService.InitAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbService.CerrarConexionAsync();

            try
            {
                if (File.Exists(_dbPath)) File.Delete(_dbPath);
                string wal = _dbPath + "-wal";
                if (File.Exists(wal)) File.Delete(wal);
                string shm = _dbPath + "-shm";
                if (File.Exists(shm)) File.Delete(shm);
            }
            catch
            {
                // Limpieza silenciosa
            }
        }

        [Fact]
        public async Task CargarClientesAsync_PueblaColeccionYActualizaTieneClientes()
        {
            // Insertar 2 clientes en la base de datos
            await _dbService.GuardarClienteAsync(new Clientes { Nombre = "Roberto", Telefono = "5511223344" });
            await _dbService.GuardarClienteAsync(new Clientes { Nombre = "Adriana", Telefono = "5522334455" });

            ClienteListViewModel vm = new(_dbService);
            Assert.False(vm.TieneClientes);
            Assert.False(vm.YaInicializado);

            await vm.CargarClientesCommand.ExecuteAsync(null);

            Assert.True(vm.TieneClientes);
            Assert.True(vm.YaInicializado);
            Assert.Equal(2, vm.Clientes.Count);
        }

        [Fact]
        public void MensajeClienteGuardado_EsNuevoTrue_InsertaEnIndiceCeroSinRecrearColeccion()
        {
            ClienteListViewModel vm = new(_dbService);
            Clientes previo = new() { Id = 1, Nombre = "Cliente Existente", Telefono = "1111111111" };
            vm.Clientes = [previo];
            ObservableCollection<Clientes> instanciaColeccionOriginal = vm.Clientes;

            // Enviar mensaje de nuevo cliente
            Clientes nuevo = new()
            {
                Id = 2,
                Nombre = "Nuevo Cliente",
                Telefono = "2222222222",
                NumerosPlantaResumen = "07, 14"
            };

            WeakReferenceMessenger.Default.Send(new ClienteGuardadoMessage(nuevo, esNuevo: true));

            // Debe seguir siendo la misma instancia de coleccion (sin parpadeo ni perdida de scroll)
            Assert.Same(instanciaColeccionOriginal, vm.Clientes);
            Assert.Equal(2, vm.Clientes.Count);
            // El nuevo cliente debe quedar al inicio (indice 0)
            Assert.Equal(2, vm.Clientes[0].Id);
            Assert.Equal("07, 14", vm.Clientes[0].NumerosPlantaResumen);
            Assert.True(vm.Clientes[0].TieneNumerosPlanta);
        }

        [Fact]
        public void MensajeClienteGuardado_EsNuevoFalse_ActualizaEnMismoIndice()
        {
            ClienteListViewModel vm = new(_dbService);
            Clientes c1 = new() { Id = 10, Nombre = "Primero", Telefono = "1111111111" };
            Clientes c2 = new() { Id = 20, Nombre = "Segundo", Telefono = "2222222222", NumerosPlantaResumen = "01" };
            Clientes c3 = new() { Id = 30, Nombre = "Tercero", Telefono = "3333333333" };

            vm.Clientes = [c1, c2, c3];

            // Editar el cliente en indice 1 (Id = 20) agregando nuevos numeros de planta
            Clientes c2Modificado = new()
            {
                Id = 20,
                Nombre = "Segundo Modificado",
                Telefono = "2222222222",
                NumerosPlantaResumen = "01, 09, 45"
            };

            WeakReferenceMessenger.Default.Send(new ClienteGuardadoMessage(c2Modificado, esNuevo: false));

            Assert.Equal(3, vm.Clientes.Count);
            // Debe permanecer en la misma posicion de indice (indice 1)
            Assert.Equal(20, vm.Clientes[1].Id);
            Assert.Equal("Segundo Modificado", vm.Clientes[1].Nombre);
            Assert.Equal("01, 09, 45", vm.Clientes[1].NumerosPlantaResumen);
            Assert.True(vm.Clientes[1].TieneNumerosPlanta);
        }

        [Fact]
        public async Task FiltroTexto_FiltraClientesReactivamente()
        {
            await _dbService.GuardarClienteAsync(new Clientes { Nombre = "Marcos Salazar", Telefono = "5551112233" });
            await _dbService.GuardarClienteAsync(new Clientes { Nombre = "Beatriz Fuentes", Telefono = "5554445566" });

            ClienteListViewModel vm = new(_dbService);
            await vm.CargarClientesCommand.ExecuteAsync(null);
            Assert.Equal(2, vm.Clientes.Count);

            // Cambiar filtro a "Beatriz"
            vm.FiltroTexto = "Beatriz";
            // En el setter, OnFiltroTextoChanged dispara CargarClientesAsync
            await Task.Delay(100);

            Assert.Single(vm.Clientes);
            Assert.Equal("Beatriz Fuentes", vm.Clientes[0].Nombre);
        }
    }
}

