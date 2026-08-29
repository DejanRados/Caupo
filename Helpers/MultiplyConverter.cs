using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Caupo.Helpers
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if(value is not double number)
                return DependencyProperty.UnsetValue;


            if(!double.TryParse (
                parameter?.ToString (),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double factor))
            {
                return DependencyProperty.UnsetValue;
            }


            double result =
                number * factor;


            /*
             * Tokom prvog WPF layout prolaza
             * ActualHeight može biti 0.
             *
             * FontSize ne prihvata 0.
             */

            if(targetType == typeof (double) &&
               result <= 0)
            {
                return DependencyProperty.UnsetValue;
            }


            return result;
        }


        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException ();
        }
    }
}