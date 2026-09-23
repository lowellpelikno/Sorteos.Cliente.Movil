using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que asocia una reserva ganadora con un premio adjudicado en un sorteo.
    /// </summary>
    [Table("ReservaPremiada")]
    public class ReservaPremiada
    {
        /// <summary>
        /// Identificador primario y autoincrementable de la premiacion.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identificador de la reserva de boletos premiada (o 0 si resulto desierto).
        /// </summary>
        [Indexed]
        public int IdNumerosReservados { get; set; }

        /// <summary>
        /// Identificador del sorteo en el cual se suscito la premiacion.
        /// </summary>
        [Indexed]
        public int IdSorteo { get; set; }

        /// <summary>
        /// Identificador del premio concedido.
        /// </summary>
        [Indexed]
        public int IdPremio { get; set; }

        /// <summary>
        /// Numero oficial del boleto favorecido.
        /// </summary>
        public int NumeroGanador { get; set; }

        /// <summary>
        /// Fecha y hora en la que se registro el resultado del sorteo.
        /// </summary>
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>
        /// Descripcion comercial del premio concedido (poblada en memoria para despliegue visual).
        /// </summary>
        [Ignore]
        public string DescripcionPremio { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del comprador ganador (poblado en memoria para despliegue visual).
        /// </summary>
        [Ignore]
        public string NombreGanador { get; set; } = string.Empty;

        /// <summary>
        /// Telefono de contacto del ganador (poblado en memoria para despliegue visual).
        /// </summary>
        [Ignore]
        public string TelefonoGanador { get; set; } = string.Empty;
    }
}

