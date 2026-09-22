using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("SorteoPremioLocal")]
    public class SorteoPremioLocal
    {
        [PrimaryKey, AutoIncrement]
        public int IdSorteoPremio { get; set; }

        [Indexed]
        public int IdSorteo { get; set; }

        [Indexed]
        public int IdPremio { get; set; }
    }
}

