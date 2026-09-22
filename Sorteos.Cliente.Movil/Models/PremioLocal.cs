using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("PremioLocal")]
    public class PremioLocal
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        [PrimaryKey, AutoIncrement]
        public int IdPremio { get; set; }

        [Indexed]
        public int Lugar { get; set; } // 1, 2, 3...

        public string DescripcionPremio { get; set; } = string.Empty;

        public bool EsMonetario { get; set; } = true;

        public string DescripcionLarga { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [Ignore]
        public string ValorFormateado
        {
            get
            {
                if (EsMonetario && decimal.TryParse(DescripcionPremio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valInv))
                {
                    return valInv.ToString("C2", CulturaEsMx);
                }

                if (decimal.TryParse(DescripcionPremio, NumberStyles.Any, CulturaEsMx, out decimal valMx))
                {
                    return valMx.ToString("C2", CulturaEsMx);
                }

                return DescripcionPremio;
            }
        }

        [Ignore]
        public string LugarTexto => $"{Lugar}º Lugar";

        [Ignore]
        public string DescripcionCompleta => $"{Lugar}º Lugar - {ValorFormateado}";

        public int CantidadSorteosAsignados { get; set; }
    }
}

