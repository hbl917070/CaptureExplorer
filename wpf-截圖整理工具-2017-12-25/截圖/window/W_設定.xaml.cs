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
using System.Windows.Shapes;

namespace 截圖.window {
    /// <summary>
    /// W_設定.xaml 的互動邏輯
    /// </summary>
    public partial class W_設定 : Window {

        MainWindow M;

        public W_設定(MainWindow m) {

            this.M = m;

            InitializeComponent();



            //讓視窗可以拖曳
            this.MouseLeftButtonDown += ((object sender, MouseButtonEventArgs e) => {
                try {
                    this.DragMove();
                } catch { }
            });



            textBox_截圖快速鍵.Text = M.s_快速鍵;



            button_截圖快速鍵.Click += (object sender, RoutedEventArgs e) => {
                textBox_截圖快速鍵.Text = "";
                M.s_快速鍵 = "";
            };


            but_小屋.Click += (object sender, RoutedEventArgs e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://home.gamer.com.tw/homeindex.php?owner=hbl917070");
                } catch { }
            };


            but_討論區.Click += (object sender, RoutedEventArgs e) => {
              
                try {
                    System.Diagnostics.Process.Start(@"https://forum.gamer.com.tw/C.php?bsn=60076&snA=4337057");
                } catch { }
            };




            text_關於.Text =
                "版本：2017 / 12 / 25" + "\n" +
                "作者：hbl917070 (深海異音)" + "\n" +
                "信箱：hbl917070@gmail.com";

            this.Closed += (sender, e) => {
                M.w_設定 = null;
            };




        }
    }
}
