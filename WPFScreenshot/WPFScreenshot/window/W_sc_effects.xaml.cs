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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFScreenshot.window {





    /// <summary>
    /// W_sc_effects.xaml 的互動邏輯
    /// </summary>
    public partial class W_sc_effects : Window {



        double int_螢幕起始坐標_x;
        double int_螢幕起始坐標_y;
        double d_解析度比例_x;
        double d_解析度比例_y;
        double d_螢幕_w;
        double d_螢幕_h;

        public W_sc_effects() {
            InitializeComponent();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public void func_局部(int x, int y, int w, int h) {

            //取得解析度偏差
            PresentationSource source = PresentationSource.FromVisual(this);
            d_解析度比例_x = source.CompositionTarget.TransformToDevice.M11;
            d_解析度比例_y = source.CompositionTarget.TransformToDevice.M22;
        

            d_螢幕_w = w / d_解析度比例_x;
            d_螢幕_h = h / d_解析度比例_y;


            //程式最大化
            this.Width = d_螢幕_w;
            this.Height = d_螢幕_h;
            this.Left = x/ d_解析度比例_x;
            this.Top = y/ d_解析度比例_y;

            fun_播放點擊動畫();
        }




        /// <summary>
        /// 
        /// </summary>
        public void func_全螢幕() {


            //取得解析度偏差
            PresentationSource source = PresentationSource.FromVisual(this);
            d_解析度比例_x = source.CompositionTarget.TransformToDevice.M11;
            d_解析度比例_y = source.CompositionTarget.TransformToDevice.M22;

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


       

            //程式最大化
            this.Width = d_螢幕_w / d_解析度比例_x;
            this.Height = d_螢幕_h / d_解析度比例_y;

            this.Left = int_螢幕起始坐標_x;
            this.Top = int_螢幕起始坐標_y;

            fun_播放點擊動畫();
        }







        /// <summary>
        /// 左鍵動畫
        /// </summary>
        public void fun_播放點擊動畫() {//顯示動畫

            re.Opacity = 0.3;

            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation();
            animation.Duration = (Duration)TimeSpan.FromSeconds(0.2);
            //animation.BeginTime = TimeSpan.FromSeconds(float_左鍵動畫時間-0.2);
            animation.AccelerationRatio = 0.99;
            animation.From = 0.15;
            animation.To = 0;
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity", new object[0]));
            Storyboard.SetTarget(animation, re);
            storyboard.Children.Add(animation);
            storyboard.Begin();







            Storyboard storyboard_3 = new Storyboard();
            DoubleAnimation animation_3 = new DoubleAnimation();
            animation_3.Duration = (Duration)TimeSpan.FromSeconds(0.2);
            animation_3.From = d_螢幕_w;
            animation_3.To = 0;
            Storyboard.SetTarget(animation_3, re);
            storyboard_3.Children.Add(animation_3);
            Storyboard.SetTargetProperty(animation_3, new PropertyPath("Width"));
            storyboard_3.Begin();


            Storyboard storyboard_4 = new Storyboard();
            DoubleAnimation animation_4 = new DoubleAnimation();
            storyboard_4.Completed += (sender, e) => {
                //MessageBox.Show("");
                //this.Close();
            };
            animation_4.Duration = (Duration)TimeSpan.FromSeconds(0.2);
            animation_4.From = d_螢幕_h;
            animation_4.To = 0;
            Storyboard.SetTarget(animation_4, re);
            storyboard_4.Children.Add(animation_4);
            Storyboard.SetTargetProperty(animation_4, new PropertyPath("Height"));
            storyboard_4.Begin();


            var tim = new System.Windows.Forms.Timer();
            tim.Interval = 1000;
            tim.Tick += (sender, e) => {
                this.Close();
                tim.Stop();
            };
            tim.Start();

            /* Storyboard storyboard2 = new Storyboard();

             ScaleTransform scale2 = new ScaleTransform(1.0, 1.0);
             w_1.ell_點擊.RenderTransform = scale2;

             DoubleAnimation growAnimation2 = new DoubleAnimation();
             growAnimation2.Duration = (Duration)TimeSpan.FromSeconds(float_左鍵動畫時間);
             growAnimation2.From = 0;
             growAnimation2.To = 1;
             storyboard2.Children.Add(growAnimation2);

             Storyboard.SetTargetProperty(growAnimation2, new PropertyPath("RenderTransform.ScaleX"));
             Storyboard.SetTarget(growAnimation2, w_1.ell_點擊);
             storyboard2.Begin();

             Storyboard.SetTargetProperty(growAnimation2, new PropertyPath("RenderTransform.ScaleY"));
             storyboard2.Begin();
             */
            //--------------

            //----------------

        }

    }
}
