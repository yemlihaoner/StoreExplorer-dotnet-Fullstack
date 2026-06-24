using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace StoreExplorer.Converters;

public sealed class RatingStarConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryGetRating(value, out var rating) || !TryGetThreshold(parameter, out var threshold))
        {
            return targetType == typeof(Color) ? Colors.Gray : "☆";
        }

        var isSelected = rating >= threshold;
        if (targetType == typeof(Color))
        {
            return isSelected ? Color.FromArgb("#F4B400") : Color.FromArgb("#9AA0A6");
        }

        return isSelected ? "★" : "☆";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static bool TryGetRating(object? value, out int rating)
    {
        switch (value)
        {
            case int intRating:
                rating = intRating;
                return true;
            case string stringRating when int.TryParse(stringRating, out var parsedRating):
                rating = parsedRating;
                return true;
            default:
                rating = 0;
                return false;
        }
    }

    private static bool TryGetThreshold(object? parameter, out int threshold)
    {
        switch (parameter)
        {
            case int intThreshold:
                threshold = intThreshold;
                return true;
            case string stringThreshold when int.TryParse(stringThreshold, out var parsedThreshold):
                threshold = parsedThreshold;
                return true;
            default:
                threshold = 0;
                return false;
        }
    }
}