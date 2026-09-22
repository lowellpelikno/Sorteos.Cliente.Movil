using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("NumeroPlanta")]
    public class NumeroPlanta
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int IdCliente { get; set; }

        [Indexed]
        public int Numero { get; set; }

        public bool AceptaCombo { get; set; } = true;

        public bool Activo { get; set; } = true;
    }
}

