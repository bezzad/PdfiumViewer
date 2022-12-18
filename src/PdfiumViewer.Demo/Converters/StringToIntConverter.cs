using System;
using System.Globalization;
using System.Windows.Data;

namespace PdfiumViewer.Demo.Converters
{
    public class StringToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value.ToString(), out int number))
            {
                return number;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? "0";
        }
    }
}
