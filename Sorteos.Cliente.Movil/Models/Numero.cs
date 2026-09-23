using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que representa una casilla o numero individual perteneciente a la cuadricula de un sorteo.
    /// </summary>
    [Table("Numero")]
    public class Numero
    {
        /// <summary>
        /// Identificador primario y autoincrementable de la casilla.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int NumeroId { get; set; }

        /// <summary>
        /// Identificador del sorteo al que pertenece la casilla.
        /// Forma parte del indice compuesto IX_Numero_Sorteo_Valor.
        /// </summary>
        [Indexed("IX_Numero_Sorteo_Valor", 1)]
        public int SorteoId { get; set; }

        /// <summary>
        /// Identificador foraneo a la reserva activa asociada; nulo si el numero esta libre o disponible.
        /// </summary>
        [Indexed]
        public int? ReservaId { get; set; } // null = disponible/libre

        /// <summary>
        /// Valor numerico o digito asignado a la casilla en el sorteo.
        /// Forma parte del indice compuesto IX_Numero_Sorteo_Valor.
        /// </summary>
        [Indexed("IX_Numero_Sorteo_Valor", 2)]
        public int NumeroValor { get; set; }

        /// <summary>
        /// Indica si la casilla esta habilitada operativamente dentro del sorteo.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Indica si la casilla se encuentra desocupada y disponible para reserva.
        /// </summary>
        [Ignore]
        public bool EstaDisponible => !ReservaId.HasValue;
    }
}

