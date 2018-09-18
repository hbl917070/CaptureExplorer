
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WPFScreenshot;
using 鍵盤全域偵測;

namespace WPFScreenshot {
    public class C_全域按鍵偵測 {


        MainWindow M;

        public C_全域按鍵偵測(MainWindow m) {


            this.M = m;

            HookManager.KeyDown += HookManager_KeyDown;
            //HookManager.KeyUp += HookManager_KeyUp;

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {


            if (M.w_截圖 != null) {
                /*if (e.KeyCode == Keys.Escape) {
                    M.w_截圖.func_關閉程式();
                    e.Handled = true;
                    return;
                }*/

                M.w_截圖.func_key_down(e);


                return;

            }

            if (M.w_設定 != null)
                if (M.w_設定.IsActive == true) {

                    String s = fun_取得按鍵(e);

                    if (M.w_設定.textBox_截圖快速鍵.IsFocused == true && M.web_資料夾.Focused == false) {
                        M.w_設定.textBox_截圖快速鍵.Text = s;
                        M.s_快速鍵 = s;
                        return;
                    }


                    if (M.w_設定.textBox_截圖快速鍵_全螢幕.IsFocused == true && M.web_資料夾.Focused == false) {
                        M.w_設定.textBox_截圖快速鍵_全螢幕.Text = s;
                        M.s_快速鍵_全螢幕 = s;
                        return;
                    }

                    if (M.w_設定.textBox_截圖快速鍵_目前視窗.IsFocused == true && M.web_資料夾.Focused == false) {
                        M.w_設定.textBox_截圖快速鍵_目前視窗.Text = s;
                        M.s_快速鍵_目前視窗 = s;
                        return;
                    }
                }



            String k = fun_取得按鍵(e);

            if (k == M.s_快速鍵) {
                M.func_截圖();
                e.Handled = true;
            } else

            if (k == M.s_快速鍵_全螢幕) {
                M.func_截圖_全螢幕();
                e.Handled = true;
            } else

            if (k == M.s_快速鍵_目前視窗) {
                M.func_截圖_目前視窗();
                e.Handled = true;
            }
        }



        private String fun_取得按鍵(KeyEventArgs e) {


            /*String sum = "";
            var ar_k = new List<System.Windows.Input.Key>();
            foreach (System.Windows.Input.Key item in (System.Windows.Input.Key[])Enum.GetValues(typeof(System.Windows.Input.Key))) {
                if (item == System.Windows.Input.Key.None) {
                    continue;
                }
                if (System.Windows.Input.Keyboard.IsKeyDown(item)) {
                    ar_k.Add(item);
                }
            }
            ar_k.Sort();
            for (int i = 0; i < ar_k.Count; i++) {
                if (i == ar_k.Count - 1) {
                    sum += ar_k[i].ToString();
                } else {
                    sum += ar_k[i].ToString() + " + ";
                }
            }
            return sum;*/

            String k = e.KeyCode.ToString();

            k = k.Replace("LShiftKey", "LShift").Replace("RShiftKey", "RShift")
                 .Replace("LControlKey", "LCtrl").Replace("RControlKey", "RCtrl")
                 .Replace("LMenu", "LAlt").Replace("RMenu", "RAlt")
                 .Replace("PrintScreen", "PrtScrSysRq");


            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) && (e.KeyCode != Keys.LShiftKey)) {
                k = "LShift + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift) && (e.KeyCode != Keys.RShiftKey)) {
                k = "RShift + " + k;
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && (e.KeyCode != Keys.LControlKey)) {
                k = "LCtrl + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl) && (e.KeyCode != Keys.RControlKey)) {
                k = "RCtrl + " + k;
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) && (e.KeyCode != Keys.LMenu)) {
                k = "LAlt + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt) && (e.KeyCode != Keys.RMenu)) {
                k = "RAlt + " + k;
            }

            //Windows鍵
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LWin) && (e.KeyCode != Keys.LWin)) {
                k = "LWin + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RWin) && (e.KeyCode != Keys.RWin)) {
                k = "RWin + " + k;
            }

            //F1 ~ F12
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F1) && (e.KeyCode != Keys.F1)) {
                k = "F1 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F2) && (e.KeyCode != Keys.F2)) {
                k = "F2 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F3) && (e.KeyCode != Keys.F3)) {
                k = "F3 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F4) && (e.KeyCode != Keys.F4)) {
                k = "F4 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F5) && (e.KeyCode != Keys.F5)) {
                k = "F5 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F6) && (e.KeyCode != Keys.F6)) {
                k = "F6 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F7) && (e.KeyCode != Keys.F7)) {
                k = "F7 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F8) && (e.KeyCode != Keys.F8)) {
                k = "F8 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F9) && (e.KeyCode != Keys.F9)) {
                k = "F9 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F10) && (e.KeyCode != Keys.F10)) {
                k = "F10 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F11) && (e.KeyCode != Keys.F11)) {
                k = "F11 + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F12) && (e.KeyCode != Keys.F12)) {
                k = "F12 + " + k;
            }

            //截圖按鍵
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.PrintScreen) && (e.KeyCode != Keys.PrintScreen)) {
                k = "PrtScrSysRq + " + k;
            }

            //方向鍵上面的6個按鍵
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Insert) && (e.KeyCode != Keys.Insert)) {
                k = "Insert + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Home) && (e.KeyCode != Keys.Home)) {
                k = "Home + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.PageUp) && (e.KeyCode != Keys.PageUp)) {
                k = "PageUp + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.PageDown) && (e.KeyCode != Keys.PageDown)) {
                k = "PageDown + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Delete) && (e.KeyCode != Keys.Delete)) {
                k = "Delete + " + k;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.End) && (e.KeyCode != Keys.End)) {
                k = "End + " + k;
            }





            return k;
        }



    }
}
