using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class SupplierPopup : Window
    {
        public SupplierPopupViewModel VM { get; set; }

        public SupplierPopup()
        {
            InitializeComponent ();
            VM = new SupplierPopupViewModel ();
            DataContext = VM;
        }

        public SupplierPopup(TblDobavljaci d)
        {
            InitializeComponent ();
            VM = new SupplierPopupViewModel (d);
            DataContext = VM;
        }

        public TblDobavljaci? Open()
        {
            return ShowDialog () == true ? VM.Dobavljac : null;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }



        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(sender is TextBox textBox)
            {
                textBox.SelectAll ();
            }
        }
    }
}