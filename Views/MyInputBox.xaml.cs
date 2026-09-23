using Caupo.Helpers;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.Views
{
    public partial class MyInputBox : Window
    {
        private readonly VirtualKeyboard keyboard;

        public bool NumbersOnly { get; set; } = false;

        public string result;


        public MyInputBox()
        {
            InitializeComponent();

            DataContext = this;

            keyboard = new VirtualKeyboard();
            KeyboardHost.Content = keyboard;

            VirtualKeyboardManager.EnterPressed += VirtualKeyboard_EnterPressed;

            Closed += MyInputBox_Closed;
        }

        private void InputText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            OKButton_Click(sender, e);
        }

        private void VirtualKeyboard_EnterPressed()
        {
            if (!IsActive)
                return;

            OKButton_Click(null, null);
        }


        private void MyInputBox_Closed(object? sender, EventArgs e)
        {
            VirtualKeyboardManager.EnterPressed -= VirtualKeyboard_EnterPressed;
        }


        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            textBox.SelectAll();

            KeyboardHost.Visibility = Visibility.Visible;
        }


        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }


        private void InputText_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            textBox.Focus();
            Keyboard.Focus(textBox);
            textBox.SelectAll();

            KeyboardHost.Visibility = Visibility.Visible;
        }


        private void NumberValidationHandler(
            object sender,
            TextCompositionEventArgs e)
        {
            Debug.WriteLine("NumbersOnly = " + NumbersOnly);

            if (sender is not TextBox textBox)
                return;

            string fullText =
                textBox.Text.Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength)
                .Insert(
                    textBox.SelectionStart,
                    e.Text);

            e.Handled =
                !System.Text.RegularExpressions.Regex.IsMatch(
                    fullText,
                    @"^\d*([.,]\d*)?$");
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            result = InputText.Text;
            Close();
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    InputText.Focus();
                    Keyboard.Focus(InputText);
                    InputText.SelectAll();

                }),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}