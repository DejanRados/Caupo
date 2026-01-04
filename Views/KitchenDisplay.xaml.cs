using Caupo.ViewModels;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for KitchenDisplay.xaml
    /// </summary>
    public partial class KitchenDisplay : Window
    {
        public KitchenDisplay()
        {
            InitializeComponent ();
            DataContext = App.GlobalKitchenVM;
        }

        private void Order_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel sp && sp.DataContext is KitchenDisplayViewModel.DisplayOrder order)
            {
                // order je instanca Order klase iz nested klase
                if (this.DataContext is KitchenDisplayViewModel vm)
                {
                    vm.RemoveOrder(order);
                }
            }
        }

    }
}
