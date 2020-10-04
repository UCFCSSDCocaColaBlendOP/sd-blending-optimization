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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Viewing.xaml
    /// </summary>
    public partial class Generate : MetroWindow
    {
        public Generate()
        {
            InitializeComponent();

        }

        private void tc_Home_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_Next_Click(object sender, RoutedEventArgs e)
        {
            tc_Home.SelectedIndex = 1;
        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            tc_Home.SelectedIndex = 0;
        }
    }
}
