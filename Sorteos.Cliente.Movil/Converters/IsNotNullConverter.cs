using System.Globalization;

namespace Sorteos.Cliente.Movil.Converters
{
/// <summary>
    /// Convertidor booleano de enlace XAML que evalua si un objeto o cadena de texto no es nula ni vacia.
    /// </summary>
    public class IsNotNullConverter : IValueConverter
    {
        /// <summary>
        /// Evalua si el valor proporcionado es distinto de nulo (y no es cadena vacia en caso de texto).
        /// </summary>
        /// <param name="value">Valor a evaluar.</param>
        /// <param name="targetType">Tipo booleano de destino.</param>
        /// <param name="parameter">Parametro opcional.</param>
        /// <param name="culture">Cultura del enlace.</param>
        /// <returns>Verdadero si el valor no es nulo ni vacio; falso en caso contrario.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return !string.IsNullOrWhiteSpace(s);
            }
            return value != null;
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

