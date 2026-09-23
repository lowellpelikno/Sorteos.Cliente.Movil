using System.Buffers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sorteos.Cliente.Movil.Helpers;

/// <summary>
/// Provee metodos de extension y utilidades de alto rendimiento para validacion, limpieza y formateo de texto y cadenas.
/// Diseñado con compatibilidad NativeAOT / iOS y optimizaciones basadas en <see cref="ReadOnlySpan{T}"/> y <see cref="SearchValues{T}"/>.
/// </summary>
public static partial class TextoHelper
{
    private const string LetrasValidas = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZáéíóúÁÉÍÓÚüÜñÑ ";
    private static readonly SearchValues<char> DigitosAscii = SearchValues.Create("0123456789");
    private static readonly SearchValues<char> CaracteresPermitidos = SearchValues.Create(LetrasValidas);

    // Sin RegexOptions.Compiled: 100% compatible con AOT / iOS
    [GeneratedRegex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex FormatoEmailRegex();

    /// <summary>
    /// Remueve espacios redundantes y normaliza espacios simples entre palabras.
    /// </summary>
    /// <param name="input">Cadena de texto por sanear.</param>
    /// <returns>Texto limpio sin espacios superfluos.</returns>
    public static string Clean(this string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : string.Join(" ", input.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Remueve la totalidad de caracteres de espacio en blanco presentes en la cadena sin asignaciones innecesarias en el heap.
    /// </summary>
    /// <param name="input">Cadena de entrada.</param>
    /// <returns>Cadena compacta sin espacios.</returns>
    public static string CleanAllWhiteSpace(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        ReadOnlySpan<char> span = input.AsSpan();
        Span<char> buffer = span.Length <= 128
            ? stackalloc char[span.Length]
            : new char[span.Length];

        int count = 0;
        foreach (char c in span)
        {
            if (!char.IsWhiteSpace(c))
                buffer[count++] = c;
        }

        return count == 0 ? string.Empty : new string(buffer[..count]);
    }

    /// <summary>
    /// Determina si la cadena contiene simbolos o caracteres que no pertenecen al alfabeto latino o vocales con tilde permitidas.
    /// </summary>
    /// <param name="input">Cadena de texto por evaluar.</param>
    /// <returns>Verdadero si contiene caracteres no autorizados; falso en caso contrario.</returns>
    public static bool ContieneCaracteresEspeciales(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return input.AsSpan().IndexOfAnyExcept(CaracteresPermitidos) >= 0;
    }

    /// <summary>
    /// Comprueba si la cadena posee secuencias consecutivas de tres o mas espacios en blanco.
    /// </summary>
    /// <param name="input">Cadena por evaluar.</param>
    /// <returns>Verdadero si localiza tres espacios contiguos.</returns>
    public static bool TieneMasDeDosEspaciosConsecutivos(this string? input)
    {
        return !string.IsNullOrEmpty(input) && input.AsSpan().Contains("   ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Comprueba si la totalidad de caracteres contenidos en la cadena son digitos numericos ASCII.
    /// </summary>
    /// <param name="input">Cadena a evaluar.</param>
    /// <returns>Verdadero si solo contiene digitos; falso en caso contrario.</returns>
    public static bool EsSoloNumeros(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return input.AsSpan().IndexOfAnyExcept(DigitosAscii) < 0;
    }

    /// <summary>
    /// Extrae unicamente los caracteres numericos de la cadena, truncando el resultado a la longitud maxima indicada.
    /// </summary>
    /// <param name="input">Cadena de entrada (ej. telefonos o folios).</param>
    /// <param name="longitudMaxima">Limite maximo de digitos permitidos.</param>
    /// <returns>Subcadena conformada exclusivamente por digitos numericos.</returns>
    public static string SoloDigitos(this string? input, int longitudMaxima = 10)
    {
        if (string.IsNullOrEmpty(input) || longitudMaxima <= 0)
            return string.Empty;

        ReadOnlySpan<char> span = input.AsSpan();
        int capacidad = Math.Min(span.Length, longitudMaxima);

        Span<char> buffer = capacidad <= 128
            ? stackalloc char[capacidad]
            : new char[capacidad];

        int count = 0;
        foreach (char c in span)
        {
            if (char.IsAsciiDigit(c))
            {
                buffer[count++] = c;
                if (count == capacidad)
                    break;
            }
        }

        return count == 0 ? string.Empty : new string(buffer[..count]);
    }

    /// <summary>
    /// Valida si el formato sintactico del correo electronico cumple con los estandares comerciales mediante Regex compilado en origen.
    /// </summary>
    /// <param name="email">Direccion de correo electronico por auditar.</param>
    /// <returns>Verdadero si la estructura es valida; falso en caso contrario.</returns>
    public static bool EsEmailValido(this string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        ReadOnlySpan<char> span = email.AsSpan().Trim();

        if (span.Length is < 6 or > 254)
            return false;

        return FormatoEmailRegex().IsMatch(span);
    }

    /// <summary>
    /// Filtra y conserva unicamente caracteres validos para la captura de montos numericos (digitos, punto o coma).
    /// </summary>
    /// <param name="input">Texto ingresado en el campo de monto.</param>
    /// <param name="longitudMaxima">Longitud maxima del buffer.</param>
    /// <returns>Cadena filtrada apta para conversion monetaria.</returns>
    public static string FiltrarCaracteresMonto(this string? input, int longitudMaxima = 15)
    {
        if (string.IsNullOrEmpty(input) || longitudMaxima <= 0)
            return string.Empty;

        ReadOnlySpan<char> span = input.AsSpan();
        int capacidad = Math.Min(span.Length, longitudMaxima);

        Span<char> buffer = capacidad <= 128
            ? stackalloc char[capacidad]
            : new char[capacidad];

        int count = 0;
        foreach (char c in span)
        {
            if (char.IsAsciiDigit(c) || c is '.' or ',')
            {
                buffer[count++] = c;
                if (count == capacidad)
                    break;
            }
        }

        return count == 0 ? string.Empty : new string(buffer[..count]);
    }

    /// <summary>
    /// Determina si una cadena representa un valor numerico monetario valido y mayor a cero, admitiendo convenciones culturales comunes.
    /// </summary>
    /// <param name="input">Cadena con el importe a evaluar.</param>
    /// <param name="monto">Parametro de salida con el valor decimal parseado.</param>
    /// <returns>Verdadero si el monto es convertible y estrictamente superior a cero.</returns>
    public static bool EsMontoMonetarioValido(this string? input, out decimal monto)
    {
        monto = 0m;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string limpio = input.Trim();

        // Evaluar con estilo Number (admite separadores de miles y punto o coma decimal)
        if (decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out monto) && monto > 0)
            return true;

        if (decimal.TryParse(limpio, NumberStyles.Number, new CultureInfo("es-MX"), out monto) && monto > 0)
            return true;

        return false;
    }
}