using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
//using Windows.Services.Maps;

namespace IG.Lib
{

    public class NumberToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double ret = 0;
            if (double.TryParse(value as string, out ret))
                return ret;
            else
                throw new ArgumentException("String argumnt cannnot be converted to number", "nameof(value)");
        }
    }
}