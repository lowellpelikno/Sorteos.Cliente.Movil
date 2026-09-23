using System.Globalization;

namespace Sorteos.Cliente.Movil.Converters
{
/// <summary>
    /// Convertidor de valores XAML para formatear fechas <see cref="DateTime"/> utilizando la cultura mexicana (es-MX).
    /// </summary>
    public class FechaCulturaConverter : IValueConverter
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        /// <summary>
        /// Convierte un objeto <see cref="DateTime"/> a su representacion textual segun el patron especificado en el parametro.
        /// </summary>
        /// <param name="value">Fecha a formatear.</param>
        /// <param name="targetType">Tipo de destino esperado.</param>
        /// <param name="parameter">Formato opcional de fecha (por defecto "dd/MM/yyyy").</param>
        /// <param name="culture">Cultura suministrada por el enlace visual.</param>
        /// <returns>Cadena con la fecha formateada en cultura mexicana.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                string formato = parameter as string ?? "dd/MM/yyyy";
                return dt.ToString(formato, CulturaEsMx);
            }
            return string.Empty;
        }

        /// <summary>
        /// Conversion inversa no soportada en este convertidor unidireccional.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

