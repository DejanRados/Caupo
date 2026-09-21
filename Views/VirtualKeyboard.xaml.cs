using Caupo.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Caupo.Views
{
    public partial class VirtualKeyboard : UserControl
    {
        private bool isUppercase = false;


        public VirtualKeyboard()
        {
            InitializeComponent();
        }


        public event Action<string>? KeyPressed;


        private void ShiftButton_Click(object sender, RoutedEventArgs e)
        {
            isUppercase = !isUppercase;

            if (sender is Button btn)
                btn.Content = isUppercase ? "\uE84B" : "\uE84A";

            UpdateKeyboardCase();
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
                            string? txt = btn.Content?.ToString();

                            if (string.IsNullOrWhiteSpace(txt))
                                continue;

                            if (txt.Length != 1)
                                continue;

                            if (!char.IsLetter(txt[0]))
                                continue;

                            btn.Content = isUppercase
                                ? txt.ToUpper()
                                : txt.ToLower();
                        }
                    }
                }
            }
        }


        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            string key = btn.Content?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(key))
                return;


            // BACKSPACE
            if (key == "\uE72B")
            {
                if (VirtualKeyboardManager.Backspace())
                    return;

                KeyPressed?.Invoke(key);
                return;
            }


            // SPACE
            if (key == "\uE75D")
            {
                if (VirtualKeyboardManager.Space())
                    return;

                KeyPressed?.Invoke(key);
                return;
            }


            // RESET / CLEAR
            if (key == "Reset")
            {
                if (VirtualKeyboardManager.Clear())
                    return;

                KeyPressed?.Invoke(key);
                return;
            }


            // ENTER ostaje kontekstualan
            // ENTER
            if (key == "Enter")
            {
                VirtualKeyboardManager.Enter();
                return;
            }


            // SAKRIJ
            if (key == "Sakrij")
            {
                HideKeyboard();
                return;
            }


            // NORMALAN UNOS
            if (VirtualKeyboardManager.InsertText(key))
                return;


            // Nema aktivnog TextBoxa:
            // npr. KasaPage koristi slova za filtriranje artikala.
            KeyPressed?.Invoke(key);
        }


        private void HideKeyboard()
        {
            DependencyObject? parent = this;

            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);

                if (parent is ContentControl contentControl &&
                   ReferenceEquals(contentControl.Content, this))
                {
                    contentControl.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            Visibility = Visibility.Collapsed;
        }
    }
}