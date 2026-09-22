using System.Globalization;

namespace Sorteos.Cliente.Movil.Converters
{
    public class FechaCulturaConverter : IValueConverter
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                string formato = parameter as string ?? "dd/MM/yyyy";
                return dt.ToString(formato, CulturaEsMx);
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

