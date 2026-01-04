using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Caupo.Views
{
    public partial class OrderCompletedPopup : Window
    {
        public OrderCompletedPopup(string waiter, string table,string brojbloka, IEnumerable<string> items)
        {
            InitializeComponent ();
            Debug.WriteLine ("Popup orderremove   InitializeComponent ();");
            // Prikaži poruku
          MessageTitle.Text = $"Narudžba broj {brojbloka}  je spremna";
           Waiter.Text = "Konobar: " + waiter ;
           TableName.Text = "Sto:" + table ;
            

        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
          
            this.Close ();
        }
    }
}

