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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFScreenshot.window;

using static WPFScreenshot.C_視窗拖曳改變大小;

namespace WPFScreenshot {
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window {






        public C_setting SET;
        public String s_快速鍵 = "PrtScrSysRq";//快速鍵預設值



        public W_設定 w_設定;
        public C_頁籤拖曳 c_分頁;
        C_右下角圖示 c_右下角圖示;
        public String s_筆記路徑 = "imgs";
        WebBrowserOverlay wbo;
        C_視窗拖曳改變大小 c_視窗改變大小;

        public double d_解析度比例_x = 1;
        public double d_解析度比例_y = 1;


        public System.Windows.Forms.WebBrowser web_資料夾;

        public MainWindow() {


            InitializeComponent();

            C_Adapter.Initialize();

            c_視窗改變大小 = new C_視窗拖曳改變大小(this);
            this.SourceInitialized += new System.EventHandler(c_視窗改變大小.MainWindow_SourceInitialized);//右下角拖曳


            //初始化web
            wbo = new WebBrowserOverlay(border_web);
            web_資料夾 = wbo.WebBrowser;


            //初始化頁籖物件
            Action<String> ac_點擊 = new Action<String>((String x) => {
                String s_path = func_exe_path() + "\\" + s_筆記路徑 + "\\" + x;
                if (Directory.Exists(s_path) == false) {
                    Directory.CreateDirectory(s_path);               
                }
                web_資料夾.Navigate(s_path);
            });
            Action<String> ac_切換 = new Action<String>((String x) => {
            });
            c_分頁 = new C_頁籤拖曳(stackPanel_1);
            c_分頁.ac_change = ac_切換;
            c_分頁.ac_click = ac_點擊;



            c_右下角圖示 = new C_右下角圖示(this);
            c_右下角圖示.func_隱藏();



            SET = new C_setting(this);
            SET.fun_開啟程式時讀取上次設定(0);





            //如果資料夾不存在，就新建
            if (Directory.Exists(func_exe_path() + "\\" + s_筆記路徑) == false) {
                //新增資料夾
                Directory.CreateDirectory(func_exe_path() + "\\" + s_筆記路徑 + "\\" + "New Folder 1");
            }


            //讀取
            List<String> ar_dir = new List<string>();

            //之前儲存的設定值
            foreach (var item in SET.s_資料夾順序.Split('\t')) {
                if (item.Trim() != "")
                    if (Directory.Exists(func_exe_path() + "\\" + s_筆記路徑 + "\\" + item.Trim())) {
                        ar_dir.Add(item);
                    }
            }

            //實際存在的資料夾
            foreach (var item in Directory.GetDirectories(func_exe_path() + "\\" + s_筆記路徑)) {
                // String s2 = System.IO.Path.GetFileNameWithoutExtension(item);
                String s2 = System.IO.Path.GetFileName(item);
                if (Directory.Exists(item))
                    if (ar_dir.Contains(s2) == false) {
                        ar_dir.Add(s2);
                    }
         
            }



            //如果沒有子資料夾
            if (ar_dir.Count == 0) {
                //新增資料夾
                Directory.CreateDirectory(func_exe_path() + "\\" + s_筆記路徑 + "\\" + "New Folder 1");
                ar_dir.Add("New Folder 1");
            }

            foreach (var item in ar_dir) {
                var bu = new U_分頁_item {
                    Text = item
                };
                bu.func_初始化(this);
                c_分頁.fun_addEvent(bu);

                if (item == SET.s_目前選取的資料夾) {
                    c_分頁.fun_SetSelect(bu);
                }
            }


            if (c_分頁.b_but_text == null) {
                c_分頁.fun_SetSelect((U_分頁_item)stackPanel_1.Children[0]);
            }

            //設定目前選取的頁面
            /*if (stackPanel_1.Children.Count > 0) {
                c_分頁.fun_SetSelect((U_分頁_item)stackPanel_1.Children[0]);
            }*/




            //讓視窗可以拖曳
            this.MouseLeftButtonDown += (sender, e) => {
                try {
                    c_視窗改變大小.ResizeWindow(ResizeDirection.Move);//拖曳視窗
                } catch { }
            };
            scroll_分頁捲軸容器.MouseLeftButtonDown += (sender, e) => {
                var obj = e.OriginalSource;
                if (obj == scroll_分頁捲軸容器)
                    try {

                        this.DragMove();
                    } catch { }
            };

            //雙擊全螢幕
            dockPanel_功能列.MouseDoubleClick += (sender, e) => {
                if (e.RightButton == MouseButtonState.Pressed) {
                    return;
                }
                var obj = e.OriginalSource;
                System.Console.WriteLine(obj);
                if (obj is Border || obj is System.Windows.Shapes.Path) {
                    return;
                }
                if (WindowState != WindowState.Maximized)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            };
            lab_標題列.MouseDoubleClick += (sender, e) => {
                if (e.RightButton == MouseButtonState.Pressed) {
                    return;
                }
                if (WindowState != WindowState.Maximized)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            };

            //讓工具列可以水平捲動
            ((UIElement)dockPanel_功能列.Content).MouseWheel += new MouseWheelEventHandler((object sender, MouseWheelEventArgs e) => {
                int x = e.Delta;
                if (x > 0)
                    dockPanel_功能列.LineLeft();
                else
                    dockPanel_功能列.LineRight();
            });








            this.Closing += (sender, e) => {

                if (Directory.Exists(func_exe_path() + "\\" + s_筆記路徑) == false) {
                    //新增資料夾
                    Directory.CreateDirectory(func_exe_path() + "\\" + s_筆記路徑);
                }

                SET.fun_儲存設定();
            };








            but_截圖.Click += (sender, e) => {
                func_截圖();
            };


            but_清空.Click += (sender, e) => {
                func_刪除目前資料夾();
            };


            but_重新整理.Click += (sender, e) => {
                func_重新整理資料夾();
            };

            but_開啟資料夾.Click += (sender, e) => {
                func_開啟資料夾();
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


            //鎖定視窗在最上層
            but_鎖定視窗.Click += (sender, e) => {
                func_視窗最上層(this, "auto");
            };


            but_縮小至右下角.Click += (sender, e) => {
                func_縮小至右下角(true);
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
        public void func_縮小至右下角(Boolean bool_啟用縮小) {


            if (bool_啟用縮小) {

                this.WindowState = WindowState.Normal;//先 視窗化

                this.ShowInTaskbar = false;
                this.Visibility = Visibility.Collapsed;
                c_右下角圖示.func_顯示();
                wbo.Visibility = Visibility.Collapsed;

            } else {

                this.ShowInTaskbar = true;
                this.Visibility = Visibility.Visible;
                c_右下角圖示.func_隱藏();
                wbo.Visibility = Visibility.Visible;
            }

        }



        /// <summary>
        /// 
        /// </summary>
        public void func_截圖() {

            new W_截圖(this).Show();

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void func_視窗最上層(MainWindow m, String type) {

            bool bool_目前顯示 = true;
            if (type == null || type.ToLower() == "auto" || type == "") {//自動
                bool_目前顯示 = !m.Topmost;
            }
            if (type.ToLower() == "false") {//手動強制
                bool_目前顯示 = false;
            }

            if (type.ToLower() == "no") {
                bool_目前顯示 = m.Topmost;
            }


            if (bool_目前顯示 == false) {
                m.but_鎖定視窗.Background = new SolidColorBrush { Color = Color.FromScRgb(0, 0, 0, 0) };
                m.Topmost = false;
            } else {
                m.but_鎖定視窗.Background = new SolidColorBrush { Color = Color.FromScRgb(0.2f, 1, 1, 1) };
                m.Topmost = true;
            }
        }






        /// <summary>
        /// 
        /// </summary>
        public void func_重新整理資料夾() {
            if (Directory.Exists(func_目前資料夾()) == false) {
                Directory.CreateDirectory(func_目前資料夾()); //新增資料夾
            }
            try {
                web_資料夾.Navigate(func_目前資料夾());
            } catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        public void func_開啟資料夾() {
            if (Directory.Exists(func_目前資料夾()) == false) {//如果資料夾不存在就新增
                Directory.CreateDirectory(func_目前資料夾());
            }
            try {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + func_目前資料夾().Replace("/", "\\") + "\"");
            } catch { }
        }


        /// <summary>
        /// 
        /// </summary>
        public void func_刪除目前資料夾() {

            var w = new W_對話(this);
            w.set_text("\n確定要把「" + c_分頁.b_but_text.Text + "」資料夾\n丟到「資源垃圾桶」？\n");
            w.set_yes(() => {


                //把資料夾移到垃圾桶，並顯示詢問視窗
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(func_目前資料夾(),
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                c_分頁.fun_delete();

                //如果已經買沒有子資料夾，就新增一個
                if (stackPanel_1.Children.Count == 0) {
                    String str_新檔名 = func_exe_path() + "/" + s_筆記路徑 + "/" + "New Folder 1";

                    if (Directory.Exists(str_新檔名) == false) {
                        Directory.CreateDirectory(str_新檔名);
                    }

                    U_分頁_item bt = new U_分頁_item() {
                        Text = "New Folder 1"
                    };
                    bt.func_初始化(this);
                    c_分頁.fun_addEvent(bt);
                    c_分頁.fun_SetSelect(bt);//設定為選取狀態
                }

            });
            w.fun_how(this);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String func_目前資料夾() {
            return func_exe_path() + "\\" + s_筆記路徑 + "\\" + c_分頁.b_but_text.Text;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

            var c_毛玻璃 = new C_window_AERO();
            c_毛玻璃.func_啟用毛玻璃(this);


            new C_全域按鍵偵測(this);

            //取得解析度偏差
            PresentationSource source = PresentationSource.FromVisual(this);
            d_解析度比例_x = source.CompositionTarget.TransformToDevice.M11;
            d_解析度比例_y = source.CompositionTarget.TransformToDevice.M22;

        }






        private void but_分頁_add_Click(object sender, RoutedEventArgs e) {

            W_頁籤新增 w = new W_頁籤新增(this);
            w.fun_新增();
            w.Show();
            w.Owner = this;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String func_exe_path() {

            return System.Windows.Forms.Application.StartupPath;

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="bol"></param>
        public void fun_鎖定視窗(Boolean bol) {

            //如果視窗已經縮小至右下角，則不啟用
            if (this.ShowInTaskbar == false)
                return;

            if (bol) {
                this.IsEnabled = false;
                wbo.Visibility = Visibility.Collapsed;
                //this.Opacity = 0.9;

            } else {
                this.IsEnabled = true;
                wbo.Visibility = Visibility.Visible;
                //this.Opacity = 1;
            }


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
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="s_移動方式"></param>
        public static void fun_動畫(FrameworkElement f, double from, double to, String s_移動方式, Action ac) {

            if (f == null)
                return;

            s_移動方式 = s_移動方式.ToUpper();


            //位移
            Storyboard storyboard2 = new Storyboard();
            DoubleAnimation growAnimation2 = new DoubleAnimation();
            growAnimation2.Duration = (Duration)TimeSpan.FromSeconds(0.15f);

            growAnimation2.Completed += (sender, e) => {//完成時執行
                ac();
                //f.Visibility = Visibility.Collapsed;
            };

            f.RenderTransform = new TranslateTransform();

            growAnimation2.From = from;
            growAnimation2.To = to;

            Storyboard.SetTargetProperty(growAnimation2, new PropertyPath("(FrameworkElement.RenderTransform).(TranslateTransform." + s_移動方式 + ")"));
            Storyboard.SetTarget(growAnimation2, f);

            storyboard2.Children.Add(growAnimation2);
            storyboard2.Begin();

            //----------------------

            Storyboard storyboard3 = new Storyboard();
            DoubleAnimation growAnimation3 = new DoubleAnimation();
            growAnimation3.Duration = (Duration)TimeSpan.FromSeconds(0.15f);

            growAnimation3.Completed += (sender, e) => {//完成時執行

                //f.Visibility = Visibility.Collapsed;
            };

            //f.RenderTransform = new TranslateTransform();

            growAnimation3.From = 0.5;
            growAnimation3.To = 1;

            Storyboard.SetTargetProperty(growAnimation3, new PropertyPath("Opacity"));
            Storyboard.SetTarget(growAnimation3, f);

            storyboard3.Children.Add(growAnimation3);
            storyboard3.Begin();

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
