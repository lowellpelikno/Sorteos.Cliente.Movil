using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("ReservaPremiada")]
    public class ReservaPremiada
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int IdNumerosReservados { get; set; }

        [Indexed]
        public int IdSorteo { get; set; }

        [Indexed]
        public int IdPremio { get; set; }

        public int NumeroGanador { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Ignore]
        public string DescripcionPremio { get; set; } = string.Empty;

        [Ignore]
        public string NombreGanador { get; set; } = string.Empty;

        [Ignore]
        public string TelefonoGanador { get; set; } = string.Empty;
    }
}

