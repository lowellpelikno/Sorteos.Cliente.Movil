using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SQLite;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class ClienteListViewModel : BaseViewModel
    {
        private readonly ILocalDatabaseService _databaseService;

        [ObservableProperty]
        public partial ObservableCollection<Clientes> Clientes { get; set; } = [];

        [ObservableProperty]
        public partial string FiltroTexto { get; set; } = string.Empty;

        public bool TieneClientes => Clientes?.Count > 0;

        public bool YaInicializado { get; private set; }

        public ClienteListViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Directorio de Clientes";

            WeakReferenceMessenger.Default.Register<ClienteGuardadoMessage>(this, (r, m) =>
            {
                OnClienteGuardado(m.Value, m.EsNuevo);
            });
        }

        private void OnClienteGuardado(Clientes cliente, bool esNuevo)
        {
            if (esNuevo)
            {
                Clientes.Insert(0, cliente);
                OnPropertyChanged(nameof(TieneClientes));
            }
            else
            {
                int index = -1;
                for (int i = 0; i < Clientes.Count; i++)
                {
                    if (Clientes[i].Id == cliente.Id)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    Clientes[index] = cliente;
                }
            }
        }

        [RelayCommand]
        public async Task CargarClientesAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                List<Clientes> lista = await _databaseService.BuscarClientesAsync(FiltroTexto, limite: 100);
                Clientes = new ObservableCollection<Clientes>(lista);
                OnPropertyChanged(nameof(TieneClientes));
                YaInicializado = true;
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar la lista de clientes debido a un problema con el almacenamiento local. Por favor, reintenta la operación.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al cargar los clientes. Por favor, inténtalo de nuevo más tarde.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        async partial void OnFiltroTextoChanged(string value)
        {
            await CargarClientesAsync();
        }

        [RelayCommand]
        private static async Task NuevoClienteAsync()
        {
            await Shell.Current.GoToAsync("ClienteFormPage");
        }

        [RelayCommand]
        private static async Task EditarClienteAsync(Clientes cliente)
        {
            if (cliente == null) return;
            await Shell.Current.GoToAsync($"ClienteFormPage?idCliente={cliente.Id}");
        }

        [RelayCommand]
        private async Task EliminarClienteAsync(Clientes cliente)
        {
            if (cliente == null) return;

            bool confirmar = await Shell.Current.DisplayAlertAsync(
                "Confirmar Eliminación",
                $"¿Deseas eliminar al cliente {cliente.NombreCompleto}? Se eliminarán también sus números de planta.",
                "Eliminar",
                "Cancelar");

            if (!confirmar) return;

            try
            {
                IsBusy = true;
                await _databaseService.EliminarClienteAsync(cliente.Id);
                Clientes.Remove(cliente);
                OnPropertyChanged(nameof(TieneClientes));
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible eliminar el cliente debido a una restricción en el almacenamiento local.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al intentar eliminar el cliente seleccionado.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

