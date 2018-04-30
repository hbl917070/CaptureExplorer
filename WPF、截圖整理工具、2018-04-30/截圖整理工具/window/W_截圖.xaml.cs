using System;
using System.Collections.Generic;
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

namespace 截圖 {
    /// <summary>
    /// W_截圖.xaml 的互動邏輯
    /// </summary>
    public partial class W_截圖 : Window {


        private System.Windows.Forms.Timer t;
        private int int_拖曳模式 = -1;
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



        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        public W_截圖(MainWindow m) {



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




            this.M = m;

            bimg = CaptureScreen();//全螢幕截圖

            InitializeComponent();

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
            t = new System.Windows.Forms.Timer();
            t.Interval = 10;
            t.Tick += T_Tick;
            t.Start();
            T_Tick(null, null);//立刻執行，避免使用者開啟截圖後立即點擊

            func_重新();

            Rectangle[] ar = { b_中心拖曳, b_左上, b_中上, b_右上, b_右中, b_右下, b_中下, b_左下, b_左中 };

            for (int i = 0; i < ar.Length; i++) {
                ar[i].MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                    st_按鈕群.Visibility = Visibility.Hidden;
                    d_滑鼠xy = func_取得滑鼠();
                    t.Start();
                };
            }

            b_中心拖曳.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_中心拖曳.Margin.Left, b_中心拖曳.Margin.Top, b_中心拖曳.ActualWidth, b_中心拖曳.ActualHeight };
                int_拖曳模式 = 0;
            };

            b_中上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_中上.Margin.Left, b_中上.Margin.Top, b_中上.ActualWidth, b_中上.ActualHeight };
                int_拖曳模式 = 10;
            };
            b_中下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_中下.Margin.Left, b_中下.Margin.Top, b_中下.ActualWidth, b_中下.ActualHeight };
                int_拖曳模式 = 20;
            };

            b_右中.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右中.Margin.Left, b_右中.Margin.Top, b_右中.ActualWidth, b_右中.ActualHeight };
                int_拖曳模式 = 30;
            };
            b_左中.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左中.Margin.Left, b_左中.Margin.Top, b_左中.ActualWidth, b_左中.ActualHeight };
                int_拖曳模式 = 40;
            };



            b_左上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左上.Margin.Left, b_左上.Margin.Top, b_左上.ActualWidth, b_左上.ActualHeight };
                int_拖曳模式 = 1;
            };
            b_右上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右上.Margin.Left, b_右上.Margin.Top, b_右上.ActualWidth, b_右上.ActualHeight };
                int_拖曳模式 = 2;
            };
            b_右下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右下.Margin.Left, b_右下.Margin.Top, b_右下.ActualWidth, b_右下.ActualHeight };
                int_拖曳模式 = 3;
            };
            b_左下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左下.Margin.Left, b_左下.Margin.Top, b_左下.ActualWidth, b_左下.ActualHeight };
                int_拖曳模式 = 4;
            };



            this.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) => {

                if (b_中心拖曳.ActualHeight < 1 && b_中心拖曳.ActualWidth < 1) {
                    func_重新();
                    return;
                }

                t.Enabled = false;
                rect_游標.Margin = new Thickness(-100, -100, 0, 0);


                fun_顯示按鈕();

            };






            //第一次拖曳
            this.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {

                if (bool_初始) {

                    b_左上.Margin = new Thickness(func_取得滑鼠().X - 10, func_取得滑鼠().Y - 10, 0, 0);

                    d_xywh = new double[] { b_左上.Margin.Left, b_左上.Margin.Top, b_左上.ActualWidth, b_左上.ActualHeight };
                    d_滑鼠xy = func_取得滑鼠();
                    t.Start();
                    int_拖曳模式 = 3;
                    bool_初始 = false;
                }
            };


            //按空白鍵 拖曳
            int int_空白鍵記錄 = 0;
            Boolean bool_空白鍵記錄 = false;


            this.KeyDown += (object sender, KeyEventArgs e) => {

                if (e.Key == Key.Escape) { //按esc結束

                    bimg.Dispose();
                    bimg = null;

                    this.Close();

                } else if (e.Key == Key.Enter) {
                    fun_確認儲存("png");
                }

                if (int_拖曳模式 == -1)
                    return;

                //按空白鍵 拖曳
                if (bool_空白鍵記錄 == false)
                    if (e.Key == Key.Space) {
                        bool_空白鍵記錄 = true;
                        d_xywh = new double[] { b_中心拖曳.Margin.Left, b_中心拖曳.Margin.Top, b_中心拖曳.ActualWidth, b_中心拖曳.ActualHeight };
                        d_滑鼠xy = func_取得滑鼠();
                        int_空白鍵記錄 = int_拖曳模式;
                        int_拖曳模式 = 0;
                    }

            };
            this.KeyUp += (object sender, KeyEventArgs e) => {


                //按空白鍵 拖曳
                if (e.Key == Key.Space) {
                    bool_空白鍵記錄 = false;

                    Rectangle r = null;
                    if (int_空白鍵記錄 == 1) {
                        r = b_左上;
                    } else if (int_空白鍵記錄 == 2) {
                        r = b_右上;
                    } else if (int_空白鍵記錄 == 3) {
                        r = b_右下;
                    } else if (int_空白鍵記錄 == 4) {
                        r = b_左下;
                    } else if (int_空白鍵記錄 == 10) {
                        r = b_中上;
                    } else if (int_空白鍵記錄 == 20) {
                        r = b_中下;
                    } else if (int_空白鍵記錄 == 30) {
                        r = b_右中;
                    } else if (int_空白鍵記錄 == 40) {
                        r = b_左中;
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
                if (int_拖曳模式 == -1) {

                    bimg.Dispose();
                    bimg = null;

                    this.Close();
                } else {
                    func_重新();
                }
                this.Title = "+++";
            };

            button_關閉.Click += (object sender, RoutedEventArgs e) => {

                bimg.Dispose();
                bimg = null;

                this.Close();
            };

            button_重新.Click += (object sender, RoutedEventArgs e) => {
                func_重新();
            };

            button_確認_png.Click += (object sender, RoutedEventArgs e) => {
                fun_確認儲存("png");
            };
            button_確認_jpg.Click += (object sender, RoutedEventArgs e) => {
                fun_確認儲存("jpg");
            };

            this.Closed += (object sender, EventArgs e) => {
                M.Top = M.d_記錄視窗位子;
            };

        }






        /// <summary>
        /// 
        /// </summary>
        private void fun_顯示按鈕() {

            int i_邊距 = 10;

            var rect = transparentRect.Rect;

            double x = 0;
            double y = 0;

            int int_上 = ((b_中上.Margin.Top < b_中下.Margin.Top) ? 10 : 20);
            int int_右 = ((b_右中.Margin.Left > b_左中.Margin.Left) ? 30 : 40);
            int int_下 = ((b_中上.Margin.Top < b_中下.Margin.Top) ? 20 : 10);
            int int_左 = ((b_右中.Margin.Left > b_左中.Margin.Left) ? 40 : 30);

            int int_左上 = 1;
            int int_右上 = 2;
            int int_右下 = 3;
            int int_左下 = 4;

            if (int_上 == 20) {
                int zz = int_左上;
                int_左上 = int_左下;
                int_左下 = zz;

                zz = int_右上;
                int_右上 = int_右下;
                int_右下 = zz;
            }

            if (int_右 == 40) {
                int zz = int_右上;
                int_右上 = int_左上;
                int_左上 = zz;

                zz = int_右下;
                int_右下 = int_左下;
                int_左下 = zz;
            }


            if (int_拖曳模式 == int_左上 || int_拖曳模式 == int_上 || int_拖曳模式 == int_左) {

                y = rect.Top - i_邊距 - st_按鈕群.ActualHeight;
                x = rect.Left + i_邊距;

                if (y < 0) {
                    y = rect.Top + i_邊距;
                    x = rect.Left - i_邊距 - st_按鈕群.ActualWidth;
                }
                if (x < 0) {
                    x = rect.Left + i_邊距;
                    if (rect.Height < 300)
                        y = rect.Top + rect.Height + i_邊距;
                }

            } else if (int_拖曳模式 == int_右上) {

                y = rect.Top - i_邊距 - st_按鈕群.ActualHeight;
                x = rect.Left + rect.Width - st_按鈕群.ActualWidth - i_邊距;

                if (y < 0) {
                    y = rect.Top + i_邊距;
                    x = rect.Left + rect.Width + i_邊距;
                }
                if (x + st_按鈕群.ActualWidth > this.ActualWidth) {
                    x = rect.Left + rect.Width - st_按鈕群.ActualWidth - i_邊距;
                    if (rect.Height < 300)
                        y = rect.Top + rect.Height + i_邊距;
                }

            } else if (int_拖曳模式 == int_右下 || int_拖曳模式 == int_右 || int_拖曳模式 == int_下 || int_拖曳模式 == 0) {

                y = rect.Top + rect.Height + i_邊距;
                x = rect.Left + rect.Width - st_按鈕群.ActualWidth - i_邊距;

                if (y + st_按鈕群.ActualHeight > this.ActualHeight) {
                    y = rect.Top + rect.Height - st_按鈕群.ActualHeight - i_邊距;
                    x = rect.Left + rect.Width + i_邊距;
                }
                if (x + st_按鈕群.ActualWidth > this.ActualWidth) {
                    x = rect.Left + rect.Width - st_按鈕群.ActualWidth - i_邊距;
                    if (rect.Height < 300)
                        y = rect.Top - st_按鈕群.ActualHeight - i_邊距;
                }

            } else if (int_拖曳模式 == int_左下) {

                y = rect.Top + rect.Height + i_邊距;
                x = rect.Left + i_邊距;

                if (y + st_按鈕群.ActualHeight > this.ActualHeight) {
                    y = rect.Top + rect.Height - st_按鈕群.ActualHeight - i_邊距;
                    x = rect.Left + -st_按鈕群.ActualWidth - i_邊距;
                }
                if (x < 0) {
                    x = rect.Left + i_邊距;
                    if (rect.Height < 300)
                        y = rect.Top - st_按鈕群.ActualHeight - i_邊距;
                }

            }

            if (y + st_按鈕群.ActualHeight > this.ActualHeight)
                y = this.ActualHeight - st_按鈕群.ActualHeight;

            if (x + st_按鈕群.ActualWidth > this.ActualWidth)
                x = this.ActualWidth - st_按鈕群.ActualWidth;

            st_按鈕群.Visibility = Visibility.Visible;
            st_按鈕群.Margin = new Thickness(x, y, 0, 0);
        }



        /// <summary>
        /// 
        /// </summary>
        public void fun_確認儲存(String type) {

            if (int_拖曳模式 == -1)
                return;

            if (b_中心拖曳.ActualHeight < 1 && b_中心拖曳.ActualWidth < 1)
                return;


            var t = transparentRect.Rect;
            /*System.Drawing.Bitmap img = KiCut(bimg, (int)(t.Left* d_解析度比例_x), (int)(t.Top* d_解析度比例_y),
                (int)(t.Width* d_解析度比例_x), (int)(t.Height* d_解析度比例_y));*/

            System.Drawing.Bitmap img = KiCut(bimg, (int)(t.Left), (int)(t.Top),
         (int)(t.Width), (int)(t.Height));

            //避免 寬度 or 高度 未滿50
            double dd = 60 / t.Width;
            if (dd < 60 / t.Height)
                dd = 60 / t.Height;

            //double d_放大倍率 = M.c_set.fun_讀取數字欄位_float(M.textBox_圖片放大, 1, 5);
            double d_放大倍率 = 1;
            if (dd > d_放大倍率)
                d_放大倍率 = dd;

            img = Resize(img, d_放大倍率);

            //儲存圖片
            func_SaveBitmap(img, type, "img/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "." + type);//存檔




            //自動存入剪貼簿
            try {
                if (M.checkBox_自動存入剪貼簿.IsChecked.Value) {
                    Clipboard.SetData(DataFormats.Bitmap, img);
                }
            } catch { }



            //清理記憶體
            img.Dispose();
            img = null;
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

            b_中下.Margin = new Thickness(0, -100, 0, 0);
            b_右中.Margin = new Thickness(-100, -100, 0, 0);

            bool_初始 = true;
            int_拖曳模式 = -1;
            t.Start();
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


            if (int_拖曳模式 == -1) {//初始狀態（僅顯示十字線）
                b_左中.Margin = new Thickness(func_取得滑鼠().X - 10, 0, 0, 0);
                b_中上.Margin = new Thickness(0, func_取得滑鼠().Y - 10, 0, 0);

                //讓這個物件跟隨，就會顯示十字游標
                rect_游標.Margin = new Thickness(func_取得滑鼠().X - 30, func_取得滑鼠().Y - 30, 0, 0);

                return;
            }



            if (int_拖曳模式 == 0) {//拖曳

                double dou_左中 = b_左中.Margin.Left - b_中心拖曳.Margin.Left;
                double dou_右中 = b_右中.Margin.Left - b_中心拖曳.Margin.Left;
                double dou_中下 = b_中下.Margin.Top - b_中心拖曳.Margin.Top;
                double dou_中上 = b_中上.Margin.Top - b_中心拖曳.Margin.Top;


                b_中心拖曳.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );

                b_左中.Margin = new Thickness(b_中心拖曳.Margin.Left + dou_左中, 0, 0, 0);
                b_右中.Margin = new Thickness(b_中心拖曳.Margin.Left + dou_右中, 0, 0, 0);
                b_中下.Margin = new Thickness(0, b_中心拖曳.Margin.Top + dou_中下, 0, 0);
                b_中上.Margin = new Thickness(0, b_中心拖曳.Margin.Top + dou_中上, 0, 0);

            }


            if (int_拖曳模式 == 10) {

                b_中上.Margin = new Thickness(0, func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1], 0, 0);
                b_左上.Margin = new Thickness(
                    b_左上.Margin.Left,
                    b_中上.Margin.Top,
                    0, 0
                );
                b_右上.Margin = new Thickness(
                    b_右上.Margin.Left,
                    b_中上.Margin.Top,
                    0, 0
                );

            } else if (int_拖曳模式 == 20) {

                b_中下.Margin = new Thickness(0, func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1], 0, 0);
                b_右下.Margin = new Thickness(
                      b_右下.Margin.Left,
                      b_中下.Margin.Top,
                      0, 0
                );
                b_左下.Margin = new Thickness(
                      b_左下.Margin.Left,
                      b_中下.Margin.Top,
                      0, 0
                );

            }


            if (int_拖曳模式 == 30) {

                b_右中.Margin = new Thickness(func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0], 0, 0, 0);
                b_右上.Margin = new Thickness(b_右中.Margin.Left, b_右上.Margin.Top, 0, 0);
                b_右下.Margin = new Thickness(b_右中.Margin.Left, b_右下.Margin.Top, 0, 0);

            } else if (int_拖曳模式 == 40) {

                b_左中.Margin = new Thickness(func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0], 0, 0, 0);
                b_左上.Margin = new Thickness(b_左中.Margin.Left, b_左上.Margin.Top, 0, 0);
                b_左下.Margin = new Thickness(b_左中.Margin.Left, b_左下.Margin.Top, 0, 0);
            }


            if (int_拖曳模式 == 1) {
                b_左上.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_中上.Margin = new Thickness(0, b_左上.Margin.Top, 0, 0);//同步移動
                b_左中.Margin = new Thickness(b_左上.Margin.Left, 0, 0, 0);

            } else if (int_拖曳模式 == 2) {

                b_右上.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_中上.Margin = new Thickness(0, b_右上.Margin.Top, 0, 0);//同步移動
                b_右中.Margin = new Thickness(b_右上.Margin.Left, 0, 0, 0);

            } else if (int_拖曳模式 == 3) {

                b_右下.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_中下.Margin = new Thickness(0, b_右下.Margin.Top, 0, 0);//同步移動
                b_右中.Margin = new Thickness(b_右下.Margin.Left, 0, 0, 0);

            } else if (int_拖曳模式 == 4) {

                b_左下.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_中下.Margin = new Thickness(0, b_左下.Margin.Top, 0, 0);//同步移動
                b_左中.Margin = new Thickness(b_左下.Margin.Left, 0, 0, 0);
            }


            b_左上.Margin = new Thickness(b_左中.Margin.Left, b_中上.Margin.Top, 0, 0);
            b_右下.Margin = new Thickness(b_右中.Margin.Left, b_中下.Margin.Top, 0, 0);
            b_左下.Margin = new Thickness(b_左中.Margin.Left, b_中下.Margin.Top, 0, 0);
            b_右上.Margin = new Thickness(b_右中.Margin.Left, b_中上.Margin.Top, 0, 0);


            double L = b_左中.Margin.Left + 10;
            double T = b_中上.Margin.Top + 10;
            double W = b_右中.Margin.Left - b_左中.Margin.Left;
            double H = b_中下.Margin.Top - b_中上.Margin.Top;

            try {
                b_中心拖曳.Width = Math.Abs(W) - 10;
                b_中心拖曳.Height = Math.Abs(H) - 10;
            } catch {
                b_中心拖曳.Width = 0;
                b_中心拖曳.Height = 0;
            }


            if (b_中上.Margin.Top > b_中下.Margin.Top)
                T = T + H;

            if (b_左中.Margin.Left > b_右中.Margin.Left)
                L = L + W;

            b_中心拖曳.Margin = new Thickness(L + 5, T + 5, 0, 0);
            //計算遮罩位置
            transparentRect.Rect = new Rect(L, T, Math.Abs(W), Math.Abs(H));

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

}
