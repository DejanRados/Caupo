using Caupo.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.Views
{
    public partial class SizeInputDialog : Window
    {
        private readonly VirtualKeyboard keyboard;


        public SizeInputDialog()
        {
            InitializeComponent();

            keyboard = new VirtualKeyboard();
            KeyboardHost.Content = keyboard;

            VirtualKeyboardManager.EnterPressed += VirtualKeyboard_EnterPressed;

            Closed += SizeInputDialog_Closed;
        }


        private void VirtualKeyboard_EnterPressed()
        {
            if (!IsActive)
                return;

            OKButton_Click(null, null);
        }


        private void SizeInputDialog_Closed(object? sender, EventArgs e)
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


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            InputTextWidth.Text = string.Empty;
            InputTextHeight.Text = string.Empty;

            Close();
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string inputTextWidth = InputTextWidth.Text;
            string inputTextHeight = InputTextHeight.Text;

            if (decimal.TryParse(inputTextWidth, out decimal resultWidth) &&
               decimal.TryParse(inputTextHeight, out decimal resultHeight))
            {
                if (resultWidth > 49 && resultHeight > 49)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        "Širina ili visina ne mogu biti manje od 50");

                    InputTextWidth.Text = string.Empty;
                    InputTextHeight.Text = string.Empty;

                    InputTextWidth.Focus();
                    return;
                }
            }
            else
            {
                MessageBox.Show(
                    "Unijeli ste vrijednost koja nije validna za širinu ili visinu");

                InputTextWidth.Text = string.Empty;
                InputTextHeight.Text = string.Empty;

                InputTextWidth.Focus();
                return;
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    InputTextWidth.Focus();
                    Keyboard.Focus(InputTextWidth);
                    InputTextWidth.SelectAll();

                }),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}