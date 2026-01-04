using System;
using System.Collections.Generic;
using System.Globalization;

using System.Windows.Data;

namespace Caupo.Helpers
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double num = System.Convert.ToDouble (value);
            double factor = System.Convert.ToDouble (parameter, CultureInfo.InvariantCulture);
            return num * factor; // npr. 40% visine
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException ();
        }
    }

}
