using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
    public class ClienteGuardadoMessage : ValueChangedMessage<Clientes>
    {
        public bool EsNuevo { get; }

        public ClienteGuardadoMessage(Clientes cliente, bool esNuevo) : base(cliente)
        {
            EsNuevo = esNuevo;
        }
    }
}

