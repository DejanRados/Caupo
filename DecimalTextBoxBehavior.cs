using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo
{

    public static class DecimalTextBoxBehavior
    {
        public static readonly DependencyProperty EnableDecimalParsingProperty =
            DependencyProperty.RegisterAttached (
                "EnableDecimalParsing",
                typeof (bool),
                typeof (DecimalTextBoxBehavior),
                new PropertyMetadata (false, OnEnableDecimalParsingChanged));

        public static void SetEnableDecimalParsing(DependencyObject element, bool value)
            => element.SetValue (EnableDecimalParsingProperty, value);

        public static bool GetEnableDecimalParsing(DependencyObject element)
            => (bool)element.GetValue (EnableDecimalParsingProperty);

        private static void OnEnableDecimalParsingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is TextBox tb)
            {
                if((bool)e.NewValue)
                {
                    tb.PreviewTextInput += Tb_PreviewTextInput;
                    DataObject.AddPastingHandler (tb, OnPaste);
                }
                else
                {
                    tb.PreviewTextInput -= Tb_PreviewTextInput;
                    DataObject.RemovePastingHandler (tb, OnPaste);
                }
            }
        }

        private static void Tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextValidDecimal (e.Text);
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if(e.DataObject.GetDataPresent (DataFormats.Text))
            {
                string text = e.DataObject.GetData (DataFormats.Text) as string;
                if(!IsTextValidDecimal (text))
                    e.CancelCommand ();
            }
            else
            {
                e.CancelCommand ();
            }
        }

        private static bool IsTextValidDecimal(string text)
        {
            // Zameni zarez sa tačkom
            text = text.Replace (',', '.');

            // Proveri da li je validan decimal
            return decimal.TryParse (text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
        }
    }

}
