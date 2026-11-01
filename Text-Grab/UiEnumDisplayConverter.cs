using System;
using System.Globalization;
using System.Windows.Data;

namespace Text_Grab;

// Translate only the item presentation. SelectedItem remains the original enum value.
public sealed class UiEnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        UiAutomationTraversalMode.Fast => "快速",
        UiAutomationTraversalMode.Balanced => "平衡",
        UiAutomationTraversalMode.Thorough => "完整",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
