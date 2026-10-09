using System.Text;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;

/// <summary>
///     Reads the file name of a <c>Content-Disposition</c> header (RFC 6266): the RFC 5987 extended
///     <c>filename*=charset'lang'pct-encoded</c> first, then the plain (quoted or token) <c>filename</c>.
///     Tolerant on purpose — a header the framework parser rejects still yields its name here.
/// </summary>
internal static class ContentDispositionParser
{
    /// <summary>
    ///     Returns the file name of the header, or <c>null</c> when it carries none.
    /// </summary>
    internal static string? GetFileName(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        string? plain = null;
        string? extended = null;
        foreach (var (name, value) in Parameters(header!))
        {
            if (string.Equals(name, "filename*", StringComparison.OrdinalIgnoreCase) && extended == null)
            {
                extended = DecodeExtended(value);
            }
            else if (string.Equals(name, "filename", StringComparison.OrdinalIgnoreCase) && plain == null)
            {
                plain = value;
            }
        }

        var result = !string.IsNullOrEmpty(extended) ? extended : plain;
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static IEnumerable<(string Name, string Value)> Parameters(string header)
    {
        var i = 0;
        // Skip the disposition type ("attachment" / "inline").
        while (i < header.Length && header[i] != ';')
        {
            i++;
        }

        while (i < header.Length)
        {
            i++; // ';'
            while (i < header.Length && char.IsWhiteSpace(header[i]))
            {
                i++;
            }

            var nameStart = i;
            while (i < header.Length && header[i] != '=' && header[i] != ';')
            {
                i++;
            }

            var name = header.Substring(nameStart, i - nameStart).Trim();
            if (i >= header.Length || header[i] == ';')
            {
                continue; // parameter without value
            }

            i++; // '='
            while (i < header.Length && char.IsWhiteSpace(header[i]))
            {
                i++;
            }

            string value;
            if (i < header.Length && header[i] == '"')
            {
                i++;
                var builder = new StringBuilder();
                while (i < header.Length && header[i] != '"')
                {
                    if (header[i] == '\\' && i + 1 < header.Length)
                    {
                        i++;
                    }

                    builder.Append(header[i]);
                    i++;
                }

                i++; // closing quote
                value = builder.ToString();
                while (i < header.Length && header[i] != ';')
                {
                    i++;
                }
            }
            else
            {
                var valueStart = i;
                while (i < header.Length && header[i] != ';')
                {
                    i++;
                }

                value = header.Substring(valueStart, i - valueStart).Trim();
            }

            if (name.Length > 0)
            {
                yield return (name, value);
            }
        }
    }

    /// <summary>
    ///     Decodes <c>charset'lang'pct-encoded</c>; UTF-8 and ISO-8859-1 charsets, anything else as UTF-8.
    ///     Returns null when the value is malformed, so the plain <c>filename</c> is used instead.
    /// </summary>
    private static string? DecodeExtended(string value)
    {
        var first = value.IndexOf('\'');
        var second = first < 0 ? -1 : value.IndexOf('\'', first + 1);
        if (first < 0 || second < 0)
        {
            return null;
        }

        var charset = value.Substring(0, first);
        var encoded = value.Substring(second + 1);
        var encoding = string.Equals(charset, "ISO-8859-1", StringComparison.OrdinalIgnoreCase)
            ? Encoding.Latin1
            : Encoding.UTF8;

        var bytes = new List<byte>(encoded.Length);
        for (var i = 0; i < encoded.Length; i++)
        {
            var c = encoded[i];
            if (c == '%')
            {
                if (i + 2 >= encoded.Length || !IsHex(encoded[i + 1]) || !IsHex(encoded[i + 2]))
                {
                    return null;
                }

                bytes.Add(Convert.ToByte(encoded.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
            {
                bytes.AddRange(encoding.GetBytes(c.ToString()));
            }
        }

        var raw = bytes.ToArray();
        if (!ReferenceEquals(encoding, Encoding.UTF8))
        {
            return encoding.GetString(raw);
        }

        try
        {
            return new UTF8Encoding(false, true).GetString(raw);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static bool IsHex(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
