using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using CaptureExplorer.Controls;
using CaptureExplorer.Services;

namespace CaptureExplorer.Dialogs {
    /// <summary>
    /// 新增、重新命名與刪除分頁資料夾的對話框。
    /// </summary>
    public partial class TabFolderDialog : Window {
        private readonly MainWindow _mainWindow;
        private readonly List<string> _existingTabNames;
        private Action _onConfirm;

        public TabFolderDialog(MainWindow mainWindow) {
            InitializeComponent();

            _mainWindow = mainWindow;
            Owner = _mainWindow;
            _mainWindow.SetWindowInteractionLocked(true);

            Closed += (sender, e) => {
                _mainWindow.SetWindowInteractionLocked(false);
            };

            but_no.Click += (sender, e) => Close();
            but_yes.Click += (sender, e) => _onConfirm();

            MouseLeftButtonDown += (sender, e) => DragMove();

            _existingTabNames = new List<string>();
            var tabItems = _mainWindow.TabsStackPanel.Children;
            for (int i = 0; i < tabItems.Count; i++) {
                _existingTabNames.Add(((TabHeader)tabItems[i]).Text);
            }

            KeyDown += (sender, e) => {
                if (e.Key == Key.Escape) {
                    Close();
                }
                else if (e.Key == Key.Enter) {
                    _onConfirm();
                }
            };
        }

        /// <summary>
        /// 延遲全選輸入框內容，方便直接覆蓋名稱。
        /// </summary>
        private void SelectAllText() {
            new Thread(() => {
                Thread.Sleep(100);
                UiThreadAdapter.RunOnUiThread(() => {
                    textBox_tabName.Focus();
                    textBox_tabName.SelectAll();
                });
            }).Start();
        }

        /// <summary>
        /// 切換成新增資料夾模式。
        /// </summary>
        public void PrepareCreate() {
            Title = "新增資料夾";
            SelectAllText();

            for (int i = 1; i < 999; i++) {
                if (!_existingTabNames.Contains("New Folder " + i)) {
                    textBox_tabName.Text = "New Folder " + i;
                    break;
                }
            }

            _onConfirm = () => {
                textBox_tabName.Text = textBox_tabName.Text.Trim();

                if (textBox_tabName.Text.Length > 100) {
                    new MessageDialog(_mainWindow).ShowInfoMessage(this, "名字太長了");
                    return;
                }

                if (textBox_tabName.Text.Replace(".", "") == "") {
                    new MessageDialog(_mainWindow).ShowInfoMessage(this, "名字不要空白啦");
                    return;
                }

                string[] invalidChars = { "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
                for (int i = 0; i < invalidChars.Length; i++) {
                    if (textBox_tabName.Text.Contains(invalidChars[i])) {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "資料夾名字不支援以下符號：\n / \\ : * ? \" < > |");
                        return;
                    }
                }

                if (_existingTabNames.Contains(textBox_tabName.Text)) {
                    new MessageDialog(_mainWindow).ShowInfoMessage(this, "「" + textBox_tabName.Text + "」已存在，請用別的名字");
                    return;
                }

                CreateTab(textBox_tabName.Text);
                Close();
            };
        }

        /// <summary>
        /// 建立新資料夾並加入分頁列。
        /// </summary>
        private void CreateTab(string folderName) {
            string newFolderPath = _mainWindow.GetSaveRootPath() + "\\" + folderName;
            if (!Directory.Exists(newFolderPath)) {
                Directory.CreateDirectory(newFolderPath);
            }

            var tabHeader = new TabHeader { Text = folderName };
            tabHeader.InitializeItem(_mainWindow);
            _mainWindow.TabController.RegisterItemEvents(tabHeader);
            _mainWindow.TabController.SetSelectedItem(tabHeader);
        }

        /// <summary>
        /// 切換成重新命名模式。
        /// </summary>
        public void PrepareRename() {
            Loaded += (sender, e) => {
                try {
                    textBox_tabName.Text = _mainWindow.TabController.SelectedTab.Text;
                }
                catch {
                    Close();
                    return;
                }

                Title = "修改資料夾名稱";
                lab_yes.Content = "修改";
                SelectAllText();

                _onConfirm = () => {
                    textBox_tabName.Text = textBox_tabName.Text.Trim();

                    if (_mainWindow.TabController.SelectedTab.Text == textBox_tabName.Text) {
                        Close();
                        return;
                    }

                    if (textBox_tabName.Text.Length > 100) {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "名字太長了啦");
                        return;
                    }

                    if (textBox_tabName.Text.Replace(".", "") == "") {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "名字不要空白啦");
                        return;
                    }

                    string[] invalidChars = { "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
                    for (int i = 0; i < invalidChars.Length; i++) {
                        if (textBox_tabName.Text.Contains(invalidChars[i])) {
                            new MessageDialog(_mainWindow).ShowInfoMessage(this, "資料夾名稱不支援以下符號：\n/ \\ : * ? \" < > |");
                            return;
                        }
                    }

                    if (_existingTabNames.Contains(textBox_tabName.Text)) {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "「" + textBox_tabName.Text + "」已存在，請用別的名字");
                        return;
                    }

                    string oldFolderPath = _mainWindow.GetSaveRootPath() + "\\" + _mainWindow.TabController.SelectedTab.Text;
                    string newFolderPath = _mainWindow.GetSaveRootPath() + "\\" + textBox_tabName.Text;

                    if (!Directory.Exists(oldFolderPath)) {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "資料夾不存在「" + textBox_tabName.Text + "」" + "\n看到這個訊息的話，最好趕快重新啟動程式");
                        return;
                    }

                    if (Directory.Exists(newFolderPath)) {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "資料夾「" + textBox_tabName.Text + "」已存在，請用別的名字哦" + "\n看到這個訊息的話，最好趕快重新啟動程式");
                        return;
                    }

                    Directory.Move(oldFolderPath, newFolderPath);
                    _mainWindow.TabController.SelectedTab.Text = textBox_tabName.Text;
                    _mainWindow.RefreshFolderView();
                    Close();
                };
            };
        }

        /// <summary>
        /// 切換成刪除確認模式。
        /// </summary>
        public void PrepareDelete() {
            Loaded += (sender, e) => {
                try {
                    textBox_tabName.Text = _mainWindow.TabController.SelectedTab.Text;
                }
                catch {
                    Close();
                    return;
                }

                textBox_tabName.IsReadOnly = true;
                Title = "刪除資料夾";
                lab_yes.Content = "刪除";

                _onConfirm = () => {
                    string folderPath = _mainWindow.GetSaveRootPath() + "\\" + _mainWindow.TabController.SelectedTab.Text;
                    if (!Directory.Exists(folderPath)) {
                        new MessageDialog(_mainWindow).ShowInfoMessage(this, "檔案不存在「" + textBox_tabName.Text + "」" + "\n看到這個訊息的話，最好趕快重新啟動程式");
                        return;
                    }

                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                        folderPath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                    _mainWindow.TabController.DeleteSelectedItem();
                    if (_mainWindow.TabsStackPanel.Children.Count == 0) {
                        CreateTab("New Folder 1");
                    }

                    Close();
                };
            };
        }
    }
}

