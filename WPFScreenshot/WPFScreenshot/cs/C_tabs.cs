using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFScreenshot.window;

namespace WPFScreenshot {
    public class C_頁籤拖曳 {


        Boolean b_拖曳 = false;
        public U_分頁_item b_but_text;
        StackPanel sp_容器;
        public Action<String> ac_click = new Action<string>((String s) => { });//點擊選項時
        public Action<String> ac_change = new Action<string>((String s) => { });//切換前


        SolidColorBrush Sol_1;
        SolidColorBrush Sol_2;
        SolidColorBrush Sol_3;

        SolidColorBrush Sol_白;


        List<U_分頁_item> ar_but = new List<U_分頁_item>();

        public C_頁籤拖曳(StackPanel sp) {

            sp_容器 = sp;

            Sol_1 = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));//預設顏色
            Sol_2 = new SolidColorBrush(Color.FromArgb(255, 15, 100, 155));//選中的顏色
            Sol_3 = new SolidColorBrush(Color.FromArgb(100, 100, 100, 100));//hover的顏色
            Sol_白 = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));//白色

            for (int i = 0; i < ar_but.Count; i++) {
                func_修改顏色_預設(ar_but[i]);
            }
        }


        private void func_修改顏色_預設(U_分頁_item sender) {
            sender.Background = Sol_1;
            //sender.tex.Foreground = Sol_白;
        }
        private void func_修改顏色_選中(U_分頁_item sender) {
            sender.Background = Sol_2;
            //sender.tex.Foreground = Sol_2 ;
        }
        private void func_修改顏色_移入(U_分頁_item sender) {
            sender.Background = Sol_3;
            //sender.tex.Foreground = Sol_白;
        }



        /// <summary>
        /// 設定目前選取的項目
        /// </summary>
        /// <param name="but"></param>
        public void fun_SetSelect(U_分頁_item but) {

            if (b_but_text != null)
                func_修改顏色_預設(b_but_text);

            //重複點選就不執行
            if (b_but_text != but) {
                ac_click(but.Text);             
            } 

            b_but_text = but;

            func_修改顏色_選中(but);

          
        }


        /// <summary>
        /// 刪除目前選取項目
        /// </summary>
        public void fun_delete() {

            var ar = sp_容器.Children;
            int x = 0;

            if (ar.Count == 1) {
                sp_容器.Children.RemoveAt(0);
                return;
            }

            if (ar.Count == 0) {
                return;
            }

            for (int i = 0; i < ar.Count; i++) {
                if ((U_分頁_item)ar[i] == b_but_text) {
                    x = i;
                    break;
                }
            }

            if (x == ar.Count - 1) {
                fun_SetSelect((U_分頁_item)ar[ar.Count - 2]);
            } else if (x == 0) {
                fun_SetSelect((U_分頁_item)ar[1]);
            } else {
                fun_SetSelect((U_分頁_item)ar[x + 1]);
            }
            sp_容器.Children.RemoveAt(x);
          
        }


        /// <summary>
        /// 給項目註冊所需要的事件
        /// </summary>
        /// <param name="but"></param>
        public void fun_addEvent(U_分頁_item but) {

            sp_容器.Children.Add(but);

            but.MouseDown += (object sender, MouseButtonEventArgs e) => {
                b_拖曳 = true;

                if (b_but_text == null) {
                    b_but_text = (U_分頁_item)sender;
                } else {
                    ac_change(b_but_text.Text);//執行 切換 事件
                }

                fun_SetSelect((U_分頁_item)sender);
                /*
                func_修改顏色_預設(b_but_text);
                b_but_text = ((U_分頁_item)sender);
                func_修改顏色_選中((U_分頁_item)sender);
                ac_click(((U_分頁_item)sender).Text);//執行 點擊 事件
                */
            };

            but.MouseUp += (object sender, MouseButtonEventArgs e) => {
                b_拖曳 = false;
            };

            but.MouseEnter += (object sender, MouseEventArgs e) => {
                if (b_拖曳 && e.LeftButton == MouseButtonState.Pressed) {

                    U_分頁_item x = ((U_分頁_item)sender);

                    String x2 = b_but_text.Text;
                    b_but_text.Text = x.Text;

                    ((U_分頁_item)sender).Text = x2;

                    //交換顏色
                    func_修改顏色_預設(b_but_text);
                    func_修改顏色_選中((U_分頁_item)sender);


                    b_but_text = x;

                } else {
                    if (sender != b_but_text)
                        func_修改顏色_移入((U_分頁_item)sender);

                }
            };


            but.MouseLeave += (object sender, MouseEventArgs e) => {
                if (sender == b_but_text) {
                    func_修改顏色_選中((U_分頁_item)sender);
                } else {
                    func_修改顏色_預設((U_分頁_item)sender);

                }
            };




        }


    }
}
