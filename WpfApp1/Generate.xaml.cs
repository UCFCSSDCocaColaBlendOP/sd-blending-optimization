using System;
using System.Collections.Generic;
using System.Data;
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
using WpfApp1.Classes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Viewing.xaml
    /// </summary>
    public partial class Generate : MetroWindow
    {
        Schedule2 sch;

        public struct JuiceList
        {
            public string juice { set; get; }
            public bool start { set; get; }
            public DateTime time { set; get; }
            public bool mixing { set; get; }
            public int line { set; get; }
        }

        public Generate(Schedule2 sch)
        {
            InitializeComponent();
            this.sch = sch;

            List<Juice> juices = sch.get_inProgress();

            DataGridTextColumn colJuice = new DataGridTextColumn();
            colJuice.Header = "Juices";
            colJuice.Binding = new Binding("juice");
            dg_Juices.Columns.Add(colJuice);

            DataGridTextColumn colStart = new DataGridTextColumn();
            colStart.Header = "Starter";
            colStart.Binding = new Binding("start");
            dg_Juices.Columns.Add(colStart);

            DataGridTextColumn colTime = new DataGridTextColumn();
            colTime.Header = "Time";
            colTime.Binding = new Binding("time");
            dg_Juices.Columns.Add(colTime);

            DataGridTextColumn colMixing = new DataGridTextColumn();
            colMixing.Header = "Mixing";
            colMixing.Binding = new Binding("mixing");
            dg_Juices.Columns.Add(colMixing);

            DataGridTextColumn colTL = new DataGridTextColumn();
            colTL.Header = "Transfer Line";
            colTL.Binding = new Binding("line");
            dg_Juices.Columns.Add(colTL);

            foreach (Juice juice in juices)
            {
                dg_Juices.Items.Add(new JuiceList { juice = juice.name, start = juice.starter, time = juice.OGFillTime, mixing = juice.mixing, line = juice.line });
            }
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

        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_NextToThaw_Click(object sender, RoutedEventArgs e)
        {
            tc_Home.SelectedIndex = 1;
        }

        private void btn_BackToThaw_Click(object sender, RoutedEventArgs e)
        {
            tc_Home.SelectedIndex = 1;
        }

        private void btn_NextToEquip_Click(object sender, RoutedEventArgs e)
        {
            tc_Home.SelectedIndex = 2;
        }

        private void btn_BackToJuice_Click(object sender, RoutedEventArgs e)
        {
            tc_Home.SelectedIndex = 0;
        }

        private void cb_State_Copy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void chck_Enable_Click(object sender, RoutedEventArgs e)
        {
            if (chck_Enable.IsChecked == true)
            {
                tb_Thaw_Start.IsEnabled = true;
                tb_Thaw_Stop.IsEnabled = true;
                cb_Thaw_Juice.IsEnabled = true;
                btn_AddToThaw.IsEnabled = true;
                btn_RemoveFromThaw.IsEnabled = true;
            }

            else
            {
                tb_Thaw_Start.IsEnabled = false;
                tb_Thaw_Stop.IsEnabled = false;
                cb_Thaw_Juice.IsEnabled = false;
                btn_AddToThaw.IsEnabled = false;
                btn_RemoveFromThaw.IsEnabled = false;
            }
        }

        private void chck_Juice_Filling_Click(object sender, RoutedEventArgs e)
        {
            if (chck_Juice_Filling.IsChecked == true)
            {
                chck_Inline_Fill.IsEnabled = true;
                cb_Transfer_Line.IsEnabled = true;
                cb_Blend_Tank_Fill.IsEnabled = true;
                tb_TL_Duration.IsEnabled = true;
                tb_BT_Duration.IsEnabled = true;
            }

            else
            {
                chck_Inline_Fill.IsChecked = false;
                tb_Batches_Fill.IsEnabled = false;
                chck_Inline_Fill.IsEnabled = false;
                cb_Transfer_Line.IsEnabled = false;
                cb_Blend_Tank_Fill.IsEnabled = false;
                tb_TL_Duration.IsEnabled = false;
                tb_BT_Duration.IsEnabled = false;
            }
        }

        private void chck_Juice_Mix_Click(object sender, RoutedEventArgs e)
        {
            if (chck_Juice_Mix.IsChecked == true)
            {
                chck_Inline_Mix.IsEnabled = true;
                cb_Equipment_Mix.IsEnabled = true;
                cb_Blend_Tank_Mix.IsEnabled = true;
                tb_Equip_Duration.IsEnabled = true;
            }

            else
            {
                chck_Inline_Mix.IsChecked = false;
                tb_Batches_Mix.IsEnabled = false;
                chck_Inline_Mix.IsEnabled = false;
                cb_Equipment_Mix.IsEnabled = false;
                cb_Blend_Tank_Mix.IsEnabled = false;
                tb_Equip_Duration.IsEnabled = false;
            }
        }

        private void chck_Inline_Fill_Click(object sender, RoutedEventArgs e)
        {
            if (chck_Inline_Fill.IsChecked == true)
            {
                tb_Batches_Fill.IsEnabled = true;
            }

            else
            {
                tb_Batches_Fill.IsEnabled = false;
            }
        }

        private void chck_Inline_Mix_Click(object sender, RoutedEventArgs e)
        {
            if (chck_Inline_Mix.IsChecked == true)
            {
                tb_Batches_Mix.IsEnabled = true;
            }

            else
            {
                tb_Batches_Mix.IsEnabled = false;
            }
        }

        private void btn_AddToThaw_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_RemoveFromThaw_Click(object sender, RoutedEventArgs e)
        {

        }

        private string get_Juice_Time(DataGrid dg)
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
            { return ""; }
        }

        private int get_TL(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return 0;
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return Convert.ToInt32(row["Transfer Line"]);
            }

            catch (Exception ex)
            { return 0; }
        }

        private bool get_Starter(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return Convert.ToBoolean(row["Starter"]);
            }

            catch (Exception ex)
            { return false; }
        }

        private bool get_Mixing(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                DataRowView row = (DataRowView)dg.SelectedItems[0];
                return Convert.ToBoolean(row["Mixing"]);
            }

            catch (Exception ex)
            { return false; }
        }

        private void dg_Juices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chck_Start_Juice.IsChecked = get_Starter(dg_Juices);
            tb_Juice_Time.Text = get_Juice_Time(dg_Juices);
            chck_Juice_Mix.IsChecked = get_Mixing(dg_Juices);
            cb_Transfer_Line.Text = get_TL(dg_Juices).ToString();
        }
    }
}
