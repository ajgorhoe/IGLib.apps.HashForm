using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IG.UI
{
    public class MainIndicatorLightMultiConverter : IMultiValueConverter
    {

        public Color InsufficientDataColor { get; set; } = Colors.OrangeRed;
        public Color SufficientDataColor { get; set; } = Colors.DarkGreen;
        public Color CalculatingColor { get; set; } = Colors.MediumSlateBlue;
        public Color CalculatedColor { get; set; } = Colors.LimeGreen;


        public Color GetIndicatorColor(bool? isSufficientData = null, bool? isCalculating = null, bool? isCalculated = false)
        {
            Color ret = InsufficientDataColor;
            if (isSufficientData.HasValue)
            {
                if (!isSufficientData.Value)
                {
                    return InsufficientDataColor;
                } else
                {
                    ret = SufficientDataColor;
                }
            }
            if (isCalculating.HasValue && isCalculating.Value)
            {
                ret = CalculatingColor;
            }
            else
            {
                if (isCalculated.HasValue && isCalculated.Value)
                {
                    ret = CalculatedColor;
                }
            }
            return ret;
        }


        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Color ret = InsufficientDataColor;
            bool? isSufficientData = null;
            bool? isCalculating = null;
            bool? isCalculated = null;
            if (values == null || values.Length < 1 || !targetType.IsAssignableFrom(typeof(Color)))
            {
                return ret;
            }
            if (values.Length > 0)
            {
                try
                {
                    if (values[0] != null)
                        isSufficientData = (bool)values[0];
                }
                catch { }
                if (values.Length > 1)
                {
                    try
                    {
                        if (values[1] != null)
                            isCalculating = (bool)values[1];
                    }
                    catch { }
                    if (values.Length > 2)
                    {
                        try
                        {
                            if (values[2] != null)
                                isCalculated = (bool)values[2];
                        }
                        catch { }
                    }
                }
            }
            return GetIndicatorColor(isSufficientData, isCalculating, isCalculated);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;  // Indicate that backward conversion is not possible
        }
    }
}
