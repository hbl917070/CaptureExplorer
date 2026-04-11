using CaptureExplorer.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace CaptureExplorer.Controls {
    /// <summary>
    /// 分頁標籤。
    /// </summary>
    public partial class TabHeader : UserControl {
        private TabContextMenu _contextMenu;
        private MainWindow _mainWindow;

        public TabHeader() {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化右鍵選單。
        /// </summary>
        public void InitializeItem(MainWindow mainWindow) {
            _mainWindow = mainWindow;
            _contextMenu = new TabContextMenu(mainWindow);

            MouseRightButtonUp += (sender, e) => {
                _contextMenu.OpenBesideCursor();
            };

            _contextMenu.AddItem("重新整理", null, () => {
                _mainWindow.RefreshFolderView();
            });
            _contextMenu.AddItem("開啟資料夾位置", null, () => {
                _mainWindow.OpenCurrentFolder();
            });
            _contextMenu.AddItem("複製路徑", null, () => {
                var path = _mainWindow.GetSaveFolderPath();
                try {
                    Clipboard.SetData(DataFormats.UnicodeText, path);
                }
                catch {
                    // 剪貼簿可能暫時無法寫入，略過即可。
                }
            });
            _contextMenu.AddItem("重新命名", null, () => {
                var dialog = new TabFolderDialog(_mainWindow);
                dialog.PrepareRename();
                dialog.Show();
                dialog.Owner = _mainWindow;
            });
            _contextMenu.AddItem("刪除資料夾", null, () => {
                _mainWindow.DeleteCurrentFolder();
            });
        }

        public string Text {
            get => HeaderText.Text;
            set => HeaderText.Text = value;
        }
    }
}

