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
    /// W_對話.xaml 的互動邏輯
    /// </summary>
    public partial class W_對話 : Window {
        public W_對話 () {
            InitializeComponent();
        }

        MainWindow M;
        Action ac = null;



        public W_對話 (MainWindow m) {
            InitializeComponent();

            this.M = m;

            //讓視窗可以拖曳
            this.MouseLeftButtonDown += ((object sender, MouseButtonEventArgs e) => {
                this.DragMove();
            });





            ac = () => {
                this.Close();
            };

            but_yes.Click += (object sender, RoutedEventArgs e) => {
                ac();
                this.Close();
            };
            but_no.Click += (object sender, RoutedEventArgs e) => {
                this.Close();
            };

            this.KeyDown += (object sender, KeyEventArgs e) => {
                if (e.Key == Key.Escape) {
                    this.Close();
                } else if (e.Key == Key.Enter) {
                    ac();
                    this.Close();
                }
            };

        }



        public void set_title (String s) {
            this.Title = s;
        }
        public void set_text (String s) {
            textBox.Text = s;
        }
        public void set_yes (Action a) {
            ac = a;
        }
        public void set_無取消按鈕 () {
            but_no.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="w"></param>
        public void fun_how (Window w) {
            this.Owner = w;
            this.Show();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="w"></param>
        /// <param name="s"></param>
        public void fun_一般訊息 (Window w, String s) {
            set_text(s);
            set_無取消按鈕();
            fun_how(w);
        }


    }
}
