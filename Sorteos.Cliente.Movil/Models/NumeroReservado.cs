using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("NumeroReservado")]
    public class NumeroReservado
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int SorteoId { get; set; }

        [Indexed]
        public int ClienteId { get; set; }

        [Indexed]
        public int IdEstatus { get; set; } = 1; // 1 = Apartado, 2 = Pagado, 3 = Liberado

        public string Nombre { get; set; } = string.Empty;

        public decimal Costo { get; set; }

        public string FormaDePago { get; set; } = "Efectivo";

        public string ComprobanteUrl { get; set; } = string.Empty;

        public DateTime FechaApartado { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        [Ignore]
        public bool EstaPagado => IdEstatus == 2;

        [Ignore]
        public string CostoFormateado => Costo.ToString("C2", CulturaEsMx);

        [Ignore]
        public string EstatusTexto => IdEstatus switch
        {
            1 => "Apartado",
            2 => "Pagado",
            3 => "Liberado",
            _ => "Desconocido"
        };
    }
}

