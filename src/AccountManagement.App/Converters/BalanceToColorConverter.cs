using System.Globalization;

namespace AccountManagement.App.Converters;

public class BalanceToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string param = parameter?.ToString()?.ToLowerInvariant() ?? "text";
        decimal balance = 0m;

        if (value is decimal d)
        {
            balance = d;
        }
        else if (value is string s && decimal.TryParse(s, NumberStyles.Any, culture, out var parsed))
        {
            balance = parsed;
        }

        if (balance > 0)
        {
            return param switch
            {
                "bg" => Color.FromArgb("#FEF3C7"),      // Soft amber
                "text" => Color.FromArgb("#D97706"),    // Amber text
                "border" => Color.FromArgb("#FDE68A"),
                _ => Color.FromArgb("#D97706")
            };
        }
        else if (balance < 0)
        {
            // Customer is in credit (advance paid)
            return param switch
            {
                "bg" => Color.FromArgb("#EFF6FF"),      // Soft blue
                "text" => Color.FromArgb("#2563EB"),    // Blue text
                "border" => Color.FromArgb("#BFDBFE"),
                _ => Color.FromArgb("#2563EB")
            };
        }
        else
        {
            // Zero balance / settled
            return param switch
            {
                "bg" => Color.FromArgb("#ECFDF5"),      // Soft green
                "text" => Color.FromArgb("#059669"),    // Green text
                "border" => Color.FromArgb("#A7F3D0"),
                _ => Color.FromArgb("#059669")
            };
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
