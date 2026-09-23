using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Estados posibles para un numero o casilla dentro de la cuadricula de un sorteo.
    /// </summary>
    public enum EstadoNumeroSorteo
    {
        /// <summary>
        /// El numero esta disponible para ser seleccionado o adquirido.
        /// </summary>
        Disponible = 0,

        /// <summary>
        /// El numero se encuentra apartado pendiente de confirmar su liquidacion economica.
        /// </summary>
        Apartado = 1,

        /// <summary>
        /// El numero cuenta con su pago debidamente registrado y validado.
        /// </summary>
        Pagado = 2,

        /// <summary>
        /// El numero resulto ganador de algun premio oficial del sorteo.
        /// </summary>
        Ganador = 3,

        /// <summary>
        /// El numero esta asignado como numero de planta fijo a un cliente preferencial.
        /// </summary>
        Planta = 4
    }

    /// <summary>
    /// Estados del ciclo de vida general de un sorteo comercial.
    /// </summary>
    public enum EstatusSorteo
    {
        /// <summary>
        /// Sorteo creado y habilitado para la asignacion y venta de boletos.
        /// </summary>
        Creado = 1,

        /// <summary>
        /// Sorteo en curso activo de realizacion de juego.
        /// </summary>
        EnJuego = 2,

        /// <summary>
        /// Sorteo finalizado formalmente con la asignacion de ganadores concluida.
        /// </summary>
        Finalizado = 3
    }

    /// <summary>
    /// Estados financieros y administrativos de un boleto o apartado.
    /// </summary>
    public enum EstatusApartado
    {
        /// <summary>
        /// Boleto apartado pendiente de pago.
        /// </summary>
        Apartado = 1,

        /// <summary>
        /// Boleto con pago registrado y verificado.
        /// </summary>
        Pagado = 2,

        /// <summary>
        /// Boleto liberado y reintegrado a disponibilidad comercial.
        /// </summary>
        Liberado = 3
    }

    /// <summary>
    /// Estructura generica para el enlace y despliegue de opciones en controles desplegables (Picker).
    /// </summary>
    public class CampoTipoSelect
    {
        /// <summary>
        /// Identificador de la opcion seleccionable.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Texto representativo de la opcion visible al usuario.
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Devuelve la representacion en texto para controles visuales.
        /// </summary>
        public override string ToString() => Nombre;
    }

    /// <summary>
    /// Representa un premio asignado a un sorteo en un lugar especifico de la jerarquia de premiacion.
    /// </summary>
    public class PremioPorSorteo
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        /// <summary>
        /// Identificador primario de la relacion intermedia entre sorteo y premio.
        /// </summary>
        public int IdSorteoPremio { get; set; }

        /// <summary>
        /// Identificador del sorteo al que pertenece el premio.
        /// </summary>
        public int IdSorteo { get; set; }

        /// <summary>
        /// Identificador del catalogo base de premios.
        /// </summary>
        public int IdPremio { get; set; }

        /// <summary>
        /// Posicion o lugar asignado en la jerarquia de premios (1 para 1er lugar, etc.).
        /// </summary>
        public int Lugar { get; set; }

        /// <summary>
        /// Descripcion o valor comercial del premio asignado.
        /// </summary>
        public string DescripcionPremio { get; set; } = string.Empty;

        /// <summary>
        /// Descripcion extendida o detalle de los bienes incluidos en el premio.
        /// </summary>
        public string DescripcionLarga { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene el valor formateado del premio, parseando cifras monetarias a formato de moneda es-MX.
        /// </summary>
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

        /// <summary>
        /// Texto representativo del lugar ordinal (ej. "1º Lugar").
        /// </summary>
        public string LugarTexto => $"{Lugar}º Lugar";

        /// <summary>
        /// Texto combinado con el lugar y el valor formateado para despliegue en listas.
        /// </summary>
        public string DescripcionCompleta => $"{Lugar}º Lugar - {ValorFormateado}";

        /// <summary>
        /// Numero de boleto que resulto ganador de este premio, si ya fue premiado.
        /// </summary>
        public int? NumeroGanador { get; set; }

        /// <summary>
        /// Nombre del cliente titular del boleto ganador.
        /// </summary>
        public string? NombreGanador { get; set; }

        /// <summary>
        /// Indica si este premio ya cuenta con un boleto ganador asignado.
        /// </summary>
        public bool TieneGanador => NumeroGanador.HasValue && NumeroGanador.Value > 0;

        /// <summary>
        /// Leyenda formateada que celebra al ganador para el despliegue en tarjetas de premiacion.
        /// </summary>
        public string LeyendaGanador => TieneGanador
            ? $"¡GANADOR! Boleto #{NumeroGanador.GetValueOrDefault():D2} — {NombreGanador}"
            : string.Empty;
    }

    /// <summary>
    /// Modelo auxiliar para la transferencia y procesamiento de numeros asignados a un cliente.
    /// </summary>
    public class AsignacionClienteSorteo
    {
        /// <summary>
        /// Identificador del cliente.
        /// </summary>
        public int IdCliente { get; set; }

        /// <summary>
        /// Nombre completo o alias del cliente.
        /// </summary>
        public string NombreCliente { get; set; } = string.Empty;

        /// <summary>
        /// Coleccion de numeros asignados.
        /// </summary>
        public List<int> NumerosAsignados { get; set; } = [];

        /// <summary>
        /// Indica si la asignacion corresponde a un paquete promocional o combo completo.
        /// </summary>
        public bool EsComboCompleto { get; set; }

        /// <summary>
        /// Comentarios u observaciones adicionales de la asignacion.
        /// </summary>
        public string Observaciones { get; set; } = string.Empty;
    }

    /// <summary>
    /// Entidad de catalogo persistida en SQLite para los estatus de un sorteo.
    /// </summary>
    [Table("EstatusSorteo")]
    public class EstatusSorteoItem
    {
        /// <summary>
        /// Identificador unico del estatus.
        /// </summary>
        [PrimaryKey]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del estatus (Creado, En Juego, Finalizado).
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripcion operativa del estatus.
        /// </summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Entidad de catalogo persistida en SQLite para los estatus de un apartado o boleto.
    /// </summary>
    [Table("EstatusApartado")]
    public class EstatusApartadoItem
    {
        /// <summary>
        /// Identificador unico del estatus.
        /// </summary>
        [PrimaryKey]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del estatus (Apartado, Pagado, Liberado).
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripcion operativa del estatus.
        /// </summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO que encapsula los detalles del titular de un numero ganador para validaciones de entrega.
    /// </summary>
    public class DetalleNumeroGanadorDto
    {
        /// <summary>
        /// Identificador de la reserva vinculada al numero ganador.
        /// </summary>
        public int? ReservaId { get; set; }

        /// <summary>
        /// Nombre del cliente o comprador registrado en la reserva.
        /// </summary>
        public string? NombreCliente { get; set; }

        /// <summary>
        /// Telefono de contacto del comprador para notificaciones de premiacion.
        /// </summary>
        public string? Telefono { get; set; }

        /// <summary>
        /// Identificador de estatus de la reserva (ej. Pagado = 2).
        /// </summary>
        public int IdEstatus { get; set; }
    }
}


