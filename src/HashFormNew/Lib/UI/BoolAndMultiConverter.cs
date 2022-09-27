﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IG.UI
{

    /// <summary>Takes multiple boolean values and returns true if all of them are true, false if any of them are false.</summary>
    public class BoolAndMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || !targetType.IsAssignableFrom(typeof(bool)))
            {
                return false;
            }

            foreach (var value in values)
            {
                if (!(value is bool boolValue))
                {
                    return false;
                }
                else if (!boolValue)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolValue) || targetTypes.Any(type => !type.IsAssignableFrom(typeof(bool))))
            {
                return null;  // backward conversion not possible
            }
            if (boolValue)
            {
                return targetTypes.Select(type => (object) true).ToArray();
            }
            else
            {
                return targetTypes.Select(type => (object) false).ToArray();
            }
        }
    }

}
