using Caupo.Helpers;
using Caupo.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml
    /// </summary>
    public partial class MyInputBox : Window
    {
        private VirtualKeyboard keyboard;
        public TextBox? FocusedTextBox = null;
        public bool NumbersOnly { get; set; } = false;
        public string result;
        public MyInputBox()
        {
            InitializeComponent();
            this.DataContext = this;
            keyboard = new VirtualKeyboard ();
            KeyboardHost.Content = keyboard;
            keyboard.KeyPressed += Keyboard_KeyPressed;


         
        }
      
        public void ReceiveKey(string key)
        {
          
            if (FocusedTextBox != null)
            {
                switch (key)
                {
                    case "\uE72B":
                        if (FocusedTextBox.Text.Length > 0)
                        {
                            int pos = FocusedTextBox.SelectionStart;
                            if (pos > 0)
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
                        OKButton_Click(null, null);
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
            if (FocusedTextBox == null) return;
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
            if (Application.Current.Windows.OfType<MyInputBox> ().FirstOrDefault (w => w.IsActive) is MyInputBox inputBox)
            {
                if (inputBox.FocusedTextBox != null)
                {
                    inputBox.ReceiveKey (key);
                    return;
                }
            }
            if (this.Content is IKeyboardInputReceiver receiver)
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
    
        private void NumberValidationHandler(object sender, TextCompositionEventArgs e)
        {
            Debug.WriteLine("NumbersOnly = " + NumbersOnly);
            var textBox = sender as TextBox;
            string fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // dozvoljen broj sa decimalnom točkom ili zarezom
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(
                fullText,
                @"^\d*([.,]\d*)?$"
            );
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            result = InputText.Text;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
           this.DialogResult = false;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
          

            Dispatcher.BeginInvoke(new Action(() =>
            {

                 InputText.Focus();
                 Keyboard.Focus(InputText);
                InputText.SelectAll();
              
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

    }
}
