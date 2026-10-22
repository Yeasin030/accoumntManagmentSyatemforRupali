using System.Globalization;
using AccountManagement.Domain.Enums;

namespace AccountManagement.App.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string param = parameter?.ToString()?.ToLowerInvariant() ?? "bg";
        bool isCancelled = false;
        TransactionStatus status = TransactionStatus.Active;

        if (value is TransactionStatus s)
        {
            status = s;
            isCancelled = s == TransactionStatus.Cancelled;
        }
        else if (value is string str)
        {
            if (Enum.TryParse<TransactionStatus>(str, true, out var parsed))
            {
                status = parsed;
                isCancelled = parsed == TransactionStatus.Cancelled;
            }
        }
        else if (value is bool b)
        {
            isCancelled = b;
        }

        if (isCancelled)
        {
            return param switch
            {
                "bg" => Color.FromArgb("#FEE2E2"),      // Soft red background
                "text" => Color.FromArgb("#DC2626"),    // Crisp red text
                "border" => Color.FromArgb("#FECACA"),  // Subtle red stroke
                _ => Color.FromArgb("#FEE2E2")
            };
        }

        return param switch
        {
            "bg" => Color.FromArgb("#D1FAE5"),      // Soft emerald background
            "text" => Color.FromArgb("#065F46"),    // Crisp emerald text
            "border" => Color.FromArgb("#A7F3D0"),  // Subtle emerald stroke
            _ => Color.FromArgb("#D1FAE5")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
