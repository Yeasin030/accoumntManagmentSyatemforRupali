using System.Globalization;
using System.Text;

namespace AccountManagement.Domain.Formatting;

public static class CurrencyFormatter
{
    private static readonly CultureInfo BdEnglishCulture = new CultureInfo("en-BD");
    private static readonly char[] BanglaDigits = { '০', '১', '২', '৩', '৪', '৫', '৬', '৭', '৮', '৯' };

    /// <summary>
    /// Formats decimal amount into standard Bangladeshi Taka currency format (e.g. ৳ 1,25,000.00).
    /// </summary>
    public static string FormatBdt(decimal amount, bool includeSymbol = true)
    {
        // Bangladeshi numbering format grouping: 3 digits initially, then groups of 2 (e.g., 1,23,456.78)
        string formattedNumber = FormatBangladeshiNumber(amount);
        return includeSymbol ? $"৳ {formattedNumber}" : formattedNumber;
    }

    /// <summary>
    /// Formats decimal amount using Bengali numerals (e.g. ৳ ১,২৫,০০০.০০).
    /// </summary>
    public static string FormatBanglaBdt(decimal amount, bool includeSymbol = true)
    {
        string englishFormatted = FormatBangladeshiNumber(amount);
        string banglaFormatted = ConvertToBanglaDigits(englishFormatted);
        return includeSymbol ? $"৳ {banglaFormatted}" : banglaFormatted;
    }

    /// <summary>
    /// Converts ASCII numeric characters in a string to Bangla digits.
    /// </summary>
    public static string ConvertToBanglaDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (c >= '0' && c <= '9')
            {
                sb.Append(BanglaDigits[c - '0']);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts Bangla numeral string to English ASCII numeric string for parsing.
    /// </summary>
    public static string ConvertToEnglishDigits(string banglaText, bool stripCommas = true)
    {
        if (string.IsNullOrEmpty(banglaText)) return string.Empty;

        var sb = new StringBuilder(banglaText.Length);
        foreach (char c in banglaText)
        {
            int index = Array.IndexOf(BanglaDigits, c);
            if (index >= 0)
            {
                sb.Append(index);
            }
            else if (c == '.' || (c >= '0' && c <= '9') || (!stripCommas && c == ','))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string FormatBangladeshiNumber(decimal amount)
    {
        bool isNegative = amount < 0;
        decimal absAmount = Math.Abs(amount);

        long integerPart = (long)Math.Truncate(absAmount);
        int decimalPart = (int)Math.Round((absAmount - integerPart) * 100);

        string intStr = integerPart.ToString();
        string formattedInt;

        if (intStr.Length <= 3)
        {
            formattedInt = intStr;
        }
        else
        {
            // Last 3 digits
            string lastThree = intStr.Substring(intStr.Length - 3);
            string remaining = intStr.Substring(0, intStr.Length - 3);

            // Group by 2 from right to left
            var groups = new List<string>();
            while (remaining.Length > 2)
            {
                groups.Insert(0, remaining.Substring(remaining.Length - 2));
                remaining = remaining.Substring(0, remaining.Length - 2);
            }
            if (remaining.Length > 0)
            {
                groups.Insert(0, remaining);
            }

            formattedInt = string.Join(",", groups) + "," + lastThree;
        }

        string result = $"{formattedInt}.{decimalPart:D2}";
        return isNegative ? $"-{result}" : result;
    }
}
