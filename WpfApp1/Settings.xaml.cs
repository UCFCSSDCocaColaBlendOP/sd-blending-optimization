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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Settings : MetroWindow
    {
        public Settings()
        {
            InitializeComponent();

            fill_Recipes();
            fill_Equipment();
        }

        private void fill_Functions(string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Function_List]";
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Function_List.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_AppliedFunctions(string equip)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_AppliedFunction_List]";
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = equip;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Applied_Functions.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private string get_Equipment(DataGrid dg)
        {
            DataRowView row = (dg.SelectedItems.Count == 0 ? null : (DataRowView)dg.SelectedItems[0]);
            return row["Equipment"].ToString();
        }

        private string get_Function(DataGrid dg)
        {
            DataRowView row = (dg.SelectedItems.Count == 0 ? null : (DataRowView)dg.SelectedItems[0]);
            return row["Function"].ToString();
        }

        private void fill_Recipes()
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Recipe_List]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Recipe.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_Juices()
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Juice_List]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Juice.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_Equipment()
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Equip_List]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Equip.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void dg_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected_Equip = get_Equipment(dg_Equip);
            tb_Name_Equipment.Text = selected_Equip;
            
            fill_Functions(selected_Equip);
            fill_AppliedFunctions(selected_Equip);
        }

        private void btn_Edit_Equipment_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Equip.SelectedItems.Count == 0)
            {
                return;
            }

            dg_Equip.IsEnabled = false;
            tb_Name_Equipment.IsEnabled = true;
            btn_Equip_AddTo.IsEnabled = true;
            btn_Equip_RemoveFrom.IsEnabled = true;

            btn_Edit_Equipment.Visibility = Visibility.Hidden;
            btn_Add_Equipment.Visibility = Visibility.Hidden;
            btn_Save_Equipment.Visibility = Visibility.Visible;
            btn_Cancel_Equipment.Visibility = Visibility.Visible;
        }

        private void btn_Cancel_Equipment_Click(object sender, RoutedEventArgs e)
        {
            tb_Name_Equipment.Text = get_Equipment(dg_Equip);

            dg_Equip.IsEnabled = true;
            tb_Name_Equipment.IsEnabled = false;
            btn_Equip_AddTo.IsEnabled = false;
            btn_Equip_RemoveFrom.IsEnabled = false;

            btn_Edit_Equipment.Visibility = Visibility.Visible;
            btn_Add_Equipment.Visibility = Visibility.Visible;
            btn_Save_Equipment.Visibility = Visibility.Hidden;
            btn_Cancel_Equipment.Visibility = Visibility.Hidden;
        }

        private void btn_Equip_AddTo_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Function_List.SelectedItems.Count == 0)
            {
                return;
            }

            string selected_Equip = get_Equipment(dg_Equip);
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[insert_EquipToFunc]";
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = selected_Equip;
                cmd.Parameters.Add("func", SqlDbType.VarChar).Value = get_Function(dg_Function_List);
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            fill_Functions(selected_Equip);
            fill_AppliedFunctions(selected_Equip);
        }

        private void btn_Equip_RemoveFrom_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Applied_Functions.SelectedItems.Count == 0)
            {
                return;
            }

            string selected_Equip = get_Equipment(dg_Equip);
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[remove_EquipToFunc]";
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = selected_Equip;
                cmd.Parameters.Add("func", SqlDbType.VarChar).Value = get_Function(dg_Applied_Functions);
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            fill_Functions(selected_Equip);
            fill_AppliedFunctions(selected_Equip);
        }

        private void btn_Save_Equipment_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
