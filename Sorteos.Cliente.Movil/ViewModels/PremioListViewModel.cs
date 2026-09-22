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
    [QueryProperty(nameof(EsModoSeleccion), "modoSeleccion")]
    public partial class PremioListViewModel : BaseViewModel
    {
        private readonly ILocalDatabaseService _databaseService;
        private List<PremioLocal> _todosLosPremios = [];

        [ObservableProperty]
        public partial ObservableCollection<PremioLocal> Premios { get; set; } = [];

        [ObservableProperty]
        public partial string FiltroTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool EsModoSeleccion { get; set; }

        public bool YaInicializado { get; private set; }

        public PremioListViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Catálogo de Premios";

            WeakReferenceMessenger.Default.Register<PremioGuardadoMessage>(this, (r, m) =>
            {
                OnPremioGuardado(m.Value, m.EsNuevo);
            });
        }

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

        partial void OnFiltroTextoChanged(string value)
        {
            AplicarFiltro();
        }

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

        [RelayCommand]
        private static async Task NuevoPremioAsync()
        {
            await Shell.Current.GoToAsync("PremioPage");
        }

        [RelayCommand]
        private static async Task EditarPremioAsync(PremioLocal premio)
        {
            if (premio == null) return;
            await Shell.Current.GoToAsync($"PremioPage?idPremio={premio.IdPremio}");
        }

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

