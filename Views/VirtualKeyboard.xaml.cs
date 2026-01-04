using Caupo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for VirtualKeyboard.xaml
    /// </summary>
    public partial class VirtualKeyboard : UserControl
    {
        private bool isUppercase = true;
        public VirtualKeyboard()
        {
            InitializeComponent ();
        }

        private void ShiftButton_Click(object sender, RoutedEventArgs e)
        {
            isUppercase = !isUppercase;
            if (sender is Button btn)
                btn.Content = isUppercase ? "\uE84A" : "\uE84B";
            UpdateKeyboardCase ();
        }

        private void UpdateKeyboardCase()
        {
            foreach (var child in KeyboardPanel.Children)
            {
                if (child is UniformGrid grid)
                {
                    foreach (var element in grid.Children)
                    {
                        if (element is Button btn)
                        {
                            // Preskoči specijalne tastere
                            string txt = btn.Content?.ToString ();

                            if (string.IsNullOrWhiteSpace (txt)) continue;
                            if (txt.Length != 1) continue;      // Enter, Š, Đ, itd. ostaju isti
                            if (!char.IsLetter (txt[0])) continue;

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
            if (sender is Button btn)
            {
                string letter = btn.Content.ToString ();
                if (!isUppercase)
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
