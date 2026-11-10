namespace FlowDesk.Domain.Common;

public static class TextRules
{
    public const int NameMaxLength = 200;

    public static string Normalize(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string text = value
            .Replace('ي', 'ی')
            .Replace('ك', 'ک');

        // Keep this list aligned with the normalization migration.
        char[] whitespaceCharacters =
        {
            '\t',
            '\n',
            '\v',
            '\f',
            '\r',
            '\u0085',
            '\u00a0',
            '\u1680',
            '\u2000',
            '\u2001',
            '\u2002',
            '\u2003',
            '\u2004',
            '\u2005',
            '\u2006',
            '\u2007',
            '\u2008',
            '\u2009',
            '\u200a',
            '\u200c',
            '\u2028',
            '\u2029',
            '\u202f',
            '\u205f',
            '\u3000'
        };

        foreach (char character in whitespaceCharacters)
        {
            text = text.Replace(character, ' ');
        }

        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }

        return text.Trim(' ');
    }

    public static string RequiredName(
        string? value,
        string parameterName,
        string label)
    {
        string text = Normalize(value);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                $"{label} را وارد کنید.",
                parameterName);
        }

        if (text.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"{label} حداکثر می‌تواند ۲۰۰ کاراکتر باشد.",
                parameterName);
        }

        return text;
    }
}