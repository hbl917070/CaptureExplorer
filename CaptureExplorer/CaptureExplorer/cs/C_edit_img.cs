using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaptureExplorer {

    [ComVisible(true)]
    public class C_web呼叫javaScript {

        //   window.external.fun_setSizeText

        private C_edit_img M;

        public C_web呼叫javaScript(C_edit_img m) {
            this.M = m;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="base64"></param>
        public void fun_save_png(String base64) {
            M.fun_save(base64, "png");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="base64"></param>
        public void fun_save_jpg(String base64) {
            M.fun_save(base64, "jpg");
        }

        public void fun_save_copy(String base64) {
            M.fun_save(base64, "copy");
        }


        public void func_初始化() {
            M.func_初始化();
        }



    }


    /// <summary>
    /// 編輯圖片用的視窗
    /// </summary>
    public class C_edit_img : Form {

        WebBrowser webBrowser1;
        Bitmap bitmap;
        String s_save_path;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="s_save_path"></param>
        public C_edit_img(Bitmap bitmap, String s_save_path) {

            fun_升級web核心();

            func_初始化視窗();
            Show();

            //取得焦點
            var tim = new Timer();
            tim.Interval = 100;
            tim.Tick += (sender, e) => {
                Focus();
                tim.Stop();
            };
            tim.Start();

            this.s_save_path = s_save_path;
            this.bitmap = bitmap;
        }

        /// <summary>
        /// 
        /// </summary>
        private void func_初始化視窗() {

            webBrowser1 = new System.Windows.Forms.WebBrowser();
            webBrowser1.Size = new Size(1,1);
            // webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            webBrowser1.Navigate(System.Windows.Forms.Application.StartupPath + "\\data\\web_paint\\index.html");
            webBrowser1.ObjectForScripting = new C_web呼叫javaScript(this);//讓網頁允許存取C#
            webBrowser1.IsWebBrowserContextMenuEnabled = false;//禁止右鍵選單
            webBrowser1.AllowNavigation = false;//載入完成後，禁止瀏覽其他網頁
            webBrowser1.BackColor = Color.FromArgb(40,40,40);

            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.webBrowser1);
            this.Text = "Edit Image";
            this.WindowState = FormWindowState.Maximized;//視窗最大化
            this.BackColor = Color.FromArgb(40, 40, 40);

        }


        /// <summary>
        /// 由JS呼叫
        /// </summary>
        public void func_初始化() {

            //避免畫面閃爍
            webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;

            String SigBase64 = "";

            //在編輯模式下用JPG，避免浪費效能
            using (var ms = new MemoryStream()) {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                SigBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(ms.GetBuffer()); //Get Base64
                webBrowser1.Document.InvokeScript("func_load_img", new object[] { SigBase64, bitmap.Width + "", bitmap.Height + "" });
            }

        }


        /// <summary>
        /// 由JS呼叫
        /// </summary>
        /// <param name="base64"></param>
        public void fun_save(String base64, String type) {
            Bitmap bit_png = null;

            //base64 轉回 Bitmap
            base64 = base64.Substring("data:image/png;base64,".Length);
            byte[] imageBytes = Convert.FromBase64String(base64);
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length)) {
                bit_png = new Bitmap(ms);
            }

            //圖片疊加
            Bitmap bit_底圖 = new Bitmap(bitmap);
            Graphics g = Graphics.FromImage(bit_底圖);
            g.DrawImage(bit_png, new Rectangle(0, 0, bitmap.Width, bitmap.Height));


            if (type == "jpg") {
                bit_底圖.Save(s_save_path + ".jpg", ImageFormat.Jpeg);
                func_關閉編輯模式();
            }

            if (type == "png") {
                bit_底圖.Save(s_save_path + ".png");
                func_關閉編輯模式();
            }

            if (type == "copy") {
                try {
                    Clipboard.SetImage(bit_底圖);
                } catch {
                    MessageBox.Show("存入剪貼簿時發生意外");
                }


            }


        }


        /// <summary>
        /// 
        /// </summary>
        void func_關閉編輯模式() {

            var tim = new Timer();
            tim.Interval = 50;
            tim.Tick += (sender, e) => {
                try {
                    this.Close();
                } catch { }

                tim.Stop();
            };
            tim.Start();
        }


        /// <summary>
        /// 使用IE11核心
        /// </summary>
        // set WebBrowser features, more info: http://stackoverflow.com/a/18333982/1768303
        public static void fun_升級web核心() {
            // don't change the registry if running in-proc inside Visual Studio
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;

            //判斷IE版本的方法
            var GetBrowserEmulationMode = new Func<UInt32>(() => {

                int browserVersion = 0;
                using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                    RegistryKeyPermissionCheck.ReadSubTree,
                    System.Security.AccessControl.RegistryRights.QueryValues)) {
                    var version = ieKey.GetValue("svcVersion");
                    if (null == version) {
                        version = ieKey.GetValue("Version");
                        if (null == version)
                            throw new ApplicationException("Microsoft Internet Explorer is required!");
                    }
                    int.TryParse(version.ToString().Split('.')[0], out browserVersion);
                }

                if (browserVersion < 7) {
                    throw new ApplicationException("Unsupported version of Microsoft Internet Explorer!");
                }

                UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode. 

                switch (browserVersion) {
                    case 7:
                        mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode. 
                        break;
                    case 8:
                        mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode. 
                        break;
                    case 9:
                        mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                    
                        break;
                    case 10:
                        mode = 10000; // Internet Explorer 10.
                        break;
                }

                return mode;
            });

            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";

            //修改預設IE版本
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION", appName, GetBrowserEmulationMode(), RegistryValueKind.DWord);

            //使用完整的IE瀏覽器功能
            //Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", appName, 1, RegistryValueKind.DWord);
            //Registry.SetValue(featureControlRegKey + "FEATURE_AJAX_CONNECTIONEVENTS", appName, 1, RegistryValueKind.DWord);
            Registry.SetValue(featureControlRegKey + "FEATURE_GPU_RENDERING", appName, 1, RegistryValueKind.DWord);
            //Registry.SetValue(featureControlRegKey + "FEATURE_WEBOC_DOCUMENT_ZOOM", appName, 1, RegistryValueKind.DWord);
            //Registry.SetValue(featureControlRegKey + "FEATURE_NINPUT_LEGACYMODE", appName, 0, RegistryValueKind.DWord);

        }


    }




  

}
