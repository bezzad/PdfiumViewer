using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace PdfiumViewer.Demo.Converters
{
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int number)
            {
                if (parameter is string operate)
                {
                    var expr = number + operate;
                    return int.Parse(new DataTable().Compute(expr, null).ToString());
                }
                return number;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
