using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que representa un numero de planta fijo asignado a un cliente preferencial.
    /// Garantiza la reserva automatica recurrente del digito en sorteos semanales.
    /// </summary>
    [Table("NumeroPlanta")]
    public class NumeroPlanta
    {
        /// <summary>
        /// Identificador primario y autoincrementable de la suscripcion de planta.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identificador del cliente titular del numero reservado.
        /// </summary>
        [Indexed]
        public int IdCliente { get; set; }

        /// <summary>
        /// Valor numerico fijo reservado en exclusiva para el cliente.
        /// </summary>
        [Indexed]
        public int Numero { get; set; }

        /// <summary>
        /// Banderin que define si el cliente acepta adquirir los numeros complementarios cuando el sorteo se maneja en modalidad combo.
        /// </summary>
        public bool AceptaCombo { get; set; } = true;

        /// <summary>
        /// Indica si la asignacion de planta se encuentra activa y vigente.
        /// </summary>
        public bool Activo { get; set; } = true;
    }
}

