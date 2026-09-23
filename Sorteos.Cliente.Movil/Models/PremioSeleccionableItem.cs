using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Modelo y DTO envoltorio observable que representa un premio con casilla de seleccion en el formulario de creacion de sorteos.
    /// </summary>
    public partial class PremioSeleccionableItem : ObservableObject
    {
        /// <summary>
        /// Entidad de premio subyacente.
        /// </summary>
        public PremioLocal Premio { get; set; } = new();

        /// <summary>
        /// Estado de seleccion interactiva del premio para su inclusion en el sorteo.
        /// </summary>
        [ObservableProperty]
        public partial bool EstaSeleccionado { get; set; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioSeleccionableItem"/> sin premio asignado.
        /// </summary>
        public PremioSeleccionableItem()
        {
        }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioSeleccionableItem"/> con un premio y un estado inicial de seleccion.
        /// </summary>
        /// <param name="premio">Instancia de <see cref="PremioLocal"/>.</param>
        /// <param name="seleccionado">Valor inicial del selector.</param>
        public PremioSeleccionableItem(PremioLocal premio, bool seleccionado = false)
        {
            Premio = premio;
            EstaSeleccionado = seleccionado;
        }
    }
}

