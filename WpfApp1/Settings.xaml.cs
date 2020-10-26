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
        bool func_list = true;

        public Settings()
        {
            InitializeComponent();

            fill_Recipes();
            fill_Equipment();
            fill_Juice_Dropdown(cb_Juice_Recipe);
            fill_Function_Dropdown(cb_Function_Recipe);
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

        private void fill_Juice_Dropdown(ComboBox cb)
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

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "Juice";
                cb.SelectedValuePath = "id";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_Function_Dropdown(ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Function_List_All]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "Functionality";
                cb.SelectedValuePath = "id";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private string get_Equipment(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return row["Equipment"].ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_Function(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return row["Function"].ToString();
            }

            catch (Exception ex)
            { return null; }
        }

        private string get_Recipe(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return row["Recipe"].ToString();
            }

            catch (Exception ex)
            { return null; }
        }

        private string get_Time(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return row["Time"].ToString();
            }

            catch (Exception ex)
            { return null; }
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
            tb_Name_Function.Text = "";
            
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
            btn_Edit_Function.IsEnabled = true;
            btn_Add_Function.IsEnabled = true;

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
            btn_Edit_Function.IsEnabled = false;
            btn_Add_Function.IsEnabled = false;

            btn_Edit_Equipment.Visibility = Visibility.Visible;
            btn_Add_Equipment.Visibility = Visibility.Visible;
            btn_Save_Equipment.Visibility = Visibility.Hidden;
            btn_Submit_Equipment.Visibility = Visibility.Hidden;
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
            if (String.IsNullOrEmpty(tb_Name_Equipment.Text))
            {
                return;
            }

            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[update_Equipment]";
                cmd.Parameters.Add("oldEquip", SqlDbType.VarChar).Value = get_Equipment(dg_Equip);
                cmd.Parameters.Add("newEquip", SqlDbType.VarChar).Value = tb_Name_Equipment.Text;
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            fill_Equipment();
            dg_Equip.IsEnabled = true;
            tb_Name_Equipment.IsEnabled = false;
            btn_Equip_AddTo.IsEnabled = false;
            btn_Equip_RemoveFrom.IsEnabled = false;
            btn_Edit_Function.IsEnabled = false;
            btn_Add_Function.IsEnabled = false;

            btn_Edit_Equipment.Visibility = Visibility.Visible;
            btn_Add_Equipment.Visibility = Visibility.Visible;
            btn_Save_Equipment.Visibility = Visibility.Hidden;
            btn_Submit_Equipment.Visibility = Visibility.Hidden;
            btn_Cancel_Equipment.Visibility = Visibility.Hidden;
        }

        private void btn_Add_Equipment_Click(object sender, RoutedEventArgs e)
        {
            dg_Equip.IsEnabled = false;
            dg_Equip.UnselectAll();
            dg_Function_List.ItemsSource = null;
            dg_Applied_Functions.ItemsSource = null;

            tb_Name_Equipment.IsEnabled = true;
            btn_Equip_AddTo.IsEnabled = false;
            btn_Equip_RemoveFrom.IsEnabled = false;

            btn_Edit_Equipment.Visibility = Visibility.Hidden;
            btn_Add_Equipment.Visibility = Visibility.Hidden;
            btn_Submit_Equipment.Visibility = Visibility.Visible;
            btn_Cancel_Equipment.Visibility = Visibility.Visible;
        }

        private void dg_Function_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tb_Name_Function.Text = get_Function(dg_Function_List);
            func_list = true;
        }

        private void dg_Applied_Functions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tb_Name_Function.Text = get_Function(dg_Applied_Functions);
            func_list = false;
        }

        private void btn_Submit_Equipment_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tb_Name_Equipment.Text))
            {
                return;
            }

            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[insert_Equipment]";
                cmd.Parameters.Add("equip", SqlDbType.VarChar).Value = tb_Name_Equipment.Text;
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            fill_Equipment();
            dg_Equip.IsEnabled = true;
            tb_Name_Equipment.IsEnabled = false;
            tb_Name_Equipment.Text = "";
            btn_Equip_AddTo.IsEnabled = false;
            btn_Equip_RemoveFrom.IsEnabled = false;
            btn_Edit_Function.IsEnabled = false;
            btn_Add_Function.IsEnabled = false;

            btn_Edit_Equipment.Visibility = Visibility.Visible;
            btn_Add_Equipment.Visibility = Visibility.Visible;
            btn_Save_Equipment.Visibility = Visibility.Hidden;
            btn_Submit_Equipment.Visibility = Visibility.Hidden;
            btn_Cancel_Equipment.Visibility = Visibility.Hidden;
        }

        private void btn_Edit_Function_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Function_List.SelectedItems.Count == 0 && dg_Applied_Functions.SelectedItems.Count == 0)
            {
                return;
            }

            dg_Equip.IsEnabled = false;
            dg_Function_List.IsEnabled = false;
            dg_Applied_Functions.IsEnabled = false;

            //tb_Name_Equipment.IsEnabled = false;
            tb_Name_Function.IsEnabled = true;
            btn_Equip_AddTo.IsEnabled = false;
            btn_Equip_RemoveFrom.IsEnabled = false;

            btn_Edit_Function.Visibility = Visibility.Hidden;
            btn_Add_Function.Visibility = Visibility.Hidden;
            btn_Save_Function.Visibility = Visibility.Visible;
            btn_Cancel_Function.Visibility = Visibility.Visible;
        }

        private void btn_Add_Function_Click(object sender, RoutedEventArgs e)
        {
            dg_Equip.IsEnabled = false;
            dg_Function_List.UnselectAll();
            dg_Applied_Functions.UnselectAll();
            dg_Function_List.IsEnabled = false;
            dg_Applied_Functions.IsEnabled = false;

            //tb_Name_Equipment.IsEnabled = true;
            tb_Name_Function.IsEnabled = true;
            btn_Equip_AddTo.IsEnabled = false;
            btn_Equip_RemoveFrom.IsEnabled = false;

            btn_Edit_Equipment.Visibility = Visibility.Hidden;
            btn_Add_Equipment.Visibility = Visibility.Hidden;
            btn_Submit_Function.Visibility = Visibility.Visible;
            btn_Cancel_Function.Visibility = Visibility.Visible;
        }

        private void btn_Cancel_Function_Click(object sender, RoutedEventArgs e)
        {
            tb_Name_Function.Text = get_Function(dg_Function_List);

            dg_Equip.IsEnabled = true;
            dg_Function_List.IsEnabled = true;
            dg_Applied_Functions.IsEnabled = true;

            tb_Name_Function.IsEnabled = false;
            btn_Equip_AddTo.IsEnabled = true;
            btn_Equip_RemoveFrom.IsEnabled = true;

            btn_Edit_Function.Visibility = Visibility.Visible;
            btn_Add_Function.Visibility = Visibility.Visible;
            btn_Save_Function.Visibility = Visibility.Hidden;
            btn_Submit_Function.Visibility = Visibility.Hidden;
            btn_Cancel_Function.Visibility = Visibility.Hidden;
        }

        private void btn_Save_Function_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tb_Name_Function.Text))
            {
                return;
            }

            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[update_Function]";
                cmd.Parameters.Add("oldFunc", SqlDbType.VarChar).Value = (func_list == true) ? get_Function(dg_Function_List) : get_Function(dg_Applied_Functions);
                cmd.Parameters.Add("newFunc", SqlDbType.VarChar).Value = tb_Name_Function.Text;
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            fill_Functions(get_Equipment(dg_Equip));
            fill_AppliedFunctions(get_Equipment(dg_Equip));

            dg_Equip.IsEnabled = true;
            dg_Function_List.IsEnabled = true;
            dg_Applied_Functions.IsEnabled = true;

            tb_Name_Function.IsEnabled = false;
            tb_Name_Function.Text = "";
            btn_Equip_AddTo.IsEnabled = true;
            btn_Equip_RemoveFrom.IsEnabled = true;

            btn_Edit_Function.Visibility = Visibility.Visible;
            btn_Add_Function.Visibility = Visibility.Visible;
            btn_Save_Function.Visibility = Visibility.Hidden;
            btn_Submit_Function.Visibility = Visibility.Hidden;
            btn_Cancel_Function.Visibility = Visibility.Hidden;
        }

        private void btn_Submit_Function_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tb_Name_Function.Text))
            {
                return;
            }

            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[insert_Function]";
                cmd.Parameters.Add("func", SqlDbType.VarChar).Value = tb_Name_Function.Text;
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            fill_Functions(get_Equipment(dg_Equip));
            fill_AppliedFunctions(get_Equipment(dg_Equip));

            dg_Equip.IsEnabled = true;
            dg_Function_List.IsEnabled = true;
            dg_Applied_Functions.IsEnabled = true;

            tb_Name_Function.IsEnabled = false;
            tb_Name_Function.Text = "";
            btn_Equip_AddTo.IsEnabled = true;
            btn_Equip_RemoveFrom.IsEnabled = true;

            btn_Edit_Function.Visibility = Visibility.Visible;
            btn_Add_Function.Visibility = Visibility.Visible;
            btn_Save_Function.Visibility = Visibility.Hidden;
            btn_Submit_Function.Visibility = Visibility.Hidden;
            btn_Cancel_Function.Visibility = Visibility.Hidden;
        }

        private void btn_Edit_Recipe_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Recipe.SelectedItems.Count == 0)
            {
                return;
            }

            dg_Recipe.IsEnabled = false;
            tb_Name_Recipe.IsEnabled = true;
            cb_Juice_Recipe.IsEnabled = true;
            chck_Inline.IsEnabled = true;
            cb_Function_Recipe.IsEnabled = true;
            tb_Time_Recipe.IsEnabled = true;
            btn_Set_Time.IsEnabled = true;

            btn_Edit_Recipe.Visibility = Visibility.Hidden;
            btn_Add_Recipe.Visibility = Visibility.Hidden;
            btn_Save_Recipe.Visibility = Visibility.Visible;
            btn_Cancel_Recipe.Visibility = Visibility.Visible;
        }

        private void btn_Add_Recipe_Click(object sender, RoutedEventArgs e)
        {
            dg_Recipe.IsEnabled = false;
            dg_Recipe.UnselectAll();
            dg_Function_Times.ItemsSource = null;
            tb_Name_Recipe.IsEnabled = true;
            cb_Juice_Recipe.IsEnabled = true;
            chck_Inline.IsEnabled = true;

            btn_Edit_Recipe.Visibility = Visibility.Hidden;
            btn_Add_Recipe.Visibility = Visibility.Hidden;
            btn_Submit_Recipe.Visibility = Visibility.Visible;
            btn_Cancel_Recipe.Visibility = Visibility.Visible;
        }

        private void dg_Recipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected_Recipe = get_Recipe(dg_Recipe);
            tb_Name_Recipe.Text = selected_Recipe;

            fill_RecipeInfo(selected_Recipe);
            fill_FunctionTimes(selected_Recipe);
        }

        private void fill_RecipeInfo(string recipe)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Recipe]";
                cmd.Parameters.Add("recipe", SqlDbType.VarChar).Value = recipe;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb_Juice_Recipe.SelectedValue = Convert.ToInt32(dt.Rows[0]["Juice"]);
                chck_Inline.IsChecked = Convert.ToBoolean(dt.Rows[0]["Inline"]);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_FunctionTimes(string recipe)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_FunctionTimes]";
                cmd.Parameters.Add("recipe", SqlDbType.VarChar).Value = recipe;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dg_Function_Times.ItemsSource = dt.DefaultView;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_Cancel_Recipe_Click(object sender, RoutedEventArgs e)
        {
            tb_Name_Recipe.Text = get_Recipe(dg_Recipe);

            dg_Recipe.IsEnabled = true;
            dg_Function_Times.IsEnabled = true;

            tb_Name_Recipe.IsEnabled = false;
            cb_Juice_Recipe.IsEnabled = false;
            chck_Inline.IsEnabled = false;
            cb_Function_Recipe.IsEnabled = false;
            tb_Time_Recipe.IsEnabled = false;
            btn_Set_Time.IsEnabled = false;

            btn_Edit_Recipe.Visibility = Visibility.Visible;
            btn_Add_Recipe.Visibility = Visibility.Visible;
            btn_Save_Recipe.Visibility = Visibility.Hidden;
            btn_Submit_Recipe.Visibility = Visibility.Hidden;
            btn_Cancel_Recipe.Visibility = Visibility.Hidden;
        }

        private void btn_Save_Recipe_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Submit_Recipe_Click(object sender, RoutedEventArgs e)
        {

        }

        private void dg_Function_Times_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cb_Function_Recipe.Text = get_Function(dg_Function_Times);
            tb_Time_Recipe.Text = get_Time(dg_Function_Times);
        }
    }
}
