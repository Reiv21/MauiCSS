using System.Globalization;

namespace MauiCSS.Converters;

public class BoolToEditTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEditing)
        {
            return isEditing ? "Zakończ edycję" : "Edytuj listę";
        }
        return "Edytuj listę";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // not needed but must be implemented for interafe
        throw new NotImplementedException();
    }
}
// used for IsEditing button
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not bool boolValue || !boolValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}
