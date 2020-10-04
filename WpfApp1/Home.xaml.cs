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
using Microsoft.Win32;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // Functions to fill each tab's DataGrid            
            fill_SO1();
            fill_SO2();
            fill_Shared();
        }

        private void btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = "";
            ofd.Title = "Open Spreadsheet";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.RestoreDirectory = true;
            ofd.Filter = "CSV files (*.csv)|*.csv|Excel Files|*.xls;*.xlsx;*.xlsm";
            ofd.FilterIndex = 2;

            if (ofd.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string filename = ofd.FileName;
                Mouse.OverrideCursor = null;
            }

            Generate form = new Generate();
            form.Show();
        }

        private void btn_Export_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Viewing form = new Viewing();
            form.Show();
            Mouse.OverrideCursor = null;
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Settings form = new Settings();
            form.Show();
            Mouse.OverrideCursor = null;
        }

        private void fill_SO1()
        {

        }

        private void fill_SO2()
        {

        }

        private void fill_Shared()
        {

        }
    }
}
