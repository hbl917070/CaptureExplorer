using System;
using System.Windows;
using System.Windows.Input;

namespace CaptureExplorer.Dialogs {
    /// <summary>
    /// 簡易對話框與提示訊息視窗。
    /// </summary>
    public partial class MessageDialog : Window {
        private MainWindow _mainWindow;
        private Action _onConfirm = null;

        public MessageDialog() {
            InitializeComponent();
        }

        public MessageDialog(MainWindow m) {
            InitializeComponent();
            _mainWindow = m;

            MouseLeftButtonDown += (sender, e) => {
                DragMove();
            };

            _mainWindow.SetWindowInteractionLocked(true);
            Closing += (sender, e) => {
                _mainWindow.SetWindowInteractionLocked(false);
            };

            _onConfirm = () => {
                Close();
            };

            but_yes.Click += (sender, e) => {
                _onConfirm();
                Close();
            };
            but_no.Click += (sender, e) => {
                Close();
            };

            KeyDown += (sender, e) => {
                if (e.Key == Key.Escape) {
                    Close();
                }
                else if (e.Key == Key.Enter) {
                    _onConfirm();
                    Close();
                }
            };
        }

        public void SetTitle(string s) {
            Title = s;
        }

        public void SetText(string s) {
            textBox.Text = s;
        }

        public void SetConfirmAction(Action confirmAction) {
            _onConfirm = confirmAction;
        }

        public void HideCancelButton() {
            but_no.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 顯示對話框並指定擁有者視窗。
        /// </summary>
        public void ShowDialogWindow(Window w) {
            Owner = w;
            Show();
        }

        /// <summary>
        /// 顯示一般提示訊息。
        /// </summary>
        public void ShowInfoMessage(Window w, string s) {
            MessageBox.Show(s);
        }
    }
}

