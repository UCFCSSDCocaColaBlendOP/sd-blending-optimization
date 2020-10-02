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
using WpfApp1.Classes;

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

        }
        //maybe we want a button called "Upload CSV"

        private void btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.ShowDialog();
            string file_name = dlg.FileName;

            Schedule schedule = new Schedule();
            schedule.ProcessCSV(file_name);

            

         

      
        }
    

        private void btn_Export_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
