using System.Buffers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sorteos.Cliente.Movil.Helpers;

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

    public static string Clean(this string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : string.Join(" ", input.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

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

    public static bool ContieneCaracteresEspeciales(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return input.AsSpan().IndexOfAnyExcept(CaracteresPermitidos) >= 0;
    }

    public static bool TieneMasDeDosEspaciosConsecutivos(this string? input)
    {
        return !string.IsNullOrEmpty(input) && input.AsSpan().Contains("   ", StringComparison.Ordinal);
    }

    public static bool EsSoloNumeros(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return input.AsSpan().IndexOfAnyExcept(DigitosAscii) < 0;
    }

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

    public static bool EsEmailValido(this string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        ReadOnlySpan<char> span = email.AsSpan().Trim();

        if (span.Length is < 6 or > 254)
            return false;

        return FormatoEmailRegex().IsMatch(span);
    }

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