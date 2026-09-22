using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
    public class SorteoCreadoMessage : ValueChangedMessage<SorteoPlantilla>
    {
        public SorteoCreadoMessage(SorteoPlantilla sorteo) : base(sorteo)
        {
        }
    }
}

