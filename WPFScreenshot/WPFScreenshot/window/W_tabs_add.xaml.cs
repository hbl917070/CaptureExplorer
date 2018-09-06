using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFScreenshot;
using WPFScreenshot.window;


namespace WPFScreenshot.window {
    /// <summary>
    /// W_頁籤新增.xaml 的互動邏輯
    /// </summary>
    public partial class W_頁籤新增 : Window {




        private MainWindow M;

        private List<String> ar_tab;
        private Action act = null;




        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        public W_頁籤新增(MainWindow m) {

            InitializeComponent();

            this.M = m;
            this.Owner = M;

            M.fun_鎖定視窗(true);

            this.Closed += (sender, e) => {
                M.fun_鎖定視窗(false);
            };

            but_no.Click += (sender, e) => {
                this.Close();
            };
            but_yes.Click += (sender, e) => {
                act();
            };



            //---------------

            //讓視窗可以拖曳
            this.MouseLeftButtonDown += ((sender, e) => {
                this.DragMove();
            });

            //-------

            ar_tab = new List<string>();
            var ar = M.stackPanel_1.Children;
            for (int i = 0; i < ar.Count; i++) {
                ar_tab.Add(((U_分頁_item)ar[i]).Text.ToString());
            }

            //--------
            this.KeyDown += ( sender,  e) => {
                if (e.Key == Key.Escape)
                    this.Close();
                else if (e.Key == Key.Enter)
                    act();
            };

        }


        /// <summary>
        /// 
        /// </summary>
        private void fun_全選() {
            new Thread(() => {
                Thread.Sleep(100);
                C_Adapter.fun_UI執行緒(() => {
                    textBox_tabName.Focus();
                    textBox_tabName.SelectAll();
                });
            }).Start();
        }


        /// <summary>
        /// 
        /// </summary>
        public void fun_新增() {


            this.Title = "新增資料夾";
            fun_全選();


            for (int i = 1; i < 999; i++) {
                if (ar_tab.Contains("New Folder " + i)) {

                } else {
                    textBox_tabName.Text = "New Folder " + i;
                    break;
                }
            }

            act = new Action(() => {

                //拿掉前後的空白
                textBox_tabName.Text = textBox_tabName.Text.Trim();


                //避免檔名過長
                if (textBox_tabName.Text.ToString().Length > 100) {
                    new W_對話(M).fun_一般訊息(this, "名字太長了");
                    return;
                }

                //避免名字空白
                if (textBox_tabName.Text.Replace(".", "") == "") {
                    new W_對話(M).fun_一般訊息(this, "名字不要空白啦");
                    return;
                }



                //避免非法字元
                String[] ff = { "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
                for (int i = 0; i < ff.Length; i++)
                    if (textBox_tabName.Text.Contains(ff[i])) {
                        new W_對話(M).fun_一般訊息(this, "資料夾名字不支援以下符號：\n / \\ : * ? \" < > |");
                        return;
                    }

                //避免名字重複
                if (ar_tab.Contains(textBox_tabName.Text)) {
                    new W_對話(M).fun_一般訊息(this, "「" + textBox_tabName.Text + "」已存在，請用別的名字");
                    return;
                }

                fun_新建一個頁籤(textBox_tabName.Text);

                this.Close();
            });

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        private void fun_新建一個頁籤(String s) {

            String str_新檔名 = M.func_取得儲存根目錄() + "\\" + s;

            if (Directory.Exists(str_新檔名) == false) {
                Directory.CreateDirectory(str_新檔名);
            }

            U_分頁_item bt = new U_分頁_item() {
                Text = s
            };
            bt.func_初始化(M);
            M.c_分頁.fun_addEvent(bt);
            M.c_分頁.fun_SetSelect(bt);//設定為選取狀態

        }


        /// <summary>
        /// 
        /// </summary>
        public void fun_修改() {

            this.Loaded += ( sender2,  e2) => {
                //顯示目前名字
                try {
                    textBox_tabName.Text = M.c_分頁.b_but_text.Text + "";
                } catch {
                    this.Close();
                }

                this.Title = "修改資料夾名稱";
                lab_yes.Content = "修改";
                fun_全選();

                act = new Action(() => {

                    //拿掉前後的空白
                    textBox_tabName.Text = textBox_tabName.Text.Trim();


                    //如果名字沒有修改就直接關閉視窗
                    if (M.c_分頁.b_but_text.Text == textBox_tabName.Text) {
                        Close();
                        return;
                    }

                    //避免檔名過長
                    if (textBox_tabName.Text.ToString().Length > 100) {
                        new W_對話(M).fun_一般訊息(this, "名字太長了啦");
                        return;
                    }


                    //避免名字空白
                    if (textBox_tabName.Text.Replace(".", "") == "") {
                        new W_對話(M).fun_一般訊息(this, "名字不要空白啦");
                        return;
                    }

                    //避免非法字元
                    String[] ff = { "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
                    for (int i = 0; i < ff.Length; i++)
                        if (textBox_tabName.Text.Contains(ff[i])) {
                            new W_對話(M).fun_一般訊息(this, "資料夾名稱不支援以下符號：\n/ \\ : * ? \" < > |");
                            return;
                        }

                    //避免名字重複
                    if (ar_tab.Contains(textBox_tabName.Text)) {
                        new W_對話(M).fun_一般訊息(this, "「" + textBox_tabName.Text + "」已存在，請用別的名字");
                        return;
                    }


                    String str_原檔名 = M.func_取得儲存根目錄() + "\\" + M.c_分頁.b_but_text.Text;
                    String str_新檔名 = M.func_取得儲存根目錄() + "\\" + textBox_tabName.Text;

                    //再次檢查檔名是否存在
                    if (Directory.Exists(str_原檔名) == false) {
                        new W_對話(M).fun_一般訊息(this, "資料夾不存在「" + textBox_tabName.Text + "」" + "\n看到這個訊息的話，最好趕快重新啟動程式");
                        return;
                    }

                    //再次檢查檔名沒有重複
                    if (Directory.Exists(str_新檔名)) {
                        new W_對話(M).fun_一般訊息(this, "資料夾「" + textBox_tabName.Text + "」已存在，請用別的名字哦" + "\n看到這個訊息的話，最好趕快重新啟動程式");
                        return;
                    }

                    Directory.Move(str_原檔名, str_新檔名);

                    //修改名字
                    String s_舊名 = M.c_分頁.b_but_text.Text;
                    String s_新名 = textBox_tabName.Text;
                    M.c_分頁.b_but_text.Text = s_新名;

                    M.func_重新整理資料夾();

                    this.Close();
                });
            };
        }




        /// <summary>
        /// 
        /// </summary>
        public void fun_刪除() {

            this.Loaded += (sender2, e2) => {

                //顯示目前名字
                try {
                    textBox_tabName.Text = M.c_分頁.b_but_text.Text + "";
                } catch {
                    this.Close();
                }

                textBox_tabName.IsReadOnly = true;
                this.Title = "刪除資料夾";
                lab_yes.Content = "刪除";


                act = new Action(() => {


                    String str_新檔名 = M.func_取得儲存根目錄() + "\\" + M.c_分頁.b_but_text.Text;

                    //再次檢查檔名沒有重複
                    if (Directory.Exists(str_新檔名) == false) {
                        new W_對話(M).fun_一般訊息(this, "檔案不存在「" + textBox_tabName.Text + "」" + "\n看到這個訊息的話，最好趕快重新啟動程式");
                        return;
                    }

                    //Directory.Delete(str_新檔名,true);

                    //把檔案移到垃圾桶，並顯示詢問視窗
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(str_新檔名,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                    M.c_分頁.fun_delete();

                    if (M.stackPanel_1.Children.Count == 0) {
                        fun_新建一個頁籤("New Folder 1");
                    }

                    this.Close();
                });

            };
        }


    }
}
