using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.Helpers
{
    public static class VirtualKeyboardManager
    {
        private static TextBox? _activeTextBox;
        private static bool _initialized;


        public static TextBox? ActiveTextBox => _activeTextBox;
        public static event Action? EnterPressed;


        public static void Initialize()
        {
            if (_initialized)
                return;

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                Keyboard.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus));

            _initialized = true;
        }

        public static void Enter()
        {
            EnterPressed?.Invoke();
        }

        private static void TextBox_GotKeyboardFocus(
            object sender,
            KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox &&
               textBox.IsEnabled &&
               !textBox.IsReadOnly)
            {
                _activeTextBox = textBox;
            }
        }


        public static void SetActiveTextBox(TextBox textBox)
        {
            _activeTextBox = textBox;
        }


        public static void ClearActiveTextBox()
        {
            _activeTextBox = null;
        }


        public static bool InsertText(string text)
        {
            TextBox? textBox = GetActiveTextBox();

            if (textBox == null)
                return false;

            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            string currentText = textBox.Text ?? string.Empty;

            if (selectionLength > 0)
            {
                currentText = currentText.Remove(
                    selectionStart,
                    selectionLength);
            }

            currentText = currentText.Insert(
                selectionStart,
                text);

            textBox.Text = currentText;

            textBox.SelectionStart =
                selectionStart + text.Length;

            textBox.SelectionLength = 0;

            return true;
        }


        public static bool Backspace()
        {
            TextBox? textBox = GetActiveTextBox();

            if (textBox == null)
                return false;

            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            if (selectionLength > 0)
            {
                textBox.Text = textBox.Text.Remove(
                    selectionStart,
                    selectionLength);

                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = 0;

                return true;
            }

            if (selectionStart <= 0)
                return true;

            textBox.Text = textBox.Text.Remove(
                selectionStart - 1,
                1);

            textBox.SelectionStart = selectionStart - 1;
            textBox.SelectionLength = 0;

            return true;
        }


        public static bool Space()
        {
            return InsertText(" ");
        }


        public static bool Clear()
        {
            TextBox? textBox = GetActiveTextBox();

            if (textBox == null)
                return false;

            textBox.Clear();
            textBox.SelectionStart = 0;
            textBox.SelectionLength = 0;

            return true;
        }


        private static TextBox? GetActiveTextBox()
        {
            if (_activeTextBox == null)
                return null;

            if (!_activeTextBox.IsLoaded ||
               !_activeTextBox.IsEnabled ||
               _activeTextBox.IsReadOnly)
            {
                _activeTextBox = null;
                return null;
            }

            return _activeTextBox;
        }
    }
}