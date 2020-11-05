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
    }
}
