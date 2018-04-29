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
using System.Windows.Navigation;
using System.Windows.Shapes;
using 截圖.window;

namespace 截圖 {
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window {



        public double d_記錄視窗位子;
        public String s_資料夾路徑 = "";
        public String s_快速鍵 = "PrintScreen";
        public W_設定 w_設定;
        public C_setting SET;

 


        public MainWindow() {
            InitializeComponent();





            //讓視窗可以拖曳
            this.MouseLeftButtonDown += (sender, e) => {
                try {
                    this.DragMove();
                } catch { }
            };



            ((UIElement)dockPanel_功能列.Content).MouseWheel += new MouseWheelEventHandler((object sender, MouseWheelEventArgs e) => {

                int x = e.Delta;

                if (x > 0)
                    dockPanel_功能列.LineLeft();
                else
                    dockPanel_功能列.LineRight();

            });



            new F_快速鍵偵測(this).Show();
            SET = new C_setting(this);

            SET.fun_開啟程式時讀取上次設定(0);

            this.Closing += (sender, e) => {

                if (Directory.Exists(System.Windows.Forms.Application.StartupPath + "/data") == false) {
                    //新增資料夾
                    Directory.CreateDirectory(System.Windows.Forms.Application.StartupPath + "/data");
                }

                SET.fun_儲存設定();
            };


  


            s_資料夾路徑 = System.Windows.Forms.Application.StartupPath + @"\img";

            if (Directory.Exists(s_資料夾路徑) == false) {
                //新增資料夾
                Directory.CreateDirectory(s_資料夾路徑);
            }

            web_資料夾.Navigate(s_資料夾路徑);



            but_截圖.Click += (sender, e) => {

                if (Top != -5000) {

                    d_記錄視窗位子 = Top;
                    Top = -5000;
                    new W_截圖(this).Show();
                }

            };


            but_清空.Click += (sender, e) => {
                var w = new W_對話(this);
                w.set_text("\n確定要把所有圖片移到『資源垃圾桶』？\n");
                w.set_yes(() => {
                    // 取得資料夾內所有檔案
                    foreach (string fname in System.IO.Directory.GetFiles(s_資料夾路徑)) {

                        try {
                            //File.Delete(fname);
                            //把檔案移到垃圾桶，並顯示詢問視窗
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(fname,
                                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        } catch { }

                    }
                });
                w.fun_how(this);
            };


            but_重新整理.Click += (sender, e) => {
                if (Directory.Exists(s_資料夾路徑) == false) {
                    Directory.CreateDirectory(s_資料夾路徑); //新增資料夾
                }
                try {
                    web_資料夾.Navigate(s_資料夾路徑);
                } catch { }
            };


            but_開啟資料夾.Click += (sender, e) => {
                if (Directory.Exists(s_資料夾路徑) == false) {//如果資料夾不存在就新增
                    Directory.CreateDirectory(s_資料夾路徑);
                }
                try {
                    System.Diagnostics.Process.Start("explorer.exe", s_資料夾路徑);
                } catch { }
            };



            but_設定.Click += (sender, e) => {
                if (w_設定 != null) {
                    w_設定.Close();
                    w_設定 = null;
                }
                w_設定 = new W_設定(this);
                w_設定.Topmost = this.Topmost;
                w_設定.Owner = this;
                w_設定.Show();
            };




            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, this.OnCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, this.OnMaximizeWindow, this.OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, this.OnMinimizeWindow, this.OnCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, this.OnRestoreWindow, this.OnCanResizeWindow));
            //this.Style = new Style( StyleProperty.OwnerType);




            this.Loaded += MainWindow_Loaded;

        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

            //var c_毛玻璃 = new C_window_AERO();
            //c_毛玻璃.func_啟用毛玻璃(this);


        
        }








        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        private void OnCloseWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.CloseWindow(this);
        }

        private void OnMaximizeWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.MaximizeWindow(this);
        }

        private void OnMinimizeWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.RestoreWindow(this);
        }






        /// <summary>
        /// 清理記憶體
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        public void fun_清理記憶體() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }



    }
}
