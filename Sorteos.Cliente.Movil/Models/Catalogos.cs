using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    public enum EstadoNumeroSorteo
    {
        Disponible = 0,
        Apartado = 1,
        Pagado = 2,
        Ganador = 3,
        Planta = 4
    }

    public enum EstatusSorteo
    {
        Creado = 1,
        EnJuego = 2,
        Finalizado = 3
    }

    public enum EstatusApartado
    {
        Apartado = 1,
        Pagado = 2,
        Liberado = 3
    }

    public class CampoTipoSelect
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public override string ToString() => Nombre;
    }

    public class PremioPorSorteo
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        public int IdSorteoPremio { get; set; }
        public int IdSorteo { get; set; }
        public int IdPremio { get; set; }
        public int Lugar { get; set; }
        public string DescripcionPremio { get; set; } = string.Empty;
        public string DescripcionLarga { get; set; } = string.Empty;

        public string ValorFormateado
        {
            get
            {
                if (decimal.TryParse(DescripcionPremio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valInv))
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
        public string LugarTexto => $"{Lugar}º Lugar";
        public string DescripcionCompleta => $"{Lugar}º Lugar - {ValorFormateado}";
        public int? NumeroGanador { get; set; }
        public string? NombreGanador { get; set; }
        public bool TieneGanador => NumeroGanador.HasValue && NumeroGanador.Value > 0;
        public string LeyendaGanador => TieneGanador
            ? $"¡GANADOR! Boleto #{NumeroGanador.GetValueOrDefault():D2} — {NombreGanador}"
            : string.Empty;
    }

    public class AsignacionClienteSorteo
    {
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public List<int> NumerosAsignados { get; set; } = [];
        public bool EsComboCompleto { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    [Table("EstatusSorteo")]
    public class EstatusSorteoItem
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    [Table("EstatusApartado")]
    public class EstatusApartadoItem
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class DetalleNumeroGanadorDto
    {
        public int? ReservaId { get; set; }
        public string? NombreCliente { get; set; }
        public string? Telefono { get; set; }
        public int IdEstatus { get; set; }
    }
}


