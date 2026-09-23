using CommunityToolkit.Mvvm.Messaging.Messages;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Messages
{
/// <summary>
    /// Mensaje desacoplado emitido via <see cref="CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger"/>
    /// cuando un premio ha sido guardado o actualizado en la base de datos local.
    /// </summary>
    /// <remarks>
    /// Permite actualizar la coleccion observable en la pantalla de listado de premios de forma atomica.
    /// </remarks>
    public class PremioGuardadoMessage : ValueChangedMessage<PremioLocal>
    {
        /// <summary>
        /// Indica si el premio representa un nuevo registro en el catalogo o una modificacion de uno existente.
        /// </summary>
        public bool EsNuevo { get; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioGuardadoMessage"/>.
        /// </summary>
        /// <param name="premio">Entidad de premio persistida.</param>
        /// <param name="esNuevo">Verdadero si es un nuevo registro; falso si es edicion.</param>
        public PremioGuardadoMessage(PremioLocal premio, bool esNuevo) : base(premio)
        {
            EsNuevo = esNuevo;
        }
    }
}

