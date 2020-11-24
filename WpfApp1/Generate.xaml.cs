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
    public partial class Generate : MetroWindow
    {
        string filename;
        Schedule2 sch;
        List<Juice> juices;

        public class JuicePair
        {
            public string name { set; get; }
            public int type { set; get; }
        }

        public class EquipList
        {
            public string equip { set; get; }
            public int state { set; get; }
            public int juice { set; get; }
            public int clean { set; get; }
            public string cleanName { set; get; }
            public DateTime time { set; get; }
        }

        public class ThawList
        {
            public string name { set; get; }
            public int juice { set; get; }
            public DateTime start { set; get; }
            public DateTime stop { set; get; }
        }

        public class EquipJuice
        {
            public string equip { set; get; }
            public DateTime time { set; get; }
        }

        public class JuiceList
        {
            public string juice { set; get; }
            public bool start { set; get; }
            public DateTime time { set; get; }
            public int type { set; get; }
            public int batchFilled { set; get; }
            public int batchTotal { set; get; }
            public bool filling { set; get; }
            public bool fillingInline { set; get; }
            public int fillingBatches { set; get; }
            public string fillingTL { set; get; }
            public DateTime fillingTLTime { set; get; }
            public string fillingBT { set; get; }
            public DateTime fillingBTTime { set; get; }
            public bool mixing { set; get; }
            public bool mixingInline { set; get; }
            public int mixingBatches { set; get; }
            public string mixingBT { set; get; }
            public List<Equipment> equipList { set; get; }
        }

        public Generate(Schedule2 sch, string filename)
        {
            InitializeComponent();
            this.filename = filename;
            this.sch = sch;

            juices = sch.inprogress;    //TODO:when null, error

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

            DataGridTextColumn colType = new DataGridTextColumn();
            colType.Header = "Type ID";
            colType.Binding = new Binding("type");
            dg_Juices.Columns.Add(colType);

            DataGridTextColumn colBatchFill = new DataGridTextColumn();
            colBatchFill.Header = "Batches Filled";
            colBatchFill.Binding = new Binding("batchFilled");
            dg_Juices.Columns.Add(colBatchFill);

            DataGridTextColumn colBatchTotal = new DataGridTextColumn();
            colBatchTotal.Header = "Batches Total";
            colBatchTotal.Binding = new Binding("batchTotal");
            dg_Juices.Columns.Add(colBatchTotal);

            DataGridTextColumn colFilling = new DataGridTextColumn();
            colFilling.Header = "Filling";
            colFilling.Binding = new Binding("filling");
            dg_Juices.Columns.Add(colFilling);

            DataGridTextColumn colFillInline = new DataGridTextColumn();
            colFillInline.Header = "Filling Inline";
            colFillInline.Binding = new Binding("fillingInline");
            dg_Juices.Columns.Add(colFillInline);

            DataGridTextColumn colFillBatches = new DataGridTextColumn();
            colFillBatches.Header = "Filling Batches";
            colFillBatches.Binding = new Binding("fillingBatches");
            dg_Juices.Columns.Add(colFillBatches);

            DataGridTextColumn colFillTL = new DataGridTextColumn();
            colFillTL.Header = "Filling TL";
            colFillTL.Binding = new Binding("fillingTL");
            dg_Juices.Columns.Add(colFillTL);

            DataGridTextColumn colFillTLTime = new DataGridTextColumn();
            colFillTLTime.Header = "Filling TL Time";
            colFillTLTime.Binding = new Binding("fillingTLTime");
            dg_Juices.Columns.Add(colFillTLTime);

            DataGridTextColumn colFillBT = new DataGridTextColumn();
            colFillBT.Header = "Filling BT";
            colFillBT.Binding = new Binding("fillingBT");
            dg_Juices.Columns.Add(colFillBT);

            DataGridTextColumn colFillBTTime = new DataGridTextColumn();
            colFillBTTime.Header = "Filling BT Time";
            colFillBTTime.Binding = new Binding("fillingBTTime");
            dg_Juices.Columns.Add(colFillBTTime);

            DataGridTextColumn colMixing = new DataGridTextColumn();
            colMixing.Header = "Mixing";
            colMixing.Binding = new Binding("mixing");
            dg_Juices.Columns.Add(colMixing);

            DataGridTextColumn colMixInline = new DataGridTextColumn();
            colMixInline.Header = "Mixing Inline";
            colMixInline.Binding = new Binding("mixingInline");
            dg_Juices.Columns.Add(colMixInline);

            DataGridTextColumn colMixBatches = new DataGridTextColumn();
            colMixBatches.Header = "Mixing Batches";
            colMixBatches.Binding = new Binding("mixingBatches");
            dg_Juices.Columns.Add(colMixBatches);

            DataGridTextColumn colMixBT = new DataGridTextColumn();
            colMixBT.Header = "Mixing BT";
            colMixBT.Binding = new Binding("mixingBT");
            dg_Juices.Columns.Add(colMixBT);

            DataGridTextColumn colEquip = new DataGridTextColumn();
            colEquip.Header = "Equipment";
            colEquip.Binding = new Binding("equip");
            dg_Equip_Juice.Columns.Add(colEquip);

            DataGridTextColumn colEquipTime = new DataGridTextColumn();
            colEquipTime.Header = "Time";
            colEquipTime.Binding = new Binding("time");
            dg_Equip_Juice.Columns.Add(colEquipTime);

            foreach (DataGridTextColumn col in dg_Juices.Columns)
            {
                col.CanUserSort = false;
                col.CanUserReorder = false;
            }

            foreach (DataGridTextColumn col in dg_Equip_Juice.Columns)
            {
                col.CanUserSort = false;
                col.CanUserReorder = false;
            }

            foreach (Juice juice in juices)
            {
                if (juice.type == -1)
                {
                    continue;
                }

                dg_Juices.Items.Add(new JuiceList
                {
                    juice = juice.name,
                    start = juice.starter,
                    time = juice.OGFillTime,
                    type = get_Type(juice.name),
                    batchFilled = juice.batchesFilled,
                    batchTotal = juice.totalBatches,
                    filling = juice.filling,
                    fillingInline = juice.fillingInline,
                    fillingBatches = juice.fillingSlurry,
                    fillingTL = "",//juice.fillingTransferLine.name,
                    fillingTLTime = juice.finishedWithTransferLine,
                    fillingBT = "",//juice.fillingTank.name,
                    fillingBTTime = juice.mixingDoneBlending,
                    mixing = juice.mixing,
                    mixingInline = juice.mixingInline,
                    mixingBatches = juice.mixingSlurry,
                    mixingBT = ""//juice.mixingTank.name
                });

                juice.mixingEquipment = new List<Equipment>();
            }

            fill_Juice_Dropdown(cb_Juice_Type);
            fill_TL_Dropdown(cb_Transfer_Line);
            fill_BT_Dropdown(cb_Blend_Tank_Fill);
            fill_BT_Dropdown(cb_Blend_Tank_Mix);
            fill_Equip_Dropdown(cb_Equip_Juice);
        }

        private void fill_Equip_Dropdown(ComboBox cb)
        {
            foreach (Equipment extra in sch.extras)
            {
                cb.Items.Add(extra.name);
            }

            foreach (Equipment system in sch.systems)
            {
                cb.Items.Add(system.name);
            }
        }

        private void fill_Thaw_Dropdown(ComboBox cb)
        {
            foreach (Juice juice in juices)
            {
                if (juice.type == -1)
                {
                    continue;
                }
                cb.Items.Add(new JuicePair() { name = juice.name, type = juice.type });
            }
        }

        private void fill_BT_Dropdown(ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_BlendTanks]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "Mix Tank";
                cb.SelectedValuePath = "Mix Tank";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void fill_TL_Dropdown(ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_TransferLines]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "Transfer Lines";
                cb.SelectedValuePath = "Transfer Lines";
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

        private void fill_Cleaning_Dropdown(ComboBox cb)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_CleanType_List]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cb.ItemsSource = dt.DefaultView;
                cb.DisplayMemberPath = "CleaningName";
                cb.SelectedValuePath = "id";
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void tc_Home_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            restart();
        }

        private void btn_Next_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_NextToThaw_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            foreach (Juice juice in juices)
            {
                if (juice.starter == true)
                {
                    juice.UpdateStarterJuice(sch.scheduleID);
                }

                else if (juice.type != -1)
                {
                    juice.UpdateStandardJuice();
                }
            }

            DataGridTextColumn colName = new DataGridTextColumn();
            colName.Header = "Juices";
            colName.Binding = new Binding("name");
            dg_Thaw.Columns.Add(colName);

            DataGridTextColumn colJuice = new DataGridTextColumn();
            colJuice.Header = "Juice Type";
            colJuice.Binding = new Binding("juice");
            dg_Thaw.Columns.Add(colJuice);

            DataGridTextColumn colStart = new DataGridTextColumn();
            colStart.Header = "Start Time";
            colStart.Binding = new Binding("start");
            dg_Thaw.Columns.Add(colStart);

            DataGridTextColumn colStop = new DataGridTextColumn();
            colStop.Header = "Stop Time";
            colStop.Binding = new Binding("stop");
            dg_Thaw.Columns.Add(colStop);

            foreach (DataGridTextColumn col in dg_Thaw.Columns)
            {
                col.CanUserSort = false;
                col.CanUserReorder = false;
            }

            fill_Juice_Dropdown(cb_Thaw_Juice);

            tc_Home.SelectedIndex = 1;
            Mouse.OverrideCursor = null;
        }

        private void btn_BackToThaw_Click(object sender, RoutedEventArgs e)
        {
            restart();
        }

        private void btn_NextToEquip_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            DataGridTextColumn colEquip = new DataGridTextColumn();
            colEquip.Header = "Equipment";
            colEquip.Binding = new Binding("equip");
            dg_Equip.Columns.Add(colEquip);

            DataGridTextColumn colState = new DataGridTextColumn();
            colState.Header = "State";
            colState.Binding = new Binding("state");
            dg_Equip.Columns.Add(colState);

            DataGridTextColumn colJuice = new DataGridTextColumn();
            colJuice.Header = "Juice";
            colJuice.Binding = new Binding("juice");
            dg_Equip.Columns.Add(colJuice);

            DataGridTextColumn colCleanType = new DataGridTextColumn();
            colCleanType.Header = "Clean Type";
            colCleanType.Binding = new Binding("clean");
            dg_Equip.Columns.Add(colCleanType);

            DataGridTextColumn colCleanName = new DataGridTextColumn();
            colCleanName.Header = "Clean Name";
            colCleanName.Binding = new Binding("cleanName");
            dg_Equip.Columns.Add(colCleanName);

            DataGridTextColumn colTime = new DataGridTextColumn();
            colTime.Header = "Time";
            colTime.Binding = new Binding("time");
            dg_Equip.Columns.Add(colTime);

            foreach (DataGridTextColumn col in dg_Equip.Columns)
            {
                col.CanUserSort = false;
                col.CanUserReorder = false;
            }

            fill_Thaw_Dropdown(cb_Equipment_Juice);
            fill_Cleaning_Dropdown(cb_Equipment_Clean);

            foreach (Equipment line in sch.transferLines)
            {
                if (line.schedule.Count == 0)
                {
                    dg_Equip.Items.Add(new EquipList
                    {
                        equip = line.name,
                        state = line.state,
                        juice = line.juiceType,
                        clean = line.cleanType,
                        cleanName = line.cleaning,
                        time = line.cleanTime
                    });
                }
            }

            foreach (Equipment aseptic in sch.aseptics)
            {
                if (aseptic.schedule.Count == 0)
                {
                    dg_Equip.Items.Add(new EquipList
                    {
                        equip = aseptic.name,
                        state = aseptic.state,
                        juice = aseptic.juiceType,
                        clean = aseptic.cleanType,
                        cleanName = aseptic.cleaning,
                        time = aseptic.cleanTime
                    });
                }
            }

            foreach (Equipment tank in sch.tanks)
            {
                if (tank.schedule.Count == 0)
                {
                    dg_Equip.Items.Add(new EquipList
                    {
                        equip = tank.name,
                        state = tank.state,
                        juice = tank.juiceType,
                        clean = tank.cleanType,
                        cleanName = tank.cleaning,
                        time = tank.cleanTime
                    });
                }
            }

            foreach (Equipment extra in sch.extras)
            {
                if (extra.schedule.Count == 0)
                {
                    dg_Equip.Items.Add(new EquipList
                    {
                        equip = extra.name,
                        state = extra.state,
                        juice = extra.juiceType,
                        clean = extra.cleanType,
                        cleanName = extra.cleaning,
                        time = extra.cleanTime
                    });
                }
            }

            foreach (Equipment system in sch.systems)
            {
                if (system.schedule.Count == 0)
                {
                    dg_Equip.Items.Add(new EquipList
                    {
                        equip = system.name,
                        state = system.state,
                        juice = system.juiceType,
                        clean = system.cleanType,
                        cleanName = system.cleaning,
                        time = system.cleanTime
                    });
                }
            }

            tc_Home.SelectedIndex = 2;
            Mouse.OverrideCursor = null;
        }

        private void btn_BackToJuice_Click(object sender, RoutedEventArgs e)
        {
            restart();
        }

        private void restart()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to restart schedule generation?", "Restart Schedule", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    Generate form = new Generate(sch, filename);
                    form.Show();
                    Close();
                    break;
                case MessageBoxResult.No:
                    break;
            }
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
            check_Juice_Filling();
        }

        private void chck_Juice_Mix_Click(object sender, RoutedEventArgs e)
        {
            check_Juice_Mixing();
        }

        private void chck_Inline_Fill_Click(object sender, RoutedEventArgs e)
        {
            check_Inline_Filling();
        }

        private void chck_Inline_Mix_Click(object sender, RoutedEventArgs e)
        {
            check_Inline_Mixing();
        }

        private void check_Juice_Filling()
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

        private void check_Juice_Mixing()
        {
            if (chck_Juice_Mix.IsChecked == true)
            {
                chck_Inline_Mix.IsEnabled = true;
                cb_Blend_Tank_Mix.IsEnabled = true;
            }

            else
            {
                chck_Inline_Mix.IsChecked = false;
                tb_Batches_Mix.IsEnabled = false;
                chck_Inline_Mix.IsEnabled = false;
                cb_Blend_Tank_Mix.IsEnabled = false;
            }
        }

        private void check_Inline_Filling()
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

        private void check_Inline_Mixing()
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

        private string get_Juice_Name(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.juice.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private int get_Juice_Type(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return -1;
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return Convert.ToInt32(row.type);
            }

            catch (Exception ex)
            { return -1; }
        }

        private int get_Type(string pseudo)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_Pseudo_Juice]";
                cmd.Parameters.Add("pseudo", SqlDbType.VarChar).Value = pseudo;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return Convert.ToInt32(dt.Rows[0]["Juice"]);
            }

            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return -1;
            }
        }

        private string get_Juice_Time(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.time.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private bool get_Starter(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return Convert.ToBoolean(row.start);
            }

            catch (Exception ex)
            { return false; }
        }

        private string get_Batches_Filled(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.batchFilled.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_Batches_Total(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.batchTotal.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private bool get_Filling(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return Convert.ToBoolean(row.filling);
            }

            catch (Exception ex)
            { return false; }
        }

        private bool get_Filling_Inline(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return Convert.ToBoolean(row.fillingInline);
            }

            catch (Exception ex)
            { return false; }
        }

        private string get_Filling_Batches(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.fillingBatches.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_TL(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.fillingTL.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_TL_Time(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.fillingTLTime.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_BT_Fill(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.fillingBT.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_BT_Time(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.fillingBTTime.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private bool get_Mixing(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return Convert.ToBoolean(row.mixing);
            }

            catch (Exception ex)
            { return false; }
        }

        private bool get_Mixing_Inline(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return false;
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return Convert.ToBoolean(row.mixingInline);
            }

            catch (Exception ex)
            { return false; }
        }

        private string get_Mixing_Batches(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.mixingBatches.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private string get_BT_Mix(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                JuiceList row = (JuiceList)dg.SelectedItems[0];
                return row.mixingBT.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private List<Equipment> get_Equip_Juice(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return null;
                }

                Juice row = juices[dg.SelectedIndex];
                return row.mixingEquipment;
            }

            catch (Exception ex)
            { return null; }
        }

        private void dg_Juices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chck_Start_Juice.IsChecked = get_Starter(dg_Juices);
            tb_Juice_Time.Text = get_Juice_Time(dg_Juices);
            cb_Juice_Type.SelectedValue = get_Juice_Type(dg_Juices);
            tb_Batches_Filled.Text = get_Batches_Filled(dg_Juices);
            tb_Batches_Total.Text = get_Batches_Total(dg_Juices);
            chck_Juice_Filling.IsChecked = get_Filling(dg_Juices);
            chck_Inline_Fill.IsChecked = get_Filling_Inline(dg_Juices);
            tb_Batches_Fill.Text = get_Filling_Batches(dg_Juices);
            cb_Transfer_Line.Text = get_TL(dg_Juices);
            tb_TL_Duration.Text = get_TL_Time(dg_Juices);
            cb_Blend_Tank_Fill.Text = get_BT_Fill(dg_Juices);
            tb_BT_Duration.Text = get_BT_Time(dg_Juices);
            chck_Juice_Mix.IsChecked = get_Mixing(dg_Juices);
            chck_Inline_Mix.IsChecked = get_Mixing_Inline(dg_Juices);
            tb_Batches_Mix.Text = get_Mixing_Batches(dg_Juices);
            cb_Blend_Tank_Mix.Text = get_BT_Mix(dg_Juices);

            check_Juice_Filling();
            check_Juice_Mixing();
            check_Inline_Filling();
            check_Inline_Mixing();

            refresh_Equip_Juice();
        }

        private void refresh_Equip_Juice()
        {
            List<Equipment> equipment = get_Equip_Juice(dg_Juices);
            if (equipment == null)
            {
                return;
            }

            dg_Equip_Juice.Items.Clear();
            foreach (Equipment equip in equipment)
            {
                dg_Equip_Juice.Items.Add(new EquipJuice
                {
                    equip = equip.name,
                    time = equip.endMixing
                });
            }
        }

        private void btn_Save_Juice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dg_Juices.SelectedItems.Count == 0)
                {
                    return;
                }

                JuiceList row = (JuiceList)dg_Juices.SelectedItems[0];
                Juice curr = juices[dg_Juices.SelectedIndex];
                row.start = (bool)chck_Start_Juice.IsChecked;
                row.time = Convert.ToDateTime(tb_Juice_Time.Text);
                row.type = Convert.ToInt32(cb_Juice_Type.SelectedValue);
                row.batchFilled = Convert.ToInt32(tb_Batches_Filled.Text);
                row.batchTotal = Convert.ToInt32(tb_Batches_Total.Text);
                row.filling = (bool)chck_Juice_Filling.IsChecked;
                row.fillingInline = (bool)chck_Inline_Fill.IsChecked;
                row.fillingBatches = Convert.ToInt32(tb_Batches_Fill.Text);
                row.fillingTL = cb_Transfer_Line.Text;
                row.fillingTLTime = Convert.ToDateTime(tb_TL_Duration.Text);
                row.fillingBT = cb_Blend_Tank_Fill.Text;
                row.fillingBTTime = Convert.ToDateTime(tb_TL_Duration.Text);
                row.mixing = (bool)chck_Juice_Mix.IsChecked;
                row.mixingInline = (bool)chck_Inline_Mix.IsChecked;
                row.mixingBatches = Convert.ToInt32(tb_Batches_Mix.Text);
                row.mixingBT = cb_Blend_Tank_Mix.Text;

                dg_Juices.Items.Refresh();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_AddEquip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dg_Juices.SelectedItems.Count == 0 || String.IsNullOrEmpty(tb_Equip_Duration.Text))
                {
                    return;
                }

                Juice row = juices[dg_Juices.SelectedIndex];

                Equipment temp = null;
                for (int i = 0; i < sch.systems.Count; i++)
                {
                    if (cb_Equip_Juice.Text == sch.systems[i].name)
                    {
                        sch.systems[i].endMixing = Convert.ToDateTime(tb_Equip_Duration.Text);
                        temp = sch.systems[i];
                    }
                }

                if (temp == null)
                {
                    for (int i = 0; i < sch.extras.Count; i++)
                    {
                        if (cb_Equip_Juice.Text == sch.extras[i].name)
                        {
                            sch.extras[i].endMixing = Convert.ToDateTime(tb_Equip_Duration.Text);
                            temp = sch.extras[i];
                        }
                    }
                }

                row.mixingEquipment.Add(temp);
                refresh_Equip_Juice();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_AddToThaw_Click(object sender, RoutedEventArgs e)
        {
            if (cb_Thaw_Juice.SelectedIndex == -1 ||
                String.IsNullOrEmpty(tb_Thaw_Start.Text) ||
                String.IsNullOrEmpty(tb_Thaw_Stop.Text))
            {
                return;
            }

            dg_Thaw.Items.Add(new ThawList
            {
                name = cb_Thaw_Juice.Text,
                juice = Convert.ToInt32(cb_Thaw_Juice.SelectedValue),
                start = Convert.ToDateTime(tb_Thaw_Start.Text),
                stop = Convert.ToDateTime(tb_Thaw_Stop.Text)                
            });

            dg_Thaw.Items.Refresh();
        }

        private void btn_RemoveFromThaw_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Thaw.SelectedItems.Count == 0)
            {
                return;
            }

            dg_Thaw.Items.RemoveAt(dg_Thaw.SelectedIndex);
            dg_Thaw.Items.Refresh();
        }

        private void dg_Thaw_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            sch.GenerateNewSchedule();
            MessageBox.Show(sch.message, "Status");
        }

        private void cb_Equipment_State_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btn_Save_Equip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dg_Equip.SelectedItems.Count == 0)
                {
                    return;
                }

                EquipList row = (EquipList)dg_Equip.SelectedItems[0];
                row.state = Convert.ToInt32(cb_Equipment_State.SelectedValue);

                if (row.state == 0 || row.state == 1)
                {
                    row.juice = -1;
                }
                else
                {
                    row.juice = get_Type(cb_Equipment_Juice.Text);
                }

                if (row.state == 0 || row.state == 1 || row.state == 3)
                {
                    row.clean = -1;
                }
                else
                {
                    row.clean = Convert.ToInt32(cb_Equipment_Clean.SelectedValue);
                }
                
                if (row.state == 0 || row.state == 1 || row.state == 2 || row.state == 3)
                {
                    row.time = DateTime.MinValue;
                }
                else
                {
                    row.time = Convert.ToDateTime(tb_Equipment_Time.Text);
                }

                if (row.state == 4)
                {
                    row.cleanName = cb_Equipment_Clean.Text;
                }

                dg_Equip.Items.Refresh();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private int get_Equipment_State(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return -1;
                }

                EquipList row = (EquipList)dg.SelectedItems[0];
                return Convert.ToInt32(row.state);
            }

            catch (Exception ex)
            { return -1; }
        }

        private int get_Equipment_Juice(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return -1;
                }

                EquipList row = (EquipList)dg.SelectedItems[0];
                return Convert.ToInt32(row.juice);
            }

            catch (Exception ex)
            { return -1; }
        }

        private int get_Equipment_Clean(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return -1;
                }

                EquipList row = (EquipList)dg.SelectedItems[0];
                return Convert.ToInt32(row.clean);
            }

            catch (Exception ex)
            { return -1; }
        }

        private string get_Equipment_Time(DataGrid dg)
        {
            try
            {
                if (dg.SelectedItems.Count == 0)
                {
                    return "";
                }

                EquipList row = (EquipList)dg.SelectedItems[0];
                return row.time.ToString();
            }

            catch (Exception ex)
            { return ""; }
        }

        private void dg_Equip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cb_Equipment_State.SelectedValue = get_Equipment_State(dg_Equip);
            cb_Equipment_Juice.SelectedValue = get_Equipment_Juice(dg_Equip);
            cb_Equipment_Clean.SelectedValue = get_Equipment_Clean(dg_Equip);
            tb_Equipment_Time.Text = get_Equipment_Time(dg_Equip);
        }
    }
}
