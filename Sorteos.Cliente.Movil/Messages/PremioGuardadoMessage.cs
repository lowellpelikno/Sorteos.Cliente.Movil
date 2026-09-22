using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
    public class PremioGuardadoMessage : ValueChangedMessage<PremioLocal>
    {
        public bool EsNuevo { get; }

        public PremioGuardadoMessage(PremioLocal premio, bool esNuevo) : base(premio)
        {
            EsNuevo = esNuevo;
        }
    }
}

