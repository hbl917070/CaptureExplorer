using CaptureExplorer.Controllers;
using CaptureExplorer.Controls;
using CaptureExplorer.Dialogs;
using CaptureExplorer.Interop;
using CaptureExplorer.Services;
using CaptureExplorer.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static CaptureExplorer.Controllers.WindowResizeController;

namespace CaptureExplorer {
    /// <summary>
    /// 主視窗
    /// </summary>
    public partial class MainWindow : Window {

        private SettingsService Settings;
        // 快速鍵預設值
        /// <summary> 截圖快捷鍵文字表示 </summary>
        public string ScreenshotHotkey = "RCtrl + PrtScrSysRq";
        /// <summary> 全螢幕截圖快捷鍵 </summary>
        public string FullscreenHotkey = "PrtScrSysRq";
        /// <summary> 目前視窗截圖快捷鍵 </summary>
        public string ActiveWindowHotkey = "RAlt + PrtScrSysRq";
        /// <summary> 自訂的儲存路徑 </summary>
        public string CustomSavePath = "D:\\圖片";
        /// <summary> 啟用自訂路徑 </summary>
        public bool IsCustomSavePath = false;
        /// <summary> 啟用單層儲存 </summary>
        public bool IsSingleLayerSave = false;


        public SettingsWindow SettingsWindow;
        public TabDragController TabController;
        TrayIconController trayIconController;
        public string ImagesFolderName = "Imgs";
        WebBrowserOverlay webOverlay;
        WindowResizeController windowResizeController;

        public double DpiScaleX = 1;
        public double DpiScaleY = 1;

        public System.Windows.Forms.WebBrowser FolderWebBrowser;

        public MainWindow() {

            InitializeComponent();

            UiThreadAdapter.Initialize();

            windowResizeController = new WindowResizeController(this);
            this.SourceInitialized += new System.EventHandler(windowResizeController.MainWindow_SourceInitialized);

            // Initialize web view overlay
            webOverlay = new WebBrowserOverlay(border_web);
            FolderWebBrowser = webOverlay.WebBrowser;

            // Initialize tab controller (use renamed TabsStackPanel)
            TabController = new TabDragController(TabsStackPanel);
            TabController.OnTabChanged = OnTabChange;
            TabController.OnTabClick = OnTabClick;

            trayIconController = new TrayIconController(this);
            trayIconController.HideTrayIcon();

            Settings = new SettingsService(this);
            Settings.LoadSettingsOnStartup();

            // 初始化分頁
            RefreshTabs(true);

            // 讓視窗可透過拖曳移動或雙擊最大化/還原
            ToolbarScrollViewer.MouseLeftButtonDown += ToolbarScrollViewer_MouseLeftButtonDown;
            LblTitle.MouseLeftButtonDown += LblTitle_MouseLeftButtonDown;
            TabsScrollContainer.MouseLeftButtonDown += TabsScrollContainer_MouseLeftButtonDown;

            // 雙擊切換最大化/還原
            ToolbarScrollViewer.MouseDoubleClick += ToolbarScrollViewer_MouseDoubleClick;
            LblTitle.MouseDoubleClick += LblTitle_MouseDoubleClick;

            // 讓工具列可以水平捲動
            ((UIElement)ToolbarScrollViewer.Content).MouseWheel += ToolbarScrollViewer_MouseWheel;

            BtnCapture.Click += BtnCapture_Click;

            BtnClear.Click += BtnClear_Click;

            BtnRefresh.Click += BtnRefresh_Click;

            BtnOpenFolder.Click += BtnOpenFolder_Click;

            BtnSettings.Click += BtnSettings_Click;

            // 鎖定視窗在最上層
            BtnLockWindow.Click += BtnLockWindow_Click;

            BtnMinimizeToTray.Click += BtnMinimizeToTray_Click;

            // 分頁容器的右鍵選單
            var tabContextMenu = new TabContextMenu(this);
            TabsScrollContainer.MouseRightButtonUp += (sender, e) => { if (e.OriginalSource == TabsScrollContainer) tabContextMenu.OpenBesideCursor(); };
            BtnAddTab.MouseRightButtonUp += (sender, e) => tabContextMenu.OpenBesideCursor();
            tabContextMenu.AddItem("重新整理", null, () => RefreshTabs());
            tabContextMenu.AddItem("開啟資料夾位置", null, () => {
                try { Process.Start("explorer.exe", "\"" + GetSaveRootPath().Replace("/", "\\") + "\""); } catch { }
            });
            tabContextMenu.AddItem("新增", null, () => BtnAddTab_Click(null, null));


            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, this.OnCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, this.OnMaximizeWindow, this.OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, this.OnMinimizeWindow, this.OnCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, this.OnRestoreWindow, this.OnCanResizeWindow));

            this.Closing += (sender, e) => { var dataPath = System.IO.Path.Combine(GetExecutablePath(), "data"); if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath); Settings.SaveSettings(); };

            this.Loaded += MainWindow_Loaded;
        }

        // --- Event handler methods (replace previous anonymous lambdas) ---
        private void OnTabClick(string path) {
            var folderPath = System.IO.Path.Combine(GetSaveRootPath(), path);
            if (IsSingleLayerSave) folderPath = GetSaveRootPath();
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            FolderWebBrowser.Navigate(folderPath);
        }

        private void OnTabChange(string path) { }

        private void ToolbarScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            try { windowResizeController.ResizeWindow(ResizeDirection.Move); } catch { }
        }

        private void LblTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            try { windowResizeController.ResizeWindow(ResizeDirection.Move); } catch { }
        }

        private void TabsScrollContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var obj = e.OriginalSource;
            if (obj == TabsScrollContainer) try { this.DragMove(); } catch { }
        }

        private void ToolbarScrollViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) return;
            var obj = e.OriginalSource;
            Console.WriteLine(obj);
            if (obj is Border || obj is System.Windows.Shapes.Path) return;
            WindowState = WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }
        private void LblTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) return;
            WindowState = WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        private void ToolbarScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e) {
            var x = e.Delta;
            if (x > 0) ToolbarScrollViewer.LineLeft(); else ToolbarScrollViewer.LineRight();
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e) {
            CaptureScreenshot();
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e) {
            DeleteCurrentFolder();
        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) {
            RefreshFolderView();
        }
        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e) {
            OpenCurrentFolder();
        }
        private void BtnSettings_Click(object sender, RoutedEventArgs e) {
            if (SettingsWindow != null) { SettingsWindow.Close(); SettingsWindow = null; }
            SettingsWindow = new SettingsWindow(this) { Topmost = this.Topmost, Owner = this };
            SettingsWindow.Show();
        }

        private void BtnLockWindow_Click(object sender, RoutedEventArgs e) {
            SetWindowTopmost(this, "auto");
        }

        private void BtnMinimizeToTray_Click(object sender, RoutedEventArgs e) {
            SetMinimizedToTray(true);
        }

        /// <summary>
        /// 根據是否為單層儲存模式，更新分頁與檢視的佈局
        /// </summary>
        public void UpdateSingleLayerLayout() {
            if (IsSingleLayerSave) {
                WebContainerBorder.SetValue(Grid.ColumnProperty, 0);
                border_web.Margin = new Thickness(7, 0, 7, 7);
            }
            else {
                WebContainerBorder.SetValue(Grid.ColumnProperty, 1);
                border_web.Margin = new Thickness(0, 0, 7, 7);
            }
        }

        /// <summary>
        /// 視窗載入完成
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            var windowAeroEffect = new WindowAeroEffect();
            windowAeroEffect.TryEnableBlur(this);

            new GlobalHotkeyMonitor(this);

            // 取得解析度偏差
            var source = PresentationSource.FromVisual(this);
            DpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            DpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        /// <summary>
        /// 回傳資料夾順序的字串
        /// </summary>
        public string GetSubfolderOrder() {
            // 儲存資料夾順序
            var sum = new StringBuilder();
            foreach (var item in TabsStackPanel.Children) {
                var u = (TabHeader)item;
                sum.Append(u.Text).Append('\t');
            }
            return sum.ToString();
        }

        /// <summary>
        /// 重新產生分頁的頁籖
        /// </summary>
        public void RefreshTabs(bool isFirstRun = false) {
            UpdateSingleLayerLayout();

            if (!isFirstRun) {
                // 儲存目前的設定值
                Settings.FolderOrder = GetSubfolderOrder();
                Settings.SelectedFolder = TabController.SelectedTab.Text;
            }

            var saveRoot = GetSaveRootPath();

            // 讀取
            var orderedDirs = new List<string>();

            // 之前儲存的設定值
            foreach (var item in Settings.FolderOrder.Split('\t')) {
                if (item.Trim() != string.Empty && Directory.Exists(System.IO.Path.Combine(saveRoot, item.Trim()))) orderedDirs.Add(item);
            }

            // 實際存在的資料夾
            var existingDirs = Directory.GetDirectories(saveRoot);
            Array.Sort(existingDirs, new NaturalOrderComparer()); // 自然排序

            foreach (var item in existingDirs) {
                var dirName = System.IO.Path.GetFileName(item);
                if (Directory.Exists(item) && !orderedDirs.Contains(dirName)) orderedDirs.Add(dirName);
            }

            // 如果沒有子資料夾
            if (orderedDirs.Count == 0) {
                // 新增資料夾
                if (!IsSingleLayerSave) Directory.CreateDirectory(System.IO.Path.Combine(saveRoot, "New Folder 1"));
                orderedDirs.Add("New Folder 1");
            }

            // 刪除所有項目
            TabController.DeleteAllItems();

            foreach (var item in orderedDirs) {
                var bu = new TabHeader {
                    Text = item
                };
                bu.InitializeItem(this);
                TabController.RegisterItemEvents(bu);

                if (item == Settings.SelectedFolder) TabController.SetSelectedItem(bu);
            }

            // 如果都沒有選擇任何頁籖，就用選擇第一個
            if (TabController.SelectedTab == null) TabController.SetSelectedItem((TabHeader)TabsStackPanel.Children[0]);

            // 儲存目前的設定值
            Settings.FolderOrder = GetSubfolderOrder();
            Settings.SelectedFolder = TabController.SelectedTab.Text;
        }

        /// <summary>
        /// 取得圖片儲存的目錄
        /// </summary>
        public string GetSaveFolderPath() {
            var p = System.IO.Path.Combine(GetSaveRootPath(), TabController.SelectedTab.Text);
            if (IsSingleLayerSave) p = GetSaveRootPath();
            if (!Directory.Exists(p)) {
                Directory.CreateDirectory(p);
                RefreshFolderView();
            }
            return p;
        }

        /// <summary>
        /// 取得儲存 子資料夾 的根目錄資料夾
        /// </summary>
        /// <returns></returns>
        public string GetSaveRootPath() {
            // 避免結尾有多餘的 \\
            CustomSavePath = CustomSavePath.Replace("/", "\\");
            if (CustomSavePath.EndsWith("\\")) CustomSavePath = CustomSavePath.TrimEnd('\\');

            var rootPath = System.IO.Path.Combine(GetExecutablePath(), ImagesFolderName);

            if (IsCustomSavePath) {
                if (Directory.Exists(CustomSavePath)) rootPath = CustomSavePath;
                else IsCustomSavePath = false;
            }

            // 如果資料夾不存在，就新建
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            return rootPath;
        }

        /// <summary>
        /// 回傳圖片的儲存路徑
        /// </summary>
        /// <param name="type">檔案類型，例如 jpg 或 png </param>
        /// <returns></returns>
        public string GetSaveFilePath(string type) {
            if (type != string.Empty) type = "." + type;

            var root = GetSaveFolderPath();
            var name = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            var savePath = System.IO.Path.Combine(root, name + type);

            // 如果檔案已經存在
            if (File.Exists(savePath)) {
                for (int i = 1; i <= 99999; i++) {
                    savePath = System.IO.Path.Combine(root, name + $" ({i})" + type);
                    if (!File.Exists(savePath)) return savePath;
                }
            }

            return savePath;
        }

        /// <summary>
        /// 設定視窗是否縮小到右下角的系統托盤
        /// </summary>
        public void SetMinimizedToTray(bool enableMinimize) {
            if (enableMinimize) {
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = false;
                this.Visibility = Visibility.Collapsed;
                trayIconController.ShowTrayIcon();
                webOverlay.Visibility = Visibility.Collapsed;
            }
            else {
                this.ShowInTaskbar = true;
                this.Visibility = Visibility.Visible;
                trayIconController.HideTrayIcon();
                webOverlay.Visibility = Visibility.Visible;
            }
        }

        public ScreenCaptureSessionController ScreenCaptureSession { get; set; }

        /// <summary>
        /// 進入截圖模式
        /// </summary>
        public void CaptureScreenshot() {
            if (ScreenCaptureSession != null)
                return;

            ScreenCaptureSession = new ScreenCaptureSessionController(this);
            ScreenCaptureSession.Start();
        }

        /// <summary>
        /// 全螢幕截圖
        /// </summary>
        public void CaptureFullscreenScreenshot() {
            var captureBounds = GetVirtualDesktopBounds();
            using (var image = CaptureVirtualDesktop(captureBounds)) {
                string savePath = GetSaveFilePath("png");
                image.Save(savePath);

                try {
                    if (ChkAutoCopyToClipboard.IsChecked.Value) {
                        Clipboard.SetData(DataFormats.Bitmap, image);
                    }
                }
                catch { }
            }

            var we = new ScreenshotFlashWindow();
            we.Show();
            we.ShowFullscreen();
        }

        /// <summary>
        /// 擷取當前活動視窗的畫面
        /// </summary>
        public void CaptureActiveWindowScreenshot() {
            // 取得該視窗的大小與位置
            System.Drawing.Rectangle bounds = new System.Drawing.Rectangle();
            GetWindowRect(GetForegroundWindow(), ref bounds);

            bounds.Width = bounds.Width - bounds.X;
            bounds.Height = bounds.Height - bounds.Y;

            var we = new ScreenshotFlashWindow();
            we.Show();
            we.ShowPartial(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            using (var img = new System.Drawing.Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                var gfx = System.Drawing.Graphics.FromImage(img);
                gfx.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, System.Drawing.CopyPixelOperation.SourceCopy);


                string savePath = GetSaveFilePath("png");

                img.Save(savePath);

                //自動存入剪貼簿
                try {
                    if (ChkAutoCopyToClipboard.IsChecked.Value) {
                        Clipboard.SetData(DataFormats.Bitmap, img);
                    }
                }
                catch { }
            }

        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref System.Drawing.Rectangle bounds);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 取得整個虛擬桌面的範圍。
        /// 多螢幕時會從最左上角一路包到最右下角，供全螢幕截圖使用。
        /// </summary>
        private static System.Drawing.Rectangle GetVirtualDesktopBounds() {
            return new System.Drawing.Rectangle(
                System.Windows.Forms.SystemInformation.VirtualScreen.Left,
                System.Windows.Forms.SystemInformation.VirtualScreen.Top,
                System.Windows.Forms.SystemInformation.VirtualScreen.Width,
                System.Windows.Forms.SystemInformation.VirtualScreen.Height);
        }

        /// <summary>
        /// 擷取整個虛擬桌面的畫面，作為全螢幕截圖輸出。
        /// 這條路徑不再依賴舊的 W_screenshot 視窗。
        /// </summary>
        private static System.Drawing.Bitmap CaptureVirtualDesktop(System.Drawing.Rectangle bounds) {
            var image = new System.Drawing.Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = System.Drawing.Graphics.FromImage(image)) {
                graphics.CopyFromScreen(
                    bounds.X,
                    bounds.Y,
                    0,
                    0,
                    bounds.Size,
                    System.Drawing.CopyPixelOperation.SourceCopy);
            }

            return image;
        }

        /// <summary>
        /// 設定視窗是否固定在最上層
        /// </summary>
        /// <param name="type"> "auto"（自動切換）、"true"（固定在最上層）、"false"（不固定在最上層） </param>
        public void SetWindowTopmost(MainWindow m, string type) {
            var current = true;
            var mode = (type ?? string.Empty).ToLowerInvariant();
            if (mode == "auto" || mode == string.Empty) current = !m.Topmost;
            if (mode == "false") current = false;
            if (mode == "no") current = m.Topmost;

            if (!current) {
                m.BtnLockWindow.Background = new SolidColorBrush { Color = Color.FromScRgb(0, 0, 0, 0) };
                m.Topmost = false;
            }
            else {
                m.BtnLockWindow.Background = new SolidColorBrush { Color = Color.FromScRgb(0.2f, 1, 1, 1) };
                m.Topmost = true;
            }
        }

        /// <summary>
        /// 刷新資料夾檢視
        /// </summary>
        public void RefreshFolderView() {
            try {
                FolderWebBrowser.Navigate(GetSaveFolderPath());
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        public void OpenCurrentFolder() {
            try {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + GetSaveFolderPath().Replace("/", "\\") + "\"");
            }
            catch { }
        }

        /// <summary>
        /// 刪除當前資料夾
        /// </summary>
        public void DeleteCurrentFolder() {
            if (IsSingleLayerSave) {
                var w = new MessageDialog(this);
                w.SetText("\n確定要把「" + System.IO.Path.GetFileName(GetSaveFolderPath()) + "」資料夾\n丟到「資源垃圾桶」？\n");
                w.SetConfirmAction(() => {
                    // 把資料夾移到垃圾桶，並顯示詢問視窗
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(GetSaveFolderPath(),
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                });
                w.ShowDialogWindow(this);
            }
            else {
                var w = new MessageDialog(this);
                w.SetText("\n確定要把「" + TabController.SelectedTab.Text + "」資料夾\n丟到「資源垃圾桶」？\n");
                w.SetConfirmAction(() => {
                    // 把資料夾移到垃圾桶，並顯示詢問視窗
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(GetSaveFolderPath(),
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                    TabController.DeleteSelectedItem();

                    // 如果已經沒有子資料夾，就新增一個
                    if (TabsStackPanel.Children.Count == 0) {
                        RefreshTabs();
                    }

                });
                w.ShowDialogWindow(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void BtnAddTab_Click(object sender, RoutedEventArgs e) {
            TabFolderDialog w = new TabFolderDialog(this);
            w.PrepareCreate();
            w.Show();
            w.Owner = this;
        }

        /// <summary>
        /// 取得可執行檔路徑
        /// </summary>
        public string GetExecutablePath() => System.Windows.Forms.Application.StartupPath;

        /// <summary>
        /// 設定視窗互動鎖定
        /// </summary>
        /// <param name="isLocked"></param>
        public void SetWindowInteractionLocked(bool isLocked) {
            try {
                // 如果視窗已經縮小至右下角，則不啟用
                if (!this.ShowInTaskbar) return;

                if (isLocked) {
                    this.IsEnabled = false;
                    // webOverlay.Visibility = Visibility.Collapsed;
                }
                else {
                    this.IsEnabled = true;
                    // webOverlay.Visibility = Visibility.Visible;
                }
            }
            catch {
                // 如果程式沒有正常退出，就強制結束程式
                System.Environment.Exit(System.Environment.ExitCode);
            }
        }

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        private void OnCloseWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.CloseWindow(this);
        }
        private void OnMaximizeWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.MaximizeWindow(this);
        }
        private void OnMinimizeWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.MinimizeWindow(this);
        }
        private void OnRestoreWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.RestoreWindow(this);
        }


        /// <summary>
        /// 對指定元素執行位移與淡入動畫，完成後執行 onComplete 動作
        /// </summary>
        /// <param name="element"> 要執行動畫的元素 </param>
        /// <param name="from"> 位移的起始位置 </param>
        /// <param name="to"> 位移的目標位置 </param>
        /// <param name="movementType"> "X" 或 "Y"，表示水平或垂直位移 </param>
        /// <param name="onComplete"> 動畫完成後要執行的動作 </param>
        public static void AnimateElement(FrameworkElement element, double from, double to, string movementType, Action onComplete) {

            if (element == null)
                return;

            movementType = movementType.ToUpper();

            // 位移
            Storyboard storyboard2 = new Storyboard();
            DoubleAnimation growAnimation2 = new DoubleAnimation();
            growAnimation2.Duration = (Duration)TimeSpan.FromSeconds(0.15f);

            growAnimation2.Completed += (sender, e) => { // 完成時執行
                onComplete();
            };

            element.RenderTransform = new TranslateTransform();

            growAnimation2.From = from;
            growAnimation2.To = to;

            Storyboard.SetTargetProperty(growAnimation2, new PropertyPath("(FrameworkElement.RenderTransform).(TranslateTransform." + movementType + ")"));
            Storyboard.SetTarget(growAnimation2, element);

            storyboard2.Children.Add(growAnimation2);
            storyboard2.Begin();

            // ----------------------

            Storyboard storyboard3 = new Storyboard();
            DoubleAnimation growAnimation3 = new DoubleAnimation();
            growAnimation3.Duration = (Duration)TimeSpan.FromSeconds(0.15f);

            growAnimation3.From = 0.5;
            growAnimation3.To = 1;

            Storyboard.SetTargetProperty(growAnimation3, new PropertyPath("Opacity"));
            Storyboard.SetTarget(growAnimation3, element);

            storyboard3.Children.Add(growAnimation3);
            storyboard3.Begin();
        }

        /// <summary>
        /// 清理記憶體
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        public void TrimWorkingSet() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

        /// <summary>
        /// 自然排序
        /// </summary>
        public class NaturalOrderComparer : IComparer<string> {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int StrCmpLogicalW(string x, string y);
            public int Compare(string x, string y) => StrCmpLogicalW(x, y);
        }

    }
}

