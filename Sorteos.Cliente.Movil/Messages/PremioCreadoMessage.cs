using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
    public class PremioCreadoMessage : ValueChangedMessage<PremioLocal>
    {
        public PremioCreadoMessage(PremioLocal value) : base(value)
        {
        }
    }
}

