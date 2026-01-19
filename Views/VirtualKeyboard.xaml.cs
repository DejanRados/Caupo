using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for VirtualKeyboard.xaml
    /// </summary>
    public partial class VirtualKeyboard : UserControl
    {
        private bool isUppercase = false;
        public VirtualKeyboard()
        {
            InitializeComponent ();
        }

        private void ShiftButton_Click(object sender, RoutedEventArgs e)
        {
            isUppercase = !isUppercase;
            if(sender is Button btn)
                btn.Content = isUppercase ? "\uE84B" : "\uE84A";
            UpdateKeyboardCase ();
        }

        private void UpdateKeyboardCase()
        {
            foreach(var child in KeyboardPanel.Children)
            {
                if(child is UniformGrid grid)
                {
                    foreach(var element in grid.Children)
                    {
                        if(element is Button btn)
                        {
                            // Preskoči specijalne tastere
                            string txt = btn.Content?.ToString ();

                            if(string.IsNullOrWhiteSpace (txt))
                                continue;
                            if(txt.Length != 1)
                                continue;      // Enter, Š, Đ, itd. ostaju isti
                            if(!char.IsLetter (txt[0]))
                                continue;

                            btn.Content = isUppercase ? txt.ToUpper () : txt.ToLower ();
                        }
                    }
                }
            }
        }

        // VirtualKeyboard.xaml.cs
        public event Action<string> KeyPressed;

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn)
            {

                string letter = btn.Content.ToString ();
              

                if(!isUppercase)
                {

                }
                else
                {

                    isUppercase = true;
                }
                KeyPressed?.Invoke (letter);
            }
        }

    }
}
