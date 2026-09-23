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
    /// ViewModel responsable de la gestion y exploracion del catalogo de premios.
    /// </summary>
    /// <remarks>
    /// Consideraciones de Usabilidad:
    /// - Modo Dual (Administracion vs Selector): Admite el parametro <see cref="EsModoSeleccion"/> para operar
    ///   como selector modal invocado durante la creacion de un sorteo o como administrador general de premios.
    /// - Jerarquia Visual Ordenada: Mantiene los premios clasificados por su posicion (<see cref="PremioLocal.Lugar"/>)
    ///   tanto en memoria como en pantalla, garantizando que el primer lugar siempre encabece el listado.
    /// - Filtrado Predictivo Multicriterio: Permite buscar por texto de lugar, descripcion del bien o valor economico.
    /// - Proteccion contra Eliminaciones Invalidas: Impide suprimir premios vinculados a sorteos vigentes,
    ///   explicando la razon de forma constructiva para preservar la integridad de los registros.
    /// - Actualizacion Atomica Reactiva: Refleja altas y ediciones en tiempo real sin recargar la pantalla completa.
    /// </remarks>
    [QueryProperty(nameof(EsModoSeleccion), "modoSeleccion")]
    public partial class PremioListViewModel : BaseViewModel
    {
        private readonly ILocalDatabaseService _databaseService;
        private List<PremioLocal> _todosLosPremios = [];

        /// <summary>
        /// Obtiene o establece la coleccion observable de premios para visualizacion en tarjetas.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<PremioLocal> Premios { get; set; } = [];

        /// <summary>
        /// Obtiene o establece el texto de busqueda para filtrar los premios en tiempo real.
        /// </summary>
        [ObservableProperty]
        public partial string FiltroTexto { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si la pantalla fue abierta como selector interactivo desde el asistente de sorteos.
        /// </summary>
        [ObservableProperty]
        public partial bool EsModoSeleccion { get; set; }

        /// <summary>
        /// Indica si la coleccion ya fue recuperada desde SQLite al menos una vez en la sesion actual.
        /// </summary>
        public bool YaInicializado { get; private set; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioListViewModel"/> y suscribe el receptor de sincronizacion.
        /// </summary>
        /// <param name="databaseService">Servicio de datos local.</param>
        public PremioListViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Catálogo de Premios";

            WeakReferenceMessenger.Default.Register<PremioGuardadoMessage>(this, (r, m) =>
            {
                OnPremioGuardado(m.Value, m.EsNuevo);
            });
        }

        /// <summary>
        /// Inserta o actualiza un premio en su posicion ordinal correcta preservando la jerarquia visual.
        /// </summary>
        private void OnPremioGuardado(PremioLocal premio, bool esNuevo)
        {
            if (esNuevo)
            {
                _todosLosPremios.Add(premio);
                _todosLosPremios = [.. _todosLosPremios.OrderBy(p => p.Lugar)];

                int index = 0;
                while (index < Premios.Count && Premios[index].Lugar < premio.Lugar)
                {
                    index++;
                }
                Premios.Insert(index, premio);
            }
            else
            {
                int indexBase = _todosLosPremios.FindIndex(p => p.IdPremio == premio.IdPremio);
                if (indexBase >= 0)
                {
                    _todosLosPremios[indexBase] = premio;
                    _todosLosPremios = [.. _todosLosPremios.OrderBy(p => p.Lugar)];
                }

                int indexVisible = -1;
                for (int i = 0; i < Premios.Count; i++)
                {
                    if (Premios[i].IdPremio == premio.IdPremio)
                    {
                        indexVisible = i;
                        break;
                    }
                }

                if (indexVisible >= 0)
                {
                    Premios[indexVisible] = premio;
                }
                else
                {
                    AplicarFiltro();
                }
            }
        }

        /// <summary>
        /// Recupera la lista completa de premios desde la base de datos y aplica el filtro vigente.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Muestra el indicador de progreso <see cref="BaseViewModel.IsBusy"/> y presenta dialogos
        /// explicativos en caso de fallo de acceso al almacenamiento.
        /// </remarks>
        [RelayCommand]
        public async Task CargarPremiosAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                _todosLosPremios = await _databaseService.ObtenerPremiosAsync();
                AplicarFiltro();
                YaInicializado = true;
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible consultar el catálogo de premios debido a un inconveniente con el almacenamiento local.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al cargar los premios. Por favor, inténtalo más tarde.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Aplica la busqueda en memoria al cambiar el termino de filtro.
        /// </summary>
        partial void OnFiltroTextoChanged(string value)
        {
            AplicarFiltro();
        }

        /// <summary>
        /// Filtra la coleccion visible evaluando lugar, descripcion y valor comercial.
        /// </summary>
        private void AplicarFiltro()
        {
            if (string.IsNullOrWhiteSpace(FiltroTexto))
            {
                Premios = new ObservableCollection<PremioLocal>(_todosLosPremios);
            }
            else
            {
                string termino = FiltroTexto.ToLowerInvariant();
                IEnumerable<PremioLocal> filtrados = _todosLosPremios.Where(p => 
                    p.LugarTexto.ToLowerInvariant().Contains(termino) ||
                    p.DescripcionLarga.ToLowerInvariant().Contains(termino) ||
                    p.ValorFormateado.ToLowerInvariant().Contains(termino));
                Premios = new ObservableCollection<PremioLocal>(filtrados);
            }
        }

        /// <summary>
        /// Conduce al formulario para registrar un nuevo premio en el catalogo.
        /// </summary>
        [RelayCommand]
        private static async Task NuevoPremioAsync()
        {
            await Shell.Current.GoToAsync("PremioPage");
        }

        /// <summary>
        /// Abre el formulario de edicion transfiriendo el identificador del premio.
        /// </summary>
        /// <param name="premio">Instancia del premio a modificar.</param>
        [RelayCommand]
        private static async Task EditarPremioAsync(PremioLocal premio)
        {
            if (premio == null) return;
            await Shell.Current.GoToAsync($"PremioPage?idPremio={premio.IdPremio}");
        }

        /// <summary>
        /// Solicita confirmacion y elimina el premio seleccionado si no esta asignado a un sorteo vigente.
        /// </summary>
        /// <remarks>
        /// Usabilidad: Notifica de forma clara si el elemento esta protegido por dependencias activas, evitando desconcierto.
        /// </remarks>
        /// <param name="premio">Premio a eliminar.</param>
        [RelayCommand]
        private async Task EliminarPremioAsync(PremioLocal premio)
        {
            if (premio == null) return;

            bool confirmar = await Shell.Current.DisplayAlertAsync(
                "Confirmación",
                $"¿Deseas eliminar el {premio.LugarTexto} ({premio.ValorFormateado})?",
                "Eliminar",
                "Cancelar");

            if (!confirmar) return;

            try
            {
                IsBusy = true;
                int resultado = await _databaseService.EliminarPremioAsync(premio.IdPremio);
                if (resultado == 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "No permitido",
                        "Este premio no se puede eliminar porque está asignado a uno o más sorteos.",
                        "Aceptar");
                }
                else
                {
                    PremioLocal? itemVisible = Premios.FirstOrDefault(p => p.IdPremio == premio.IdPremio);
                    if (itemVisible != null)
                    {
                        Premios.Remove(itemVisible);
                    }

                    itemVisible = _todosLosPremios.FirstOrDefault(p => p.IdPremio == premio.IdPremio);
                    if (itemVisible != null)
                    {
                        _todosLosPremios.Remove(itemVisible);
                    }
                }
            }
            catch (SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible eliminar el premio seleccionado debido a una restricción en la base de datos.", "Aceptar");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al intentar eliminar el premio.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Comando activado al presionar sobre una tarjeta de premio.
        /// </summary>
        /// <remarks>
        /// Usabilidad: En modo seleccion, devuelve el premio al formulario de creacion de sorteos y cierra la vista;
        /// en modo catalogo regular, abre la ficha de edicion.
        /// </remarks>
        /// <param name="premio">Premio seleccionado por el usuario.</param>
        [RelayCommand]
        private async Task SeleccionarPremioAsync(PremioLocal premio)
        {
            if (premio == null) return;

            if (EsModoSeleccion)
            {
                WeakReferenceMessenger.Default.Send(new PremioCreadoMessage(premio));
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await PremioListViewModel.EditarPremioAsync(premio);
            }
        }
    }
}

