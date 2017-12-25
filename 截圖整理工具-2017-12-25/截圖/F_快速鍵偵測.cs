using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 截圖 {


    public partial class F_快速鍵偵測 : Form {




        /**
         * 
         *  此類別用於『全域鍵盤偵測』
         * 
         *  要使用『WindowsHookLib.dll』來進行全域鍵盤的偵測，必須用一個form視窗來建立
         *  否則會無預警閃退
         *       
         */

        MainWindow M;


        public F_快速鍵偵測 (MainWindow m) {
            InitializeComponent();
            this.M = m;
            this.Load += F_快速鍵偵測_Load;
        }


        private void F_快速鍵偵測_Load (object sender, EventArgs e) {
            keyboardHook1.InstallHook(); //偵測鍵盤
            keyboardHook1.KeyUp += KeyboardHook1_KeyUp;
            keyboardHook1.KeyDown += KeyboardHook1_KeyDown;
        }


        /// <summary>
        /// 按下時，設定快速鍵
        /// </summary>
        private void KeyboardHook1_KeyDown (object sender, WindowsHookLib.KeyboardEventArgs e) {

            if (M.w_設定 == null)
                return;

            if (M.w_設定.IsActive == true) {

                if (M.w_設定.textBox_截圖快速鍵.IsFocused == true && M.web_資料夾.Focused == false) {

                    String s = fun_取得按鍵(e);
                    M.w_設定.textBox_截圖快速鍵.Text = s;
                    M.s_快速鍵 = s;

                    return;
                }
            }



        }


        /// <summary>
        /// 放開時，偵測是否啟動
        /// </summary>
        private void KeyboardHook1_KeyUp (object sender, WindowsHookLib.KeyboardEventArgs e) {

            if (M.w_設定 != null)
                if (M.w_設定.IsActive == true) {
                    if (M.w_設定.textBox_截圖快速鍵.IsFocused == true &&
                         M.web_資料夾.Focused == false) {
                        return;
                    }

                }


            String k = fun_取得按鍵(e);

            if (M.Top != -5000)
                if (k.Equals(M.s_快速鍵)) {

                    M.d_記錄視窗位子 = M.Top;
                    M.Top = -5000;
                    new W_截圖(M).Show();
                }

        }


        private String fun_取得按鍵 (WindowsHookLib.KeyboardEventArgs e) {
            String k = e.KeyCode.ToString();

            if (e.Shift && (e.KeyCode != Keys.ShiftKey)) {
                k = "shift + " + k;
            }

            if (e.Control && (e.KeyCode != Keys.ControlKey)) {
                k = "ctrl + " + k;
            }

            if (e.Alt && (e.KeyCode != Keys.Menu)) {
                k = "alt + " + k;
            }
            return k;
        }






    }
}
