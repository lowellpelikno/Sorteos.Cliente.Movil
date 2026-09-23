using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que representa un dia dentro del ciclo semanal operativo (Lunes a Domingo).
    /// </summary>
    [Table("DiaSemanaItem")]
    public class DiaSemanaEntity
    {
        /// <summary>
        /// Identificador primario y autoincrementable del registro de dia.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identificador secuencial del dia de la semana (1 = Lunes, ..., 7 = Domingo).
        /// </summary>
        [Indexed]
        public int IdDia { get; set; } // 1 = Lunes, ..., 7 = Domingo

        /// <summary>
        /// Nombre en texto del dia de la semana (Lunes, Martes, etc.).
        /// </summary>
        public string NombreDia { get; set; } = string.Empty;

        /// <summary>
        /// Letra inicial del dia para representacion compacta (L, M, M, J, V, S, D).
        /// </summary>
        public string InicialDia { get; set; } = string.Empty;

        /// <summary>
        /// Fecha calendario asignada para el dia dentro de la semana activa.
        /// </summary>
        public DateTime Fecha { get; set; }
    }
}

