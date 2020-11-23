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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Viewing.xaml
    /// </summary>
    public partial class Viewing : MetroWindow
    {
        public Viewing()
        {
            InitializeComponent();

            fill_Sch_ID(cb_SchID);
        }

        private void fill_Sch_ID(ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Sch_List]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "schedule_id";
                cb.SelectedValuePath = "schedule_id";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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

        private void fill_SO_Juice(int so_ID, DateTime sch_ID, ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_SO_Juice]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("sch_ID", SqlDbType.DateTime).Value = sch_ID;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "Juice";
                cb.SelectedValuePath = "Juice";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void cb_SchID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Type.SelectedIndex == -1)
            {
                return;
            }
        }

        private void cb_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SchID.SelectedIndex == -1)
            {
                return;
            }

            if (cb_SchID.Text == "By Juice")
            {
                dg_SO1_Juice.Visibility = Visibility.Visible;
                dg_SO2_Juice.Visibility = Visibility.Visible;
                dg_Shared_Juice.Visibility = Visibility.Visible;
                dg_TL_Juice.Visibility = Visibility.Visible;
                dg_Aseptic_Juice.Visibility = Visibility.Visible;
                cb_SO1_Juice.Visibility = Visibility.Visible;
                cb_SO2_Juice.Visibility = Visibility.Visible;
                cb_Shared_Juice.Visibility = Visibility.Visible;
                cb_TL_Juice.Visibility = Visibility.Visible;
                cb_Aseptic_Juice.Visibility = Visibility.Visible;

                dg_SO1_Equip.Visibility = Visibility.Hidden;
                dg_SO2_Equip.Visibility = Visibility.Hidden;
                dg_Shared_Equip.Visibility = Visibility.Hidden;
                dg_TL_Equip.Visibility = Visibility.Hidden;
                dg_Aseptic_Equip.Visibility = Visibility.Hidden;
                cb_SO1_Equip.Visibility = Visibility.Hidden;
                cb_SO2_Equip.Visibility = Visibility.Hidden;
                cb_Shared_Equip.Visibility = Visibility.Hidden;
                cb_TL_Equip.Visibility = Visibility.Hidden;
                cb_Aseptic_Equip.Visibility = Visibility.Hidden;
            }

            else if (cb_SchID.Text == "By Equipment")
            {
                dg_SO1_Juice.Visibility = Visibility.Hidden;
                dg_SO2_Juice.Visibility = Visibility.Hidden;
                dg_Shared_Juice.Visibility = Visibility.Hidden;
                dg_TL_Juice.Visibility = Visibility.Hidden;
                dg_Aseptic_Juice.Visibility = Visibility.Hidden;
                cb_SO1_Juice.Visibility = Visibility.Hidden;
                cb_SO2_Juice.Visibility = Visibility.Hidden;
                cb_Shared_Juice.Visibility = Visibility.Hidden;
                cb_TL_Juice.Visibility = Visibility.Hidden;
                cb_Aseptic_Juice.Visibility = Visibility.Hidden;

                dg_SO1_Equip.Visibility = Visibility.Visible;
                dg_SO2_Equip.Visibility = Visibility.Visible;
                dg_Shared_Equip.Visibility = Visibility.Visible;
                dg_TL_Equip.Visibility = Visibility.Visible;
                dg_Aseptic_Equip.Visibility = Visibility.Visible;
                cb_SO1_Equip.Visibility = Visibility.Visible;
                cb_SO2_Equip.Visibility = Visibility.Visible;
                cb_Shared_Equip.Visibility = Visibility.Visible;
                cb_TL_Equip.Visibility = Visibility.Visible;
                cb_Aseptic_Equip.Visibility = Visibility.Visible;
            }
        }

        private void fill_Data_Equip(int so_ID, string equip, DataGrid dg, DateTime sch_ID)
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
                cmd.Parameters.Add("sch_ID", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
