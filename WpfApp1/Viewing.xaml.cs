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
            cb_Type.SelectedIndex = 0;
        }

        private void refresh()
        {
            // SO1
            fill_SO_Juice(1, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()), cb_SO1_Juice);
            cb_SO1_Juice.SelectedIndex = 0;
            if (cb_SO1_Juice.SelectedValue != null)
            {
                fill_Data_Juice(1, cb_SO1_Juice.SelectedValue.ToString(), dg_SO1_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
            fill_SO_Equip(1, cb_SO1_Equip);
            cb_SO1_Equip.SelectedIndex = 0;
            if (cb_SO1_Equip.SelectedValue != null)
            {
                fill_Data_Equip(1, cb_SO1_Equip.SelectedValue.ToString(), dg_SO1_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }

            // SO2
            fill_SO_Juice(2, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()), cb_SO2_Juice);
            cb_SO2_Juice.SelectedIndex = 0;
            if (cb_SO2_Juice.SelectedValue != null)
            {
                fill_Data_Juice(2, cb_SO2_Juice.SelectedValue.ToString(), dg_SO2_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
            fill_SO_Equip(2, cb_SO2_Equip);
            cb_SO2_Equip.SelectedIndex = 0;
            if (cb_SO2_Equip.SelectedValue != null)
            {
                fill_Data_Equip(2, cb_SO2_Equip.SelectedValue.ToString(), dg_SO2_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }

            // Shared
            fill_SO_Juice(3, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()), cb_Shared_Juice);
            cb_Shared_Juice.SelectedIndex = 0;
            if (cb_Shared_Juice.SelectedValue != null)
            {
                fill_Data_Juice(3, cb_Shared_Juice.SelectedValue.ToString(), dg_Shared_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
            fill_SO_Equip(3, cb_Shared_Equip);
            cb_Shared_Equip.SelectedIndex = 0;            
            if (cb_Shared_Equip.SelectedValue != null)
            {
                fill_Data_Equip(3, cb_Shared_Equip.SelectedValue.ToString(), dg_Shared_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }

            // Transfer Line
            fill_SO_Juice(4, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()), cb_TL_Juice);
            cb_TL_Juice.SelectedIndex = 0;
            if (cb_TL_Juice.SelectedValue != null)
            {
                fill_Data_Juice(4, cb_TL_Juice.SelectedValue.ToString(), dg_TL_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
            fill_SO_Equip(4, cb_TL_Equip);
            cb_TL_Equip.SelectedIndex = 0;
            if (cb_TL_Equip.SelectedValue != null)
            {
                fill_Data_Equip(4, cb_TL_Equip.SelectedValue.ToString(), dg_TL_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }

            // Aseptic
            fill_SO_Juice(5, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()), cb_Aseptic_Juice);
            cb_Aseptic_Juice.SelectedIndex = 0;
            if (cb_Aseptic_Juice.SelectedValue != null)
            {
                fill_Data_Juice(5, cb_Aseptic_Juice.SelectedValue.ToString(), dg_Aseptic_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
            fill_SO_Equip(5, cb_Aseptic_Equip);
            cb_Aseptic_Equip.SelectedIndex = 0;
            if (cb_Aseptic_Equip.SelectedValue != null)
            {
                fill_Data_Equip(5, cb_Aseptic_Equip.SelectedValue.ToString(), dg_Aseptic_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
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
            if (cb_Type.SelectedIndex != -1)
            {
                refresh();
            }
        }

        private void cb_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Type.SelectedIndex == 0)
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

            else if (cb_Type.SelectedIndex == 1)
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
                cmd.CommandText = "[select_View_Sch_Equip]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Parameters.Add("sch_ID", SqlDbType.VarChar).Value = sch_ID;
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

        private void fill_Data_Juice(int so_ID, string juice, DataGrid dg, DateTime sch_ID)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_View_Sch_Juice]";
                cmd.Parameters.Add("so_ID", SqlDbType.BigInt).Value = so_ID;
                cmd.Parameters.Add("juice", SqlDbType.VarChar).Value = juice;
                cmd.Parameters.Add("sch_ID", SqlDbType.VarChar).Value = sch_ID;
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

        private void cb_SO1_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SO1_Equip.SelectedValue != null)
            {
                fill_Data_Equip(1, cb_SO1_Equip.SelectedValue.ToString(), dg_SO1_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_SO1_Juice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SO1_Juice.SelectedValue != null)
            {
                fill_Data_Juice(1, cb_SO1_Juice.SelectedValue.ToString(), dg_SO1_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_SO2_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SO2_Equip.SelectedValue != null)
            {
                fill_Data_Equip(2, cb_SO2_Equip.SelectedValue.ToString(), dg_SO2_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_SO2_Juice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SO2_Juice.SelectedValue != null)
            {
                fill_Data_Juice(2, cb_SO2_Juice.SelectedValue.ToString(), dg_SO2_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_Shared_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Shared_Equip.SelectedValue != null)
            {
                fill_Data_Equip(3, cb_Shared_Equip.SelectedValue.ToString(), dg_Shared_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_Shared_Juice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Shared_Juice.SelectedValue != null)
            {
                fill_Data_Juice(3, cb_Shared_Juice.SelectedValue.ToString(), dg_Shared_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_TL_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_TL_Equip.SelectedValue != null)
            {
                fill_Data_Equip(4, cb_TL_Equip.SelectedValue.ToString(), dg_TL_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_TL_Juice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_TL_Juice.SelectedValue != null)
            {
                fill_Data_Juice(4, cb_TL_Juice.SelectedValue.ToString(), dg_TL_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_Aseptic_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Aseptic_Equip.SelectedValue != null)
            {
                fill_Data_Equip(5, cb_Aseptic_Equip.SelectedValue.ToString(), dg_Aseptic_Equip, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }

        private void cb_Aseptic_Juice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Aseptic_Juice.SelectedValue != null)
            {
                fill_Data_Juice(5, cb_Aseptic_Juice.SelectedValue.ToString(), dg_Aseptic_Juice, Convert.ToDateTime(cb_SchID.SelectedValue.ToString()));
            }
        }
    }
}
