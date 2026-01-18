using System.Globalization;
using System.Windows.Data;

namespace Caupo.Helpers
{
    public class CategoryToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString () == parameter?.ToString ();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? parameter : Binding.DoNothing;
    }

}
