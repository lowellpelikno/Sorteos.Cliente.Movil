using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
/// <summary>
    /// Mensaje desacoplado emitido via <see cref="CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger"/>
    /// cuando un cliente ha sido persistido o actualizado satisfactoriamente.
    /// </summary>
    /// <remarks>
    /// Permite mutaciones atomicas en las colecciones del listado de clientes sin recargas globales.
    /// </remarks>
    public class ClienteGuardadoMessage : ValueChangedMessage<Clientes>
    {
        /// <summary>
        /// Indica si el cliente representa un registro nuevo (alta) o una actualizacion sobre un cliente existente (edicion).
        /// </summary>
        public bool EsNuevo { get; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ClienteGuardadoMessage"/>.
        /// </summary>
        /// <param name="cliente">Entidad de cliente guardada.</param>
        /// <param name="esNuevo">Verdadero si es un nuevo registro; falso si es edicion.</param>
        public ClienteGuardadoMessage(Clientes cliente, bool esNuevo) : base(cliente)
        {
            EsNuevo = esNuevo;
        }
    }
}

