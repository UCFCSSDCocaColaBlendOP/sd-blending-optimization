using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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
        Schedule2 sch;

        public MainWindow()
        {
            InitializeComponent();

            // Functions to fill each tab's DataGrid     
            refresh();
        }

        private void btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            string filename = "";
            ofd.InitialDirectory = "";
            ofd.Title = "Open Spreadsheet";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.RestoreDirectory = true;
            ofd.Filter = "CSV files (*.csv)|*.csv";
            ofd.FilterIndex = 2;

            if (ofd.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                filename = ofd.FileName;
                sch = new Schedule2(filename);

                Generate form = new Generate(sch, filename);
                form.Show();
                Mouse.OverrideCursor = null;
            }           
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

        private void fill_SO_Equip(int so_ID, ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Equip]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "Equipment";
                cb.SelectedValuePath = "Equipment";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_SO1(int so_ID, string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Data]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_SO1.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_SO2(int so_ID, string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Data]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_SO2.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_Shared(int so_ID, string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Data]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Shared.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_TL(int so_ID, string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Data]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Transfer_Line.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_Aseptic(int so_ID, string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Data]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Aseptic.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void dg_SO1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cb_SO1_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SO1_Equip.SelectedValue != null)
                fill_SO1(1, cb_SO1_Equip.SelectedValue.ToString());
        }

        private void cb_SO2_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SO2_Equip.SelectedValue != null)
                fill_SO2(2, cb_SO2_Equip.SelectedValue.ToString());
        }

        private void cb_Shared_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Shared_Equip.SelectedValue != null)
                fill_Shared(3, cb_Shared_Equip.SelectedValue.ToString());
        }

        private void cb_TL_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_TL_Equip.SelectedValue != null)
                fill_TL(4, cb_TL_Equip.SelectedValue.ToString());
        }

        private void cb_Aseptic_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Aseptic_Equip.SelectedValue != null)
                fill_Aseptic(5, cb_Aseptic_Equip.SelectedValue.ToString());
        }

        private void btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            refresh();
        }

        private void refresh()
        {
            // SO1
            fill_SO_Equip(1, cb_SO1_Equip);
            cb_SO1_Equip.SelectedIndex = 0;
            if (cb_SO1_Equip.SelectedValue != null)
                fill_SO1(1, cb_SO1_Equip.SelectedValue.ToString());

            // SO2
            fill_SO_Equip(2, cb_SO2_Equip);
            cb_SO2_Equip.SelectedIndex = 0;
            if (cb_SO2_Equip.SelectedValue != null)
                fill_SO2(2, cb_SO2_Equip.SelectedValue.ToString());

            // Shared
            fill_SO_Equip(3, cb_Shared_Equip);
            cb_Shared_Equip.SelectedIndex = 0;
            if (cb_Shared_Equip.SelectedValue != null)
                fill_Shared(3, cb_Shared_Equip.SelectedValue.ToString());

            // Transfer Line
            fill_SO_Equip(4, cb_TL_Equip);
            cb_TL_Equip.SelectedIndex = 0;
            if (cb_TL_Equip.SelectedValue != null)
                fill_TL(4, cb_TL_Equip.SelectedValue.ToString());

            // Aseptic
            fill_SO_Equip(5, cb_Aseptic_Equip);
            cb_Aseptic_Equip.SelectedIndex = 0;
            if (cb_Aseptic_Equip.SelectedValue != null)
                fill_Aseptic(5, cb_Aseptic_Equip.SelectedValue.ToString());
        }
    }
}
