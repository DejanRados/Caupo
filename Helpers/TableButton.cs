using System.Windows;
using System.Windows.Controls;

namespace Caupo.Helpers
{
    public class TableButton : Button
    {
        public static readonly DependencyProperty TableNameProperty =
            DependencyProperty.Register(
                nameof(TableName),
                typeof(string),
                typeof(TableButton),
                new PropertyMetadata(string.Empty));

        public string TableName
        {
            get => (string)GetValue(TableNameProperty);
            set => SetValue(TableNameProperty, value);
        }

        public static readonly DependencyProperty WaiterProperty =
            DependencyProperty.Register(
                nameof(Waiter),
                typeof(string),
                typeof(TableButton),
                new PropertyMetadata(null));

        public string? Waiter
        {
            get => (string?)GetValue(WaiterProperty);
            set => SetValue(WaiterProperty, value);
        }

        public static readonly DependencyProperty WaiterIdProperty =
            DependencyProperty.Register(
                nameof(WaiterId),
                typeof(string),
                typeof(TableButton),
                new PropertyMetadata(null));

        public string? WaiterId
        {
            get => (string?)GetValue(WaiterIdProperty);
            set => SetValue(WaiterIdProperty, value);
        }

        public static readonly DependencyProperty TotalProperty =
            DependencyProperty.Register(
                nameof(Total),
                typeof(string),
                typeof(TableButton),
                new PropertyMetadata(null));

        public string? Total
        {
            get => (string?)GetValue(TotalProperty);
            set => SetValue(TotalProperty, value);
        }

        public static readonly DependencyProperty IsOccupiedProperty =
    DependencyProperty.Register(
        nameof(IsOccupied),
        typeof(bool),
        typeof(TableButton),
        new PropertyMetadata(false));

        public bool IsOccupied
        {
            get => (bool)GetValue(IsOccupiedProperty);
            set => SetValue(IsOccupiedProperty, value);
        }
    }
}