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
    /// <summary>
    /// ViewModel encargado del directorio y administracion de clientes registrados.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Busqueda Reactiva e Incremental: Al teclear en <see cref="FiltroTexto"/> se filtra en tiempo real por nombre,
    ///   apellidos o numero telefonico, agilizando la localizacion de contactos sin pasos intermedios.
    /// - Manejo de Estados Vacios: Expone <see cref="TieneClientes"/> para alternar graficos o mensajes orientativos
    ///   cuando la agenda este vacia o cuando el criterio de busqueda no arroje coincidencias.
    /// - Mutacion Atomica y Cero Parpadeos: Gracias a <see cref="WeakReferenceMessenger"/>, las altas y ediciones
    ///   se reflejan puntualmente en <see cref="Clientes"/> sin recargar la coleccion completa, manteniendo intacta la posicion de scroll.
    /// - Prevencion de Destruccion Accidental: El comando de eliminacion solicita una confirmacion explicita advirtiendo
    ///   que tambien se retiraran los numeros de planta asociados.
    /// </remarks>
    public partial class ClienteListViewModel : BaseViewModel
    {
        private readonly ILocalDatabaseService _databaseService;

        /// <summary>
        /// Obtiene o establece la lista observable de clientes para renderizado en la interfaz.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<Clientes> Clientes { get; set; } = [];

        /// <summary>
        /// Obtiene o establece el texto del cuadro de busqueda de clientes.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Cada modificacion activa la recarga automatica filtrada.
        /// </remarks>
        [ObservableProperty]
        public partial string FiltroTexto { get; set; } = string.Empty;

        /// <summary>
        /// Indica si existen registros en la lista actual para controlar la visibilidad del estado vacio en la vista.
        /// </summary>
        public bool TieneClientes => Clientes?.Count > 0;

        /// <summary>
        /// Indica si el listado ya realizo su primera carga exitosa en la sesion actual.
        /// </summary>
        public bool YaInicializado { get; private set; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ClienteListViewModel"/> y suscribe el receptor de altas y ediciones.
        /// </summary>
        /// <param name="databaseService">Servicio de datos SQLite local.</param>
        public ClienteListViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Directorio de Clientes";

            WeakReferenceMessenger.Default.Register<ClienteGuardadoMessage>(this, (r, m) =>
            {
                OnClienteGuardado(m.Value, m.EsNuevo);
            });
        }

        /// <summary>
        /// Procesa la insercion o actualizacion en el indice correspondiente de la lista observable.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Evita recargas ciegas globales, ofreciendo una actualizacion visual instantanea al usuario.
        /// </remarks>
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

        /// <summary>
        /// Consulta los clientes en la base de datos local segun el filtro vigente con un limite de seguridad para rendimiento.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Gestiona <see cref="BaseViewModel.IsBusy"/> para desplegar el spinner de carga y reporta avisos legibles
        /// si ocurre un incidente de lectura en disco.
        /// </remarks>
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

        /// <summary>
        /// Metodo parcial desencadenado al cambiar el termino de busqueda.
        /// </summary>
        async partial void OnFiltroTextoChanged(string value)
        {
            await CargarClientesAsync();
        }

        /// <summary>
        /// Navega hacia el formulario de creacion de un nuevo cliente.
        /// </summary>
        [RelayCommand]
        private static async Task NuevoClienteAsync()
        {
            await Shell.Current.GoToAsync("ClienteFormPage");
        }

        /// <summary>
        /// Navega hacia el formulario de edicion transfiriendo el identificador del cliente seleccionado.
        /// </summary>
        /// <param name="cliente">Instancia del cliente a editar.</param>
        [RelayCommand]
        private static async Task EditarClienteAsync(Clientes cliente)
        {
            if (cliente == null) return;
            await Shell.Current.GoToAsync($"ClienteFormPage?idCliente={cliente.Id}");
        }

        /// <summary>
        /// Solicita confirmacion interactiva y elimina el registro del cliente y sus numeros de planta de forma transaccional.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Previene borrados accidentales mediante dialogo modal y retira el elemento en memoria de forma inmediata.
        /// </remarks>
        /// <param name="cliente">Cliente seleccionado para eliminacion.</param>
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

