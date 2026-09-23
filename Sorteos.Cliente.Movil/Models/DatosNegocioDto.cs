namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// DTO que encapsula los datos comerciales, bancarios y de contacto del organizador para las fichas de pago de boletos.
    /// </summary>
    public class DatosNegocioDto
    {
        /// <summary>
        /// Razon social o nombre comercial del organizador del sorteo.
        /// </summary>
        public string NombreNegocio { get; set; } = string.Empty;

        /// <summary>
        /// Numero de telefono o WhatsApp para atencion a clientes y recepcion de comprobantes.
        /// </summary>
        public string TelefonoContacto { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la institucion bancaria donde se reciben las transferencias o depositos.
        /// </summary>
        public string BancoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Clave Bancaria Estandarizada (CLABE interbancaria) para transferencias SPEI.
        /// </summary>
        public string CuentaClabe { get; set; } = string.Empty;

        /// <summary>
        /// Numero de tarjeta de debito para depositos en tiendas de conveniencia.
        /// </summary>
        public string NumeroTarjeta { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del titular registrado de la cuenta bancaria.
        /// </summary>
        public string TitularCuenta { get; set; } = string.Empty;

        /// <summary>
        /// Notas, condiciones o politicas de tolerancia para la vigencia de los apartados.
        /// </summary>
        public string NotasApartado { get; set; } = string.Empty;
    }
}

