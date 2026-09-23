using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
/// <summary>
    /// Mensaje desacoplado emitido via <see cref="CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger"/>
    /// cuando un nuevo sorteo ha sido creado y guardado en la base de datos local.
    /// </summary>
    /// <remarks>
    /// Permite a las pantallas de agenda y dashboard reflejar el sorteo de forma inmediata sin recarga indiscriminada.
    /// </remarks>
    public class SorteoCreadoMessage : ValueChangedMessage<SorteoPlantilla>
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SorteoCreadoMessage"/>.
        /// </summary>
        /// <param name="sorteo">Instancia del sorteo recien creado.</param>
        public SorteoCreadoMessage(SorteoPlantilla sorteo) : base(sorteo)
        {
        }
    }
}

