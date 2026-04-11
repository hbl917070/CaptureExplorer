using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CaptureExplorer.Interop {
    [ComVisible(true)]
    /// <summary>
    /// 提供網頁編輯器呼叫的橋接介面。
    /// </summary>
    public class WebScriptBridge {
        private readonly ImageEditorWindow editorWindow;

        public WebScriptBridge(ImageEditorWindow m) {
            editorWindow = m;
        }

        /// <summary>
        /// 將編輯結果另存成 PNG。
        /// </summary>
        public void SavePng(string base64) {
            editorWindow.SaveEditedImage(base64, "png");
        }

        /// <summary>
        /// 將編輯結果另存成 JPG。
        /// </summary>
        public void SaveJpg(string base64) {
            editorWindow.SaveEditedImage(base64, "jpg");
        }

        /// <summary>
        /// 將編輯結果複製到剪貼簿。
        /// </summary>
        public void SaveCopy(string base64) {
            editorWindow.SaveEditedImage(base64, "copy");
        }

        /// <summary>
        /// 通知編輯視窗可以載入原圖內容。
        /// </summary>
        public void InitializeBridge() {
            editorWindow.InitializeEditor();
        }
        // The web editor expects these legacy method names (used by the shipped HTML/JS).
        // Provide simple aliases so the bridge is compatible with the page's calls.
        public void fun_save_png(string base64) => SavePng(base64);
        public void fun_save_jpg(string base64) => SaveJpg(base64);
        public void fun_save_copy(string base64) => SaveCopy(base64);
        public void func_初始化() => InitializeBridge();
    }

    /// <summary>
    /// 以 WinForms 視窗承載舊版網頁圖片編輯器。
    /// </summary>
    public class ImageEditorWindow : Form {
        private WebBrowser webBrowser1;
        private readonly Bitmap bitmap;
        private readonly string s_save_path;

        /// <summary>
        /// 建立並顯示圖片編輯視窗。
        /// </summary>
        public ImageEditorWindow(Bitmap bitmap, string s_save_path) {
            ConfigureBrowserEmulation();
            InitializeWindow();
            Show();

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
        /// 建立視窗與內嵌瀏覽器控制項。
        /// </summary>
        private void InitializeWindow() {
            webBrowser1 = new WebBrowser {
                Size = new Size(1, 1),
                ObjectForScripting = new WebScriptBridge(this),
                IsWebBrowserContextMenuEnabled = false,
                AllowNavigation = false,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            webBrowser1.Navigate(Application.StartupPath + "\\Data\\WebPaint\\index.html");

            ClientSize = new Size(800, 450);
            Controls.Add(webBrowser1);
            Text = "Edit Image";
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(40, 40, 40);
        }

        /// <summary>
        /// 在網頁端完成初始化後載入原始圖片。
        /// </summary>
        public void InitializeEditor() {
            webBrowser1.Dock = DockStyle.Fill;

            using (var ms = new MemoryStream()) {
                bitmap.Save(ms, ImageFormat.Jpeg);
                string sigBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(ms.GetBuffer());
                webBrowser1.Document.InvokeScript("func_load_img", new object[] { sigBase64, bitmap.Width + "", bitmap.Height + "" });
            }
        }

        /// <summary>
        /// 將前景繪圖層合成回原圖並輸出結果。
        /// </summary>
        public void SaveEditedImage(string base64, string type) {
            base64 = base64.Substring("data:image/png;base64,".Length);
            byte[] imageBytes = Convert.FromBase64String(base64);

            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            using (var bit_png = new Bitmap(ms))
            using (Bitmap bit_底圖 = new Bitmap(bitmap))
            using (Graphics g = Graphics.FromImage(bit_底圖)) {
                g.DrawImage(bit_png, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                if (type == "jpg") {
                    bit_底圖.Save(s_save_path + ".jpg", ImageFormat.Jpeg);
                    CloseEditorMode();
                }

                if (type == "png") {
                    bit_底圖.Save(s_save_path + ".png");
                    CloseEditorMode();
                }

                if (type == "copy") {
                    try {
                        Clipboard.SetImage(bit_底圖);
                    } catch {
                        MessageBox.Show("存入剪貼簿時發生意外");
                    }
                }
            }
        }

        /// <summary>
        /// 稍微延後關閉視窗，避免儲存回呼被打斷。
        /// </summary>
        void CloseEditorMode() {
            var tim = new Timer();
            tim.Interval = 50;
            tim.Tick += (sender, e) => {
                try {
                    Close();
                } catch { }

                tim.Stop();
            };
            tim.Start();
        }

        /// <summary>
        /// 設定內嵌瀏覽器使用較新的 IE 相容模式。
        /// </summary>
        public static void ConfigureBrowserEmulation() {
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime) {
                return;
            }

            var getBrowserEmulationMode = new Func<uint>(() => {
                int browserVersion = 0;
                using (var ieKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Internet Explorer",
                    RegistryKeyPermissionCheck.ReadSubTree,
                    System.Security.AccessControl.RegistryRights.QueryValues)) {
                    var version = ieKey.GetValue("svcVersion");
                    if (version == null) {
                        version = ieKey.GetValue("Version");
                        if (version == null) {
                            throw new ApplicationException("Microsoft Internet Explorer is required!");
                        }
                    }

                    int.TryParse(version.ToString().Split('.')[0], out browserVersion);
                }

                if (browserVersion < 7) {
                    throw new ApplicationException("Unsupported version of Microsoft Internet Explorer!");
                }

                uint mode = 11000;
                switch (browserVersion) {
                    case 7:
                        mode = 7000;
                        break;
                    case 8:
                        mode = 8000;
                        break;
                    case 9:
                        mode = 9000;
                        break;
                    case 10:
                        mode = 10000;
                        break;
                }

                return mode;
            });

            var appName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";

            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION", appName, getBrowserEmulationMode(), RegistryValueKind.DWord);
            Registry.SetValue(featureControlRegKey + "FEATURE_GPU_RENDERING", appName, 1, RegistryValueKind.DWord);
        }
    }
}
