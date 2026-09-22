using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("FolioSecuencia")]
    public class FolioSecuencia
    {
        [PrimaryKey]
        public string Clave { get; set; } = "SORTEO";

        public int UltimoFolio { get; set; } = 0;

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}

