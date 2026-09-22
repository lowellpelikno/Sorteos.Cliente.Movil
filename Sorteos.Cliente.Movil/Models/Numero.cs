using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("Numero")]
    public class Numero
    {
        [PrimaryKey, AutoIncrement]
        public int NumeroId { get; set; }

        [Indexed("IX_Numero_Sorteo_Valor", 1)]
        public int SorteoId { get; set; }

        [Indexed]
        public int? ReservaId { get; set; } // null = disponible/libre

        [Indexed("IX_Numero_Sorteo_Valor", 2)]
        public int NumeroValor { get; set; }

        public bool Activo { get; set; } = true;

        [Ignore]
        public bool EstaDisponible => !ReservaId.HasValue;
    }
}

