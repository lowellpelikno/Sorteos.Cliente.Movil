using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que representa una transaccion o solicitud de apartado de boletos para un sorteo.
    /// Agrupa uno o multiples numeros bajo una misma operacion de cobro y registro de comprobante.
    /// </summary>
    [Table("NumeroReservado")]
    public class NumeroReservado
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        /// <summary>
        /// Identificador primario y autoincrementable de la reserva.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identificador del sorteo al que pertenece la reserva.
        /// </summary>
        [Indexed]
        public int SorteoId { get; set; }

        /// <summary>
        /// Identificador del cliente que solicito la reserva (o 0 si es cliente eventual).
        /// </summary>
        [Indexed]
        public int ClienteId { get; set; }

        /// <summary>
        /// Identificador del estatus de la reserva (1 = Apartado, 2 = Pagado, 3 = Liberado).
        /// </summary>
        [Indexed]
        public int IdEstatus { get; set; } = 1; // 1 = Apartado, 2 = Pagado, 3 = Liberado

        /// <summary>
        /// Nombre personalizado del titular o comprador registrado para la reserva.
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Monto economico convenido para la totalidad de boletos de esta reserva.
        /// </summary>
        public decimal Costo { get; set; }

        /// <summary>
        /// Medio de pago registrado (Efectivo, Transferencia, Tarjeta, etc.).
        /// </summary>
        public string FormaDePago { get; set; } = "Efectivo";

        /// <summary>
        /// Ruta al archivo digital de comprobante adjunto en el almacenamiento local.
        /// </summary>
        public string ComprobanteUrl { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora en la que se creo la reserva.
        /// </summary>
        public DateTime FechaApartado { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si la reserva sigue vigente y activa en el sistema.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Indica si la reserva se encuentra liquidada en su totalidad.
        /// </summary>
        [Ignore]
        public bool EstaPagado => IdEstatus == 2;

        /// <summary>
        /// Monto economico formateado como moneda mexicana ($#,##0.00).
        /// </summary>
        [Ignore]
        public string CostoFormateado => Costo.ToString("C2", CulturaEsMx);

        /// <summary>
        /// Descripcion en texto del estatus actual de la reserva.
        /// </summary>
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

