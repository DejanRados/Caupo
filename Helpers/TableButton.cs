using System.Windows;
using System.Windows.Controls;

namespace Caupo.Helpers
{
    public class TableButton : Button
    {
        public static readonly DependencyProperty TableNameProperty =
            DependencyProperty.Register (
                nameof (TableName),
                typeof (string),
                typeof (TableButton),
                new PropertyMetadata (string.Empty));

        public string TableName
        {
            get => (string)GetValue (TableNameProperty);
            set => SetValue (TableNameProperty, value);
        }

        public string Waiter { get; set; }
        public string WaiterId { get; set; }
        public string Total { get; set; }
    }
}
