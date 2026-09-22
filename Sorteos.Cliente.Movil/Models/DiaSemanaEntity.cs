using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("DiaSemanaItem")]
    public class DiaSemanaEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int IdDia { get; set; } // 1 = Lunes, ..., 7 = Domingo

        public string NombreDia { get; set; } = string.Empty;

        public string InicialDia { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }
    }
}

