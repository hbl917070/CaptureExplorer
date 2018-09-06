using System;
using System.Collections.Generic;
using System.IO;
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

namespace WPFScreenshot.window {
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


            //初始化快速鍵
            textBox_截圖快速鍵.Text = M.s_快速鍵;
            textBox_截圖快速鍵_全螢幕.Text = M.s_快速鍵_全螢幕;
            textBox_截圖快速鍵_目前視窗.Text = M.s_快速鍵_目前視窗;

            text_自定儲存路徑.Text = M.s_自定儲存路徑;

            if (M.bool_自定儲存路徑) {
                radio_儲存路徑_自定.IsChecked = true;
            } else {
                radio_儲存路徑_預設.IsChecked = true;
            }

            text_自定儲存路徑.TextChanged += (sender, e) => {
                M.s_自定儲存路徑 = text_自定儲存路徑.Text;
                if (Directory.Exists(M.s_自定儲存路徑)&& radio_儲存路徑_自定.IsChecked.Value) {
                    M.func_分頁重新整理();
                }
            };
            radio_儲存路徑_自定.Checked += (sender, e) => {
                M.bool_自定儲存路徑 = true;
                if (Directory.Exists(M.s_自定儲存路徑) && radio_儲存路徑_自定.IsChecked.Value) {
                    M.func_分頁重新整理();
                }
            };
            radio_儲存路徑_預設.Checked += (sender, e) => {
                M.bool_自定儲存路徑 = false;
                M.func_分頁重新整理();
            };


            button_截圖快速鍵.Click += (sender, e) => {
                textBox_截圖快速鍵.Text = "";
                M.s_快速鍵 = "";
            };

            button_截圖快速鍵_全螢幕.Click += (sender, e) => {
                textBox_截圖快速鍵_全螢幕.Text = "";
                M.s_快速鍵_全螢幕 = "";
            };
            button_截圖快速鍵_目前視窗.Click += (sender, e) => {
                textBox_截圖快速鍵_目前視窗.Text = "";
                M.s_快速鍵_目前視窗 = "";
            };

            but_小屋.Click += (sender, e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://home.gamer.com.tw/homeindex.php?owner=hbl917070");
                } catch { }
            };
            but_討論區.Click += (sender, e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://forum.gamer.com.tw/C.php?bsn=60076&snA=4337057");
                } catch { }
            };


            but_自定路徑_選資料夾.Click += (sender, e) => {

                var dialog = new FolderSelectDialog {
                    //InitialDirectory = musicFolderTextBox.Text,
                    Title = "Select a folder"
                };
                if (dialog.Show()) {
                    text_自定儲存路徑.Text = dialog.FileName;
                }
            };


            text_關於.Text =
                "版本：2.0.2 (2018 / 09 / 07)" + "\n" +
                "作者：hbl917070 (深海異音)" + "\n" +
                "信箱：hbl917070@gmail.com";

            this.Closed += (sender, e) => {
                M.w_設定 = null;
            };

            web_說明.Navigate(M.func_exe_path() + "\\data\\explain\\index.html");
            web_說明.IsWebBrowserContextMenuEnabled = false;//禁止右鍵
            web_說明.WebBrowserShortcutsEnabled = false;//禁止快速鍵
            web_說明.AllowNavigation = false;//載入完成後，禁止瀏覽其他網頁

        }
    }
}
