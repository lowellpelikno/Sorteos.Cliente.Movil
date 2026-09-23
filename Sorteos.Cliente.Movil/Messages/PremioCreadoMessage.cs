using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
/// <summary>
    /// Mensaje desacoplado emitido via <see cref="CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger"/>
    /// cuando un nuevo premio ha sido generado o seleccionado para ser incorporado en el listado.
    /// </summary>
    public class PremioCreadoMessage : ValueChangedMessage<PremioLocal>
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioCreadoMessage"/>.
        /// </summary>
        /// <param name="value">Instancia del premio creado.</param>
        public PremioCreadoMessage(PremioLocal value) : base(value)
        {
        }
    }
}

