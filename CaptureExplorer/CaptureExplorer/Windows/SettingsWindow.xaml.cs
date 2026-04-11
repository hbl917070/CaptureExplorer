using CaptureExplorer.Interop;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CaptureExplorer.Windows {
    /// <summary>
    /// 設定視窗。
    /// </summary>
    public partial class SettingsWindow : Window {

        private readonly MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow) {

            _mainWindow = mainWindow;

            InitializeComponent();

            // 讓視窗可以拖曳
            this.MouseLeftButtonDown += ((object sender, MouseButtonEventArgs e) => {
                try {
                    this.DragMove();
                }
                catch { }
            });

            // 初始化快速鍵
            screenshotHotkeyTextBox.Text = _mainWindow.ScreenshotHotkey;
            fullscreenHotkeyTextBox.Text = _mainWindow.FullscreenHotkey;
            activeWindowHotkeyTextBox.Text = _mainWindow.ActiveWindowHotkey;

            customSavePathTextBox.Text = _mainWindow.CustomSavePath;

            if (_mainWindow.IsCustomSavePath) customSavePathRadioButton.IsChecked = true; else defaultSavePathRadioButton.IsChecked = true;
            if (_mainWindow.IsSingleLayerSave) singleLayerSaveModeRadioButton.IsChecked = true; else dualLayerSaveModeRadioButton.IsChecked = true;


            customSavePathTextBox.TextChanged += (sender, e) => {
                _mainWindow.CustomSavePath = customSavePathTextBox.Text;
                if (Directory.Exists(_mainWindow.CustomSavePath) && customSavePathRadioButton.IsChecked.Value) _mainWindow.RefreshTabs();
            };

            customSavePathRadioButton.Checked += (sender, e) => { _mainWindow.IsCustomSavePath = true; if (Directory.Exists(_mainWindow.CustomSavePath) && customSavePathRadioButton.IsChecked.Value) _mainWindow.RefreshTabs(); };
            defaultSavePathRadioButton.Checked += (sender, e) => { _mainWindow.IsCustomSavePath = false; _mainWindow.RefreshTabs(); };

            singleLayerSaveModeRadioButton.Checked += (sender, e) => { _mainWindow.IsSingleLayerSave = true; _mainWindow.RefreshTabs(); };
            dualLayerSaveModeRadioButton.Checked += (sender, e) => { _mainWindow.IsSingleLayerSave = false; _mainWindow.RefreshTabs(); };


            clearScreenshotHotkeyButton.Click += (sender, e) => { screenshotHotkeyTextBox.Text = string.Empty; _mainWindow.ScreenshotHotkey = string.Empty; };
            clearFullscreenHotkeyButton.Click += (sender, e) => { fullscreenHotkeyTextBox.Text = string.Empty; _mainWindow.FullscreenHotkey = string.Empty; };
            clearActiveWindowHotkeyButton.Click += (sender, e) => { activeWindowHotkeyTextBox.Text = string.Empty; _mainWindow.ActiveWindowHotkey = string.Empty; };

            authorHomeButton.Click += (sender, e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://home.gamer.com.tw/homeindex.php?owner=hbl917070");
                }
                catch { }
            };
            forumButton.Click += (sender, e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://forum.gamer.com.tw/C.php?bsn=60076&snA=4337057");
                }
                catch { }
            };
            githubButton.Click += (sender, e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://github.com/hbl917070/CaptureExplorer");
                }
                catch { }
            };
            officialWebsiteButton.Click += (sender, e) => {
                try {
                    System.Diagnostics.Process.Start(@"https://hbl917070.github.io/CaptureExplorer/web/index.html");
                }
                catch { }
            };

            browseCustomPathButton.Click += (sender, e) => {

                var dialog = new FolderSelectDialog {
                    // InitialDirectory = musicFolderTextBox.Text,
                    Title = "Select a folder"
                };
                if (dialog.Show()) {
                    customSavePathTextBox.Text = dialog.FileName;
                    customSavePathRadioButton.IsChecked = true;
                }
            };

            aboutTextBox.Text =
                "版本：2.0.4 (2026 / 04 / 12)" + "\n" +
                "作者：hbl917070 (深海異音)" + "\n" +
                "信箱：hbl917070@gmail.com";

            this.Closed += (sender, e) => { _mainWindow.SettingsWindow = null; };

            helpBrowser.Navigate(_mainWindow.GetExecutablePath() + "\\Data\\Explain\\index.html");
            helpBrowser.IsWebBrowserContextMenuEnabled = false; // 禁止右鍵
            helpBrowser.WebBrowserShortcutsEnabled = false; // 禁止快速鍵
            helpBrowser.AllowNavigation = false; // 載入完成後，禁止瀏覽其他網頁
        }
    }
}

