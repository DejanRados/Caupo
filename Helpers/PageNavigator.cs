using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Caupo.Helpers
{
    public static class PageNavigator
    {
        public static Action<UserControl> Navigate { get; set; }
        public static void NavigateWithFade(UserControl newPage)
        {
            Navigate?.Invoke (newPage); // ovdje će MainWindow odraditi fade
        }
    }
}
