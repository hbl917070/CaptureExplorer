using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using CaptureExplorer.Services;
using CaptureExplorer.Windows;

namespace CaptureExplorer.Controllers {
    /// <summary>
    /// 管理整個截圖 session 的生命週期。
    /// 主要責任是：
    /// 1. 先為每個螢幕產生自己的背景截圖
    /// 2. 建立對應的 overlay 視窗
    /// 3. 維持「同一時間只允許一個 active overlay」的規則
    /// 4. 在 session 結束時統一關閉視窗與釋放資源
    /// </summary>
    public sealed class ScreenCaptureSessionController {
        private readonly MainWindow mainWindow;
        private readonly List<ScreenCaptureContext> contexts = new List<ScreenCaptureContext>();
        private readonly List<ScreenCaptureOverlayWindow> overlays = new List<ScreenCaptureOverlayWindow>();
        private readonly WindowState previousWindowState;
        private readonly double previousWindowTop;
        private bool isClosing;

        public ScreenCaptureSessionController(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
            previousWindowState = mainWindow.WindowState;
            previousWindowTop = mainWindow.Top;
        }

        public ScreenCaptureOverlayWindow ActiveOverlay { get; private set; }

        public bool IsRunning => isClosing == false && overlays.Count > 0;

        /// <summary>
        /// 啟動新的截圖 session。
        /// 流程上會先把主視窗移開，避免被自己截進去，
        /// 再逐一擷取各螢幕畫面，最後建立各自的 overlay 視窗。
        /// </summary>
        public void Start() {
            HideMainWindowBeforeCapture();

            foreach (var screen in Screen.AllScreens) {
                contexts.Add(new ScreenCaptureContext(screen, CaptureScreen(screen)));
            }

            foreach (var context in contexts) {
                var overlay = new ScreenCaptureOverlayWindow(mainWindow, this, context);
                overlays.Add(overlay);
                overlay.Show();
            }
        }

        /// <summary>
        /// 切換目前的 active overlay。
        /// 一旦某個螢幕成為 active，其餘螢幕的既有選取會被清掉，
        /// 這樣可以維持「一次只處理一個螢幕、一組選取」的互動模型。
        /// </summary>
        public void ActivateOverlay(ScreenCaptureOverlayWindow overlay) {
            if (overlay == null || isClosing) {
                return;
            }

            ActiveOverlay = overlay;

            foreach (var item in overlays) {
                if (ReferenceEquals(item, overlay) == false) {
                    item.ClearSelectionFromSession();
                }
            }

            if (overlay.IsVisible) {
                overlay.Focus();
            }
        }

        /// <summary>
        /// 讓全域快捷鍵路由到目前的 active overlay。
        /// 如果此時沒有 active overlay，至少仍保留 Esc 可以關閉整個 session。
        /// </summary>
        public void HandleGlobalKeyDown(KeyEventArgs e) {
            if (isClosing) {
                return;
            }

            if (ActiveOverlay != null) {
                ActiveOverlay.HandleGlobalKeyDown(e);
                return;
            }

            if (e.KeyCode == Keys.Escape) {
                Close();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 嘗試取消目前 session 中的 active selection。
        /// 這個方法是讓「在 A 選取、到 B 按右鍵」這類跨視窗操作，
        /// 仍能先取消既有選取，而不是直接結束截圖模式。
        /// </summary>
        public bool CancelActiveSelection() {
            if (ActiveOverlay == null || ActiveOverlay.HasActiveSelection == false) {
                return false;
            }

            ActiveOverlay.ClearSelectionFromSession();
            return true;
        }

        /// <summary>
        /// 結束整個截圖 session，統一關閉所有 overlay 並釋放截圖 bitmap。
        /// </summary>
        public void Close() {
            if (isClosing) {
                return;
            }

            isClosing = true;

            foreach (var overlay in overlays.ToList()) {
                overlay.Close();
            }

            overlays.Clear();

            foreach (var context in contexts) {
                context.Bitmap.Dispose();
            }

            contexts.Clear();
            ActiveOverlay = null;
            RestoreMainWindowAfterCapture();
            mainWindow.ScreenCaptureSession = null;
            mainWindow.TrimWorkingSet();
        }

        /// <summary>
        /// 讓 overlay 在自己關閉時回報 controller。
        /// 如果所有 overlay 都已經關閉，就補做一次完整收尾。
        /// </summary>
        internal void NotifyOverlayClosed(ScreenCaptureOverlayWindow overlay) {
            overlays.Remove(overlay);

            if (ReferenceEquals(ActiveOverlay, overlay)) {
                ActiveOverlay = null;
            }

            if (isClosing == false && overlays.Count == 0) {
                Close();
            }
        }

        /// <summary>
        /// 截圖前先把主視窗移出可見範圍，避免被錄進背景圖。
        /// </summary>
        private void HideMainWindowBeforeCapture() {
            if (mainWindow.ShowInTaskbar == false || mainWindow.WindowState == WindowState.Minimized) {
                return;
            }

            mainWindow.SetWindowInteractionLocked(true);
            mainWindow.WindowStyle = WindowStyle.None;
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Top = -5000;
        }

        /// <summary>
        /// 截圖結束後，把主視窗恢復到原本狀態。
        /// </summary>
        private void RestoreMainWindowAfterCapture() {
            if (mainWindow.ShowInTaskbar == false || previousWindowState == WindowState.Minimized) {
                return;
            }

            mainWindow.Top = previousWindowTop;
            mainWindow.WindowState = previousWindowState;
            mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            mainWindow.SetWindowInteractionLocked(false);
        }

        /// <summary>
        /// 擷取單一螢幕的實際像素畫面。
        /// 每個螢幕各抓一張，可以避開不同螢幕解析度 / 縮放混在同一張大圖時的座標問題。
        /// </summary>
        private static Bitmap CaptureScreen(Screen screen) {
            var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);

            using (var graphics = Graphics.FromImage(bitmap)) {
                graphics.CopyFromScreen(
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    0,
                    0,
                    screen.Bounds.Size,
                    CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }
    }
}

