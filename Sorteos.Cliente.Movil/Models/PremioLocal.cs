using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que define un premio disponible en el catalogo general del sistema.
    /// </summary>
    [Table("PremioLocal")]
    public class PremioLocal
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        /// <summary>
        /// Identificador primario y autoincrementable del premio.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int IdPremio { get; set; }

        /// <summary>
        /// Posicion o lugar por defecto en la jerarquia de premiacion (1 = 1er lugar, etc.).
        /// </summary>
        [Indexed]
        public int Lugar { get; set; } // 1, 2, 3...

        /// <summary>
        /// Descripcion comercial o monto monetario del premio.
        /// </summary>
        public string DescripcionPremio { get; set; } = string.Empty;

        /// <summary>
        /// Determina si el premio representa una cantidad monetaria en efectivo o un bien en especie.
        /// </summary>
        public bool EsMonetario { get; set; } = true;

        /// <summary>
        /// Descripcion detallada o especificaciones tecnicas del premio.
        /// </summary>
        public string DescripcionLarga { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el premio se encuentra habilitado para ser vinculado a nuevos sorteos.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Obtiene el valor formateado con simbolo de moneda si es monetario, o la descripcion textual en caso de bienes.
        /// </summary>
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

        /// <summary>
        /// Texto ordinal del lugar (ej. "1º Lugar").
        /// </summary>
        [Ignore]
        public string LugarTexto => $"{Lugar}º Lugar";

        /// <summary>
        /// Descripcion consolidada que combina el lugar y el valor para despliegue en catalogos.
        /// </summary>
        [Ignore]
        public string DescripcionCompleta => $"{Lugar}º Lugar - {ValorFormateado}";

        /// <summary>
        /// Conteo de sorteos historicos o activos a los que se encuentra asignado este premio (calculado via subconsulta SQL).
        /// </summary>
        public int CantidadSorteosAsignados { get; set; }
    }
}

