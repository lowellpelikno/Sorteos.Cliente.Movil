using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// DTO y modelo observable utilizado en el flujo interactivo de resolucion y asignacion de ganadores para un premio.
    /// </summary>
    public partial class PremioAsignacionDto : ObservableObject
    {
        /// <summary>
        /// Identificador unico del catalogo de premios.
        /// </summary>
        public int IdPremio { get; set; }

        /// <summary>
        /// Posicion o lugar de premiacion (ej. 1 para 1er lugar).
        /// </summary>
        public int Lugar { get; set; }

        /// <summary>
        /// Texto ordinal del lugar (ej. "1º Lugar").
        /// </summary>
        public string LugarTexto => $"{Lugar}º Lugar";

        /// <summary>
        /// Descripcion comercial o monto del premio.
        /// </summary>
        public string DescripcionPremio { get; set; } = string.Empty;

        /// <summary>
        /// Valor comercial formateado con simbolo de moneda.
        /// </summary>
        public string ValorFormateado { get; set; } = string.Empty;

        /// <summary>
        /// Digito o cadena numerica ingresada manualmente por el operador durante la captura del resultado.
        /// </summary>
        [ObservableProperty]
        public partial string NumeroIngresado { get; set; } = string.Empty;

        /// <summary>
        /// Numero oficial favorecido en el sorteo.
        /// </summary>
        [ObservableProperty]
        public partial int? NumeroGanador { get; set; }

        /// <summary>
        /// Nombre del comprador o titular que tenia apartado o comprado el boleto favorecido.
        /// </summary>
        [ObservableProperty]
        public partial string NombreGanador { get; set; } = string.Empty;

        /// <summary>
        /// Telefono de contacto del ganador.
        /// </summary>
        [ObservableProperty]
        public partial string TelefonoGanador { get; set; } = string.Empty;

        /// <summary>
        /// Identificador de la reserva vinculada al boleto premiado.
        /// </summary>
        [ObservableProperty]
        public partial int? ReservaId { get; set; }

        /// <summary>
        /// Banderin que certifica si el premio ya cuenta con un boleto ganador validado.
        /// </summary>
        [ObservableProperty]
        public partial bool EstaAsignado { get; set; }

        /// <summary>
        /// Indica si el numero favorecido no tenia comprador asignado, declarandose desierto el premio.
        /// </summary>
        [ObservableProperty]
        public partial bool EsDesierto { get; set; }

        /// <summary>
        /// Sintesis en texto del dictamen de premiacion emitido para la interfaz.
        /// </summary>
        [ObservableProperty]
        public partial string ResumenResultado { get; set; } = string.Empty;
    }
}

