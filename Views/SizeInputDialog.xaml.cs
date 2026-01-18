using Caupo.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for SizeInputDialog.xaml
    /// </summary>
    public partial class SizeInputDialog : Window
    {
        private VirtualKeyboard keyboard;
        public TextBox? FocusedTextBox = null;
        public SizeInputDialog()
        {
            InitializeComponent ();
            InputTextWidth.Focus ();
            keyboard = new VirtualKeyboard ();
            KeyboardHost.Content = keyboard;
            keyboard.KeyPressed += Keyboard_KeyPressed;
        }


        public void ReceiveKey(string key)
        {

            if(FocusedTextBox != null)
            {
                switch(key)
                {
                    case "\uE72B":
                        if(FocusedTextBox.Text.Length > 0)
                        {
                            int pos = FocusedTextBox.SelectionStart;
                            if(pos > 0)
                            {
                                FocusedTextBox.Text =
                                    FocusedTextBox.Text.Remove (pos - 1, 1);
                                FocusedTextBox.SelectionStart = pos - 1;
                            }
                        }
                        break;
                    case "\uE75D":
                        InsertIntoFocused ("\u0020");
                        break;
                    case "Sakrij":
                        //FocusedTextBox = null;
                        KeyboardHost.Visibility = Visibility.Collapsed;
                        break;
                    case "Enter":
                        //FocusedTextBox = null;
                        OKButton_Click (null, null);
                        break;
                    case "Reset":
                        FocusedTextBox.Text = "";
                        break;
                    default:
                        InsertIntoFocused (key);
                        break;
                }
                return;
            }
        }
        private void InsertIntoFocused(string text)
        {
            if(FocusedTextBox == null)
                return;
            int pos = FocusedTextBox.SelectionStart;
            FocusedTextBox.Text = FocusedTextBox.Text.Insert (pos, text);
            FocusedTextBox.SelectionStart = pos + text.Length;
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FocusedTextBox = sender as TextBox;
            FocusedTextBox.Clear ();
            FocusedTextBox.SelectAll ();
            KeyboardHost.Visibility = Visibility.Visible;

        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {

            FocusedTextBox = null;
        }
        private void Keyboard_KeyPressed(string key)
        {
            if(Application.Current.Windows.OfType<SizeInputDialog> ().FirstOrDefault (w => w.IsActive) is SizeInputDialog inputBox)
            {
                if(inputBox.FocusedTextBox != null)
                {
                    inputBox.ReceiveKey (key);
                    return;
                }
            }
            if(this.Content is IKeyboardInputReceiver receiver)
                receiver.ReceiveKey (key);
        }
        private void InputText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusedTextBox = sender as TextBox;
            FocusedTextBox.Clear ();
            FocusedTextBox.SelectAll ();
            KeyboardHost.Visibility = Visibility.Visible;
            //MainWindow.Instance.ShowKeyboard ();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            InputTextWidth.Text = string.Empty;
            InputTextHeight.Text = string.Empty;
            this.Close ();
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string inputTextWidth = InputTextWidth.Text;
            string inputTextHeight = InputTextHeight.Text;

            if(decimal.TryParse (inputTextWidth, out decimal resultWidth) && decimal.TryParse (inputTextHeight, out decimal resultHeight))
            {
                if(Convert.ToDecimal (inputTextWidth) > 49 && Convert.ToDecimal (inputTextHeight) > 49)
                {
                    this.DialogResult = true;
                    this.Close ();
                }
                else
                {
                    MessageBox.Show ("Širina ili visina ne mogu biti manje od 50");
                    InputTextWidth.Text = string.Empty;
                    InputTextHeight.Text = string.Empty;
                    InputTextWidth.Focus ();
                    return;
                }
            }
            else
            {
                MessageBox.Show ("Unijeli ste vrijednost koja nije validna za širinu ili visinu");
                InputTextWidth.Text = string.Empty;
                InputTextHeight.Text = string.Empty;
                InputTextWidth.Focus ();
                return;
            }


        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke (new Action (() =>
            {

                InputTextWidth.Focus ();
                Keyboard.Focus (InputTextWidth);
                InputTextWidth.SelectAll ();

            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

        }
    }
}
