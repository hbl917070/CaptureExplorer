using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace WPFScreenshot {
    /// <summary>
    /// W_截圖.xaml 的互動邏輯
    /// </summary>
    public partial class W_截圖 : Window {


        private System.Windows.Forms.Timer t_拖曳中;
        private 拖曳模式 int_拖曳模式 = 拖曳模式.none;
        private double[] d_xywh = { 0, 0, 0, 0 };
        private System.Drawing.Point d_滑鼠xy = new System.Drawing.Point();
        private Boolean bool_初始 = true;

        private System.Drawing.Bitmap bimg = null;

        private int int_螢幕起始坐標_x = 0;
        private int int_螢幕起始坐標_y = 0;
        private double d_解析度比例_x = 1;
        private double d_解析度比例_y = 1;
        private double d_螢幕_w = 0;
        private double d_螢幕_h = 0;

        private MainWindow M;
        private WindowState ws_全螢幕前的狀態;
        private double d_記錄視窗位子 = 0;


        SolidColorBrush color_滑鼠移入 = new SolidColorBrush { Color = Color.FromArgb(255, 100, 250, 255) };
        SolidColorBrush color_原始 = new SolidColorBrush { Color = Color.FromArgb(50, 100, 250, 255) };


        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        public W_截圖(MainWindow m) {


            this.M = m;
            InitializeComponent();
            this.Top = -5000;


            //截圖前記錄視窗狀態
            ws_全螢幕前的狀態 = M.WindowState;
            d_記錄視窗位子 = M.Top;


            //避免全螢幕導致視窗無法隱藏
            M.fun_鎖定視窗(true);
            M.WindowStyle = System.Windows.WindowStyle.None;//無邊框
            M.WindowState = WindowState.Normal;
            M.Top = -5000;


            func_初始化();


            this.Closed += (object sender, EventArgs e) => {
                M.Top = d_記錄視窗位子;
                M.WindowState = ws_全螢幕前的狀態;
                M.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                M.fun_鎖定視窗(false);
            };
        }





        /// <summary>
        /// 
        /// </summary>
        private void func_初始化() {


            lab_size.Visibility = Visibility.Hidden;//隱藏szie資訊

            //初始化顏色
            b_上.Fill = color_原始;
            b_下.Fill = color_原始;
            b_右.Fill = color_原始;
            b_左.Fill = color_原始;
            b_右上.Stroke = color_原始;
            b_右下.Stroke = color_原始;
            b_左上.Stroke = color_原始;
            b_左下.Stroke = color_原始;




            //取得螢幕總其實坐標、總大小
            int_螢幕起始坐標_x = 0;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens) {//列出所有螢幕資訊     
                if (screen.Bounds.X < int_螢幕起始坐標_x)
                    int_螢幕起始坐標_x = screen.Bounds.X;
                if (screen.Bounds.Y < int_螢幕起始坐標_y)
                    int_螢幕起始坐標_y = screen.Bounds.Y;

                int yy = screen.Bounds.Y + screen.Bounds.Height;
                if (yy > d_螢幕_h)
                    d_螢幕_h = yy;

                int xx = screen.Bounds.X + screen.Bounds.Width;
                if (xx > d_螢幕_w)
                    d_螢幕_w = xx;
            }
            d_螢幕_w -= int_螢幕起始坐標_x;
            d_螢幕_h -= int_螢幕起始坐標_y;



            bimg = CaptureScreen();//全螢幕截圖



            //取得解析度偏差
            PresentationSource source = PresentationSource.FromVisual(M);
            d_解析度比例_x = source.CompositionTarget.TransformToDevice.M11;
            d_解析度比例_y = source.CompositionTarget.TransformToDevice.M22;



            this.Background = new ImageBrush(ToBitmapSource(bimg));//設定背景

            this.Focus();

            //程式最大化
            this.Width = d_螢幕_w / d_解析度比例_x;
            this.Height = d_螢幕_h / d_解析度比例_y;
            //var Work = System.Windows.Forms.Screen.GetBounds(new System.Drawing.Point((int)this.Left, (int)this.Top));


            //MessageBox.Show(d_螢幕_h + "\n" + System.Windows.Forms.Cursor.Clip.Size.Height + "");


            this.Left = int_螢幕起始坐標_x;
            this.Top = int_螢幕起始坐標_y;

            //啟動計時器
            t_拖曳中 = new System.Windows.Forms.Timer();
            t_拖曳中.Interval = 10;
            t_拖曳中.Tick += T_Tick;
            t_拖曳中.Start();
            T_Tick(null, null);//立刻執行，避免使用者開啟截圖後立即點擊

            func_重新();

            Rectangle[] ar = { b_中心拖曳, b_左上, b_上, b_右上, b_右, b_右下, b_下, b_左下, b_左 };

            //滑鼠按下
            for (int i = 0; i < ar.Length; i++) {
                ar[i].MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                    st_按鈕群.Visibility = Visibility.Hidden;
                    d_滑鼠xy = func_取得滑鼠();
                    t_拖曳中.Start();
                };
            }


            //滑鼠移入後改變顏色
            for (int i = 0; i < ar.Length; i++) {
                ar[i].MouseEnter += (sender, e) => {
                    if (sender == b_上 || sender == b_右 || sender == b_下 || sender == b_左) {
                        if (t_拖曳中.Enabled == false)
                            ((Rectangle)sender).Fill = color_滑鼠移入;
                    }
                };
                ar[i].MouseLeave += (sender, e) => {
                    if (sender == b_上 || sender == b_右 || sender == b_下 || sender == b_左) {
                        if (t_拖曳中.Enabled == false) {
                            ((Rectangle)sender).Fill = color_原始;
                        }
                    }
                };

                ar[i].MouseEnter += (sender, e) => {
                    if (sender == b_左上 || sender == b_左下 || sender == b_右上 || sender == b_右下) {
                        if (t_拖曳中.Enabled == false)
                            ((Rectangle)sender).Stroke = color_滑鼠移入;
                    }
                };
                ar[i].MouseLeave += (sender, e) => {
                    if (sender == b_左上 || sender == b_左下 || sender == b_右上 || sender == b_右下) {
                        if (t_拖曳中.Enabled == false)
                            ((Rectangle)sender).Stroke = color_原始;
                    }
                };
            }

            b_中心拖曳.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_中心拖曳.Margin.Left, b_中心拖曳.Margin.Top, b_中心拖曳.ActualWidth, b_中心拖曳.ActualHeight };
                int_拖曳模式 = 拖曳模式.中心;
            };

            b_上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_上.Margin.Left, b_上.Margin.Top, b_上.ActualWidth, b_上.ActualHeight };
                int_拖曳模式 = 拖曳模式.上;
            };
            b_下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_下.Margin.Left, b_下.Margin.Top, b_下.ActualWidth, b_下.ActualHeight };
                int_拖曳模式 = 拖曳模式.下;
            };

            b_右.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右.Margin.Left, b_右.Margin.Top, b_右.ActualWidth, b_右.ActualHeight };
                int_拖曳模式 = 拖曳模式.右;
            };
            b_左.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左.Margin.Left, b_左.Margin.Top, b_左.ActualWidth, b_左.ActualHeight };
                int_拖曳模式 = 拖曳模式.左;
            };



            b_左上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左上.Margin.Left, b_左上.Margin.Top, b_左上.ActualWidth, b_左上.ActualHeight };
                int_拖曳模式 = 拖曳模式.左上;
            };
            b_右上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右上.Margin.Left, b_右上.Margin.Top, b_右上.ActualWidth, b_右上.ActualHeight };
                int_拖曳模式 = 拖曳模式.右上;
            };
            b_右下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右下.Margin.Left, b_右下.Margin.Top, b_右下.ActualWidth, b_右下.ActualHeight };
                int_拖曳模式 = 拖曳模式.右下;
            };
            b_左下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左下.Margin.Left, b_左下.Margin.Top, b_左下.ActualWidth, b_左下.ActualHeight };
                int_拖曳模式 = 拖曳模式.左下;
            };



            this.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) => {

                if (transparentRect.Rect.Width == 0 || transparentRect.Rect.Height == 0) {
                    func_重新();
                    return;
                }

                t_拖曳中.Enabled = false;
                rect_游標.Margin = new Thickness(-100, -100, 0, 0);

                fun_顯示按鈕();

            };






            //第一次拖曳
            this.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {

                if (bool_初始) {

                    b_左上.Margin = new Thickness(func_取得滑鼠().X - 10, func_取得滑鼠().Y - 10, 0, 0);

                    d_xywh = new double[] { b_左上.Margin.Left, b_左上.Margin.Top, b_左上.ActualWidth, b_左上.ActualHeight };
                    d_滑鼠xy = func_取得滑鼠();
                    t_拖曳中.Start();
                    int_拖曳模式 = 拖曳模式.右下;
                    bool_初始 = false;
                }
            };


            //按空白鍵 拖曳
            拖曳模式 int_空白鍵記錄 = 拖曳模式.中心;
            Boolean bool_空白鍵記錄 = false;


            this.KeyDown += (object sender, KeyEventArgs e) => {

                if (e.Key == Key.Escape) { //按esc結束

                    func_關閉程式();

                } else if (e.Key == Key.Enter) {
                    fun_確認儲存("png");
                }

                if (int_拖曳模式 == 拖曳模式.none)
                    return;

                //按空白鍵 拖曳
                if (bool_空白鍵記錄 == false)
                    if (e.Key == Key.Space) {
                        bool_空白鍵記錄 = true;
                        d_xywh = new double[] { b_中心拖曳.Margin.Left, b_中心拖曳.Margin.Top, b_中心拖曳.ActualWidth, b_中心拖曳.ActualHeight };
                        d_滑鼠xy = func_取得滑鼠();
                        int_空白鍵記錄 = int_拖曳模式;
                        int_拖曳模式 = 拖曳模式.中心;
                    }

            };
            this.KeyUp += (object sender, KeyEventArgs e) => {


                //按空白鍵 拖曳
                if (e.Key == Key.Space) {
                    bool_空白鍵記錄 = false;

                    Rectangle r = null;
                    if (int_空白鍵記錄 == 拖曳模式.左上) {
                        r = b_左上;
                    } else if (int_空白鍵記錄 == 拖曳模式.右上) {
                        r = b_右上;
                    } else if (int_空白鍵記錄 == 拖曳模式.右下) {
                        r = b_右下;
                    } else if (int_空白鍵記錄 == 拖曳模式.左下) {
                        r = b_左下;
                    } else if (int_空白鍵記錄 == 拖曳模式.上) {
                        r = b_上;
                    } else if (int_空白鍵記錄 == 拖曳模式.下) {
                        r = b_下;
                    } else if (int_空白鍵記錄 == 拖曳模式.右) {
                        r = b_右;
                    } else if (int_空白鍵記錄 == 拖曳模式.左) {
                        r = b_左;
                    } else {

                        return;
                    }

                    d_xywh = new double[] { r.Margin.Left, r.Margin.Top, r.ActualWidth, r.ActualHeight };
                    d_滑鼠xy = func_取得滑鼠();

                    int_拖曳模式 = int_空白鍵記錄;
                    this.Title = "**";
                }

            };


            //右鍵 結束
            this.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => {
                if (int_拖曳模式 == 拖曳模式.none) {

                    bimg.Dispose();
                    bimg = null;

                    this.Close();
                } else {
                    func_重新();
                }
                this.Title = "+++";
            };

            button_關閉.Click += ( sender,  e) => {
                func_關閉程式();
            };

            button_重新.Click += ( sender,  e) => {
                func_重新();
            };

            button_確認_png.Click += ( sender,  e) => {
                fun_確認儲存("png");
            };
            button_確認_jpg.Click += ( sender,  e) => {
                fun_確認儲存("jpg");
            };
            button_確認_copy.Click += ( sender,  e) => {
                fun_確認儲存("copy");
            };

            button_編輯.Click += ( sender,  e) => {
                fun_確認儲存("edit");
            };

            but_複製顏色_16.Click += ( sender,  e) => {
                Clipboard.SetText(lab_複製顏色_16.Content+"");
                func_關閉程式();
            };
            but_複製顏色_rgb.Click += (sender, e) => {
                Clipboard.SetText(lab_複製顏色_rgb.Content + "");
                func_關閉程式();
            };
        }



        /// <summary>
        /// 
        /// </summary>
        private void fun_顯示按鈕() {



            lab_size.Visibility = Visibility.Hidden;//隱藏size資訊


            //避免結束拖曳後，顏色尚未恢復
            b_上.Fill = color_原始;
            b_下.Fill = color_原始;
            b_右.Fill = color_原始;
            b_左.Fill = color_原始;
            b_右上.Stroke = color_原始;
            b_右下.Stroke = color_原始;
            b_左上.Stroke = color_原始;
            b_左下.Stroke = color_原始;


            var rect = transparentRect.Rect;
            var mouse = func_取得滑鼠();

            //判斷是「吸取顏色」，還是「截圖」
            if (rect.Width == 1 && rect.Height == 1) {

                var c = bimg.GetPixel((int)rect.Left, (int)rect.Top);

                lab_複製顏色_16.Content = "#" + Convert.ToString(c.R, 16).ToUpper() + Convert.ToString(c.G, 16).ToUpper() + Convert.ToString(c.B, 16).ToUpper();
                lab_複製顏色_rgb.Content = $"rgb({c.R},{c.G},{c.B})";

                re_顏色預覽.Fill = new SolidColorBrush { Color = Color.FromRgb(c.R, c.G, c.B) };

                button_確認_copy.Visibility = Visibility.Collapsed;
                button_確認_jpg.Visibility = Visibility.Collapsed;
                button_確認_png.Visibility = Visibility.Collapsed;
                button_編輯.Visibility = Visibility.Collapsed;

                st_顏色工具列.Visibility = Visibility.Visible;

            } else {
                button_確認_copy.Visibility = Visibility.Visible;
                button_確認_jpg.Visibility = Visibility.Visible;
                button_確認_png.Visibility = Visibility.Visible;
                button_編輯.Visibility = Visibility.Visible;

                st_顏色工具列.Visibility = Visibility.Collapsed;
            }



            //計算坐標
            int int_邊距 = 10;

            double x = rect.Width + rect.Left + int_邊距;
            double y = rect.Height + rect.Top + int_邊距;

            if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下) {
                x = mouse.X + int_邊距;
            }
            if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右) {
                y = mouse.Y + int_邊距;
            }

            double lw = st_按鈕群.ActualWidth;
            double lh = st_按鈕群.ActualHeight;

            //在矩形選取範圍內
            if (mouse.X + 15 >= rect.Left &&
               mouse.X <= rect.Left + rect.Width - 15) {

                if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下) {
                    x = mouse.X + int_邊距;
                } else {
                    x = rect.Left - lw - int_邊距;
                }
            }

            if (mouse.Y + 15 >= rect.Top &&
              mouse.Y <= rect.Top + rect.Height - 15) {

                if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右) {
                    y = mouse.Y + int_邊距;
                } else {
                    y = rect.Top - lh - int_邊距;
                }
            }

            /*if (int_拖曳模式 == 拖曳模式.中心) {
                 x = rect.Width + rect.Left + int_邊距;
                 y = rect.Height + rect.Top + int_邊距;
            }*/

            //超出螢幕
            if (x + lw + int_邊距 >= this.ActualWidth) {
                x = mouse.X - lw - int_邊距;
            }
            if (y + lh + int_邊距 >= this.ActualHeight) {
                y = mouse.Y - lh - int_邊距;
            }
            if (x < 0) {
                x = mouse.X + int_邊距;
            }
            if (y < 0) {
                y = mouse.Y + int_邊距;
            }


            st_按鈕群.Visibility = Visibility.Visible;
            st_按鈕群.Margin = new Thickness(x, y, 0, 0);
        }



        /// <summary>
        /// 
        /// </summary>
        public void fun_確認儲存(String type) {

            if (int_拖曳模式 == 拖曳模式.none)
                return;

            var t = transparentRect.Rect;


            if ((int)(t.Width) == 0 || (int)(t.Height) == 0)
                return;



            /*System.Drawing.Bitmap img = KiCut(bimg, (int)(t.Left* d_解析度比例_x), (int)(t.Top* d_解析度比例_y),
                (int)(t.Width* d_解析度比例_x), (int)(t.Height* d_解析度比例_y));*/

            System.Drawing.Bitmap img = KiCut(bimg, (int)(t.Left), (int)(t.Top),
         (int)(t.Width), (int)(t.Height));

            //避免 寬度 or 高度 未滿50
            /*double dd = 60 / t.Width;
            if (dd < 60 / t.Height)
                dd = 60 / t.Height;

            double d_放大倍率 = 1;
            if (dd > d_放大倍率)
                d_放大倍率 = dd;

            img = Resize(img, d_放大倍率);*/

            if (Directory.Exists(M.func_目前資料夾()) == false) {
                Directory.CreateDirectory(M.func_目前資料夾());
                M.func_重新整理資料夾();
            }

            String s_儲存路徑 = M.func_目前資料夾() + "/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "." + type;

            if (type == "jpg" || type == "png") {


                //儲存圖片
                func_SaveBitmap(img, type, s_儲存路徑);//存檔


                //自動存入剪貼簿
                try {
                    if (M.checkBox_自動存入剪貼簿.IsChecked.Value) {
                        Clipboard.SetData(DataFormats.Bitmap, img);
                    }
                } catch { }


            } else if (type == "edit") {

               var c_edit_img=  new C_edit_img(new System.Drawing.Bitmap(img), s_儲存路徑);

                //如果原視窗是置頂，就讓編輯視窗也置頂
                if (M.Topmost) {
                    c_edit_img.TopMost = true;
                }

            } else {

                //存入剪貼簿
                try {
                    Clipboard.SetData(DataFormats.Bitmap, img);
                } catch { }
            }


            //清理記憶體
            img.Dispose();
            img = null;


            func_關閉程式();

        }


        /// <summary>
        /// 
        /// </summary>
        void func_關閉程式() {
            bimg.Dispose();
            bimg = null;
            M.fun_清理記憶體();
            this.Close();
        }



        /// <summary>
        /// 圖片縮放
        /// </summary>
        public static System.Drawing.Bitmap Resize(System.Drawing.Bitmap originImage, Double times) {
            int width = Convert.ToInt32(originImage.Width * times);
            int height = Convert.ToInt32(originImage.Height * times);

            return Process(originImage, originImage.Width, originImage.Height, width, height);
        }
        private static System.Drawing.Bitmap Process(System.Drawing.Bitmap originImage, int oriwidth, int oriheight, int width, int height) {
            var resizedbitmap = new System.Drawing.Bitmap(width, height);
            var g = System.Drawing.Graphics.FromImage(resizedbitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(System.Drawing.Color.Transparent);
            g.DrawImage(originImage, new System.Drawing.Rectangle(0, 0, width, height), new System.Drawing.Rectangle(0, 0, oriwidth, oriheight), System.Drawing.GraphicsUnit.Pixel);
            return resizedbitmap;
        }



        /// <summary>
        /// 
        /// </summary>
        public void func_重新() {


            //計算遮罩位置
            transparentRect.Rect = new Rect(0, 0, 0, 0);

            b_右上.Margin = new Thickness(-100, 0, 0, 0);
            b_右下.Margin = new Thickness(-100, 0, 0, 0);
            b_左上.Margin = new Thickness(-100, 0, 0, 0);
            b_左下.Margin = new Thickness(-100, 0, 0, 0);

            b_下.Margin = new Thickness(0, -100, 0, 0);
            b_右.Margin = new Thickness(-100, -100, 0, 0);

            bool_初始 = true;
            int_拖曳模式 = 拖曳模式.none;
            t_拖曳中.Start();
            st_按鈕群.Visibility = Visibility.Hidden;

        }





        /// <summary>
        /// 
        /// </summary>
        public System.Drawing.Point func_取得滑鼠() {

            var Work = System.Windows.Forms.Screen.GetBounds(new System.Drawing.Point((int)this.Left, (int)this.Top));

            //從螢幕的最左上角開始計算，否則多螢幕可能會出錯
            var mmm = System.Windows.Forms.Cursor.Position;
            mmm.X -= int_螢幕起始坐標_x;
            mmm.Y -= int_螢幕起始坐標_y;

            mmm.X = (int)(mmm.X / d_解析度比例_x);
            mmm.Y = (int)(mmm.Y / d_解析度比例_y);


            return mmm;
        }





        /// <summary>
        /// 拖曳、改變大小
        /// </summary>
        private void T_Tick(object sender, EventArgs e) {


            if (int_拖曳模式 == 拖曳模式.none) {//初始狀態（僅顯示十字線）
                b_左.Margin = new Thickness(func_取得滑鼠().X - 10, 0, 0, 0);
                b_上.Margin = new Thickness(0, func_取得滑鼠().Y - 10, 0, 0);

                //讓這個物件跟隨，就會顯示十字游標
                rect_游標.Margin = new Thickness(func_取得滑鼠().X - 30, func_取得滑鼠().Y - 30, 0, 0);

                func_顯示size();

                return;
            }



            if (int_拖曳模式 == 拖曳模式.中心) {//拖曳

                double dou_左中 = b_左.Margin.Left - b_中心拖曳.Margin.Left;
                double dou_右中 = b_右.Margin.Left - b_中心拖曳.Margin.Left;
                double dou_中下 = b_下.Margin.Top - b_中心拖曳.Margin.Top;
                double dou_中上 = b_上.Margin.Top - b_中心拖曳.Margin.Top;


                b_中心拖曳.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );

                b_左.Margin = new Thickness(b_中心拖曳.Margin.Left + dou_左中, 0, 0, 0);
                b_右.Margin = new Thickness(b_中心拖曳.Margin.Left + dou_右中, 0, 0, 0);
                b_下.Margin = new Thickness(0, b_中心拖曳.Margin.Top + dou_中下, 0, 0);
                b_上.Margin = new Thickness(0, b_中心拖曳.Margin.Top + dou_中上, 0, 0);

            }


            if (int_拖曳模式 == 拖曳模式.上) {

                b_上.Margin = new Thickness(0, func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1], 0, 0);
                b_左上.Margin = new Thickness(
                    b_左上.Margin.Left,
                    b_上.Margin.Top,
                    0, 0
                );
                b_右上.Margin = new Thickness(
                    b_右上.Margin.Left,
                    b_上.Margin.Top,
                    0, 0
                );

            } else if (int_拖曳模式 == 拖曳模式.下) {

                b_下.Margin = new Thickness(0, func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1], 0, 0);
                b_右下.Margin = new Thickness(
                      b_右下.Margin.Left,
                      b_下.Margin.Top,
                      0, 0
                );
                b_左下.Margin = new Thickness(
                      b_左下.Margin.Left,
                      b_下.Margin.Top,
                      0, 0
                );

            }


            if (int_拖曳模式 == 拖曳模式.右) {

                b_右.Margin = new Thickness(func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0], 0, 0, 0);
                b_右上.Margin = new Thickness(b_右.Margin.Left, b_右上.Margin.Top, 0, 0);
                b_右下.Margin = new Thickness(b_右.Margin.Left, b_右下.Margin.Top, 0, 0);

            } else if (int_拖曳模式 == 拖曳模式.左) {

                b_左.Margin = new Thickness(func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0], 0, 0, 0);
                b_左上.Margin = new Thickness(b_左.Margin.Left, b_左上.Margin.Top, 0, 0);
                b_左下.Margin = new Thickness(b_左.Margin.Left, b_左下.Margin.Top, 0, 0);
            }


            if (int_拖曳模式 == 拖曳模式.左上) {
                b_左上.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_上.Margin = new Thickness(0, b_左上.Margin.Top, 0, 0);//同步移動
                b_左.Margin = new Thickness(b_左上.Margin.Left, 0, 0, 0);

            } else if (int_拖曳模式 == 拖曳模式.右上) {

                b_右上.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_上.Margin = new Thickness(0, b_右上.Margin.Top, 0, 0);//同步移動
                b_右.Margin = new Thickness(b_右上.Margin.Left, 0, 0, 0);

            } else if (int_拖曳模式 == 拖曳模式.右下) {

                b_右下.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_下.Margin = new Thickness(0, b_右下.Margin.Top, 0, 0);//同步移動
                b_右.Margin = new Thickness(b_右下.Margin.Left, 0, 0, 0);

            } else if (int_拖曳模式 == 拖曳模式.左下) {

                b_左下.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_下.Margin = new Thickness(0, b_左下.Margin.Top, 0, 0);//同步移動
                b_左.Margin = new Thickness(b_左下.Margin.Left, 0, 0, 0);
            }


            b_左上.Margin = new Thickness(b_左.Margin.Left, b_上.Margin.Top, 0, 0);
            b_右下.Margin = new Thickness(b_右.Margin.Left, b_下.Margin.Top, 0, 0);
            b_左下.Margin = new Thickness(b_左.Margin.Left, b_下.Margin.Top, 0, 0);
            b_右上.Margin = new Thickness(b_右.Margin.Left, b_上.Margin.Top, 0, 0);


            double L = b_左.Margin.Left + 10;
            double T = b_上.Margin.Top + 10;
            double W = b_右.Margin.Left - b_左.Margin.Left;
            double H = b_下.Margin.Top - b_上.Margin.Top;

            try {
                b_中心拖曳.Width = Math.Abs(W) - 10;
                b_中心拖曳.Height = Math.Abs(H) - 10;
            } catch {
                b_中心拖曳.Width = 0;
                b_中心拖曳.Height = 0;
            }


            if (b_上.Margin.Top > b_下.Margin.Top)
                T = T + H;

            if (b_左.Margin.Left > b_右.Margin.Left)
                L = L + W;

            b_中心拖曳.Margin = new Thickness(L + 5, T + 5, 0, 0);
            //計算遮罩位置
            transparentRect.Rect = new Rect(L, T, Math.Abs(W) + 1, Math.Abs(H) + 1);


            func_顯示size();
        }





        /// <summary>
        /// 
        /// </summary>
        private void func_顯示size() {

            var rect = transparentRect.Rect;
            var mouse = func_取得滑鼠();

            String size = "";
            if (int_拖曳模式 == 拖曳模式.none) {
                size = $"l:{mouse.X}  t:{mouse.Y}";
            } else {
                size = $"l:{(int)rect.Left}  t:{(int)rect.Top}  w:{(int)rect.Width}  h:{(int)rect.Height}";
            }

            lab_size.Content = size;


            int int_邊距 = 5;

            double x = rect.Width + rect.Left + int_邊距;
            double y = rect.Height + rect.Top + int_邊距;

            if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下 || int_拖曳模式 == 拖曳模式.none) {
                x = mouse.X + int_邊距;
            }
            if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右 | int_拖曳模式 == 拖曳模式.none) {
                y = mouse.Y + int_邊距;
            }

            double lw = lab_size.ActualWidth;
            double lh = lab_size.ActualHeight;

            //在矩形選取範圍內
            if (mouse.X + 15 >= rect.Left &&
               mouse.X <= rect.Left + rect.Width - 15) {

                if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下) {
                    x = mouse.X + int_邊距;
                } else {
                    x = rect.Left - lw - int_邊距;
                }
            }

            if (mouse.Y + 15 >= rect.Top &&
              mouse.Y <= rect.Top + rect.Height - 15) {

                if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右) {
                    y = mouse.Y + int_邊距;
                } else {
                    y = rect.Top - lh - int_邊距;
                }
            }

            //超出螢幕
            if (mouse.X + lw + int_邊距 >= this.ActualWidth) {
                x = mouse.X - lw - int_邊距;
            }
            if (mouse.Y + lh + int_邊距 >= this.ActualHeight) {
                y = mouse.Y - lh - int_邊距;
            }
            if (x < 0) {
                x = mouse.X + int_邊距;
            }
            if (y < 0) {
                y = mouse.Y + int_邊距;
            }


            lab_size.Margin = new Thickness(x, y, 0, 0);


            lab_size.Visibility = Visibility.Visible;

        }



        /// <summary>
        /// 取得全螢幕的截圖
        /// </summary>
        private System.Drawing.Bitmap CaptureScreen() {
            /// new a bitmap with screen width and height
            var b = new System.Drawing.Bitmap(
                    (int)d_螢幕_w,
                    (int)d_螢幕_h);

            //從螢幕的最左上角開始計算
            var Work = System.Windows.Forms.Screen.GetBounds(new System.Drawing.Point(int_螢幕起始坐標_x, int_螢幕起始坐標_y));

            /// copy screen through .net form api
            using (var g = System.Drawing.Graphics.FromImage(b)) {
                g.CopyFromScreen(int_螢幕起始坐標_x, int_螢幕起始坐標_y, 0, 0,
                    b.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }


            return b;
        }



        /// <summary>
        /// 儲存圖片
        /// </summary>
        void func_SaveBitmap(System.Drawing.Bitmap b, String type, string path) {


            System.Console.WriteLine(path);

            //png
            if (type == "png") {
                b.Save(path);
                return;
            }

            //------------


            //jpg

            var jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);

            // Create an Encoder object based on the GUID
            // for the Quality parameter category.
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one
            // EncoderParameter object in the array.
            var myEncoderParameters = new System.Drawing.Imaging.EncoderParameters(1);

            var myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 98L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            b.Save(path, jpgEncoder, myEncoderParameters);

        }
        private System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format) {
            System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders();
            foreach (System.Drawing.Imaging.ImageCodecInfo codec in codecs) {
                if (codec.FormatID == format.Guid) {
                    return codec;
                }
            }
            return null;
        }




        /// <summary>
        /// 轉換成WPF圖片格式
        /// </summary>
        private static BitmapSource ToBitmapSource(System.Drawing.Bitmap b) {
            /// transform to hbitmap image
            IntPtr hb = b.GetHbitmap();

            /// convert hbitmap to bitmap source
            BitmapSource bs =
                Imaging.CreateBitmapSourceFromHBitmap(
                    hb, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            /// release temp hbitmap image
            DeleteObject(hb);

            return bs;
        }
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject(IntPtr hObject);





        /// <summary>
        /// 剪裁 -- 用GDI+
        /// </summary>
        /// <param name="b">原始Bitmap</param>
        /// <param name="StartX">開始坐標X</param>
        /// <param name="StartY">開始坐標Y</param>
        /// <param name="iWidth">寬度</param>
        /// <param name="iHeight">高度</param>
        /// <returns>剪裁後的Bitmap</returns>
        public System.Drawing.Bitmap KiCut(System.Drawing.Bitmap b, int StartX, int StartY, int iWidth, int iHeight) {
            if (b == null) {
                return null;
            }

            //int w = (int)(b.Width / d_解析度比例_x);
            //int h = (int)(b.Height / d_解析度比例_y);
            int w = (b.Width);
            int h = (b.Height);


            if (StartX >= w || StartY >= h) {
                return null;
            }

            if (StartX + iWidth > w) {
                iWidth = w - StartX;

            }

            if (StartY + iHeight > h) {
                iHeight = h - StartY;

            }

            try {


                StartX = (int)(StartX * d_解析度比例_x);
                StartY = (int)(StartY * d_解析度比例_y);
                iWidth = (int)(iWidth * d_解析度比例_x);
                iHeight = (int)(iHeight * d_解析度比例_y);


                var bmpOut = new System.Drawing.Bitmap(iWidth, iHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                var g = System.Drawing.Graphics.FromImage(bmpOut);

                g.DrawImage(b,
                    new System.Drawing.Rectangle(0, 0, iWidth, iHeight),
                    new System.Drawing.Rectangle(StartX, StartY, iWidth, iHeight),
                    System.Drawing.GraphicsUnit.Pixel
                );

                g.Dispose();

                return bmpOut;
            } catch {
                return null;
            }
        }



    }








    enum 拖曳模式 {

        none = -1,

        中心 = 0,

        上 = 10,
        下 = 20,
        右 = 30,
        左 = 40,


        左上 = 1,
        右上 = 2,
        右下 = 3,
        左下 = 4,


    }

}
