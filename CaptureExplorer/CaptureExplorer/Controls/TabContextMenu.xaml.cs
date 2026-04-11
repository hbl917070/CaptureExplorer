using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CaptureExplorer.Controls {
    /// <summary>
    /// 分頁右鍵選單容器。
    /// </summary>
    public partial class TabContextMenu : UserControl {
        private readonly MainWindow _mainWindow;

        public TabContextMenu(MainWindow mainWindow) {
            _mainWindow = mainWindow;
            InitializeComponent();
            popupMenu.StaysOpen = false;
        }

        /// <summary>
        /// 新增選單項目。
        /// </summary>
        public TabContextMenuItem AddItem(string text, BitmapSource image, Action onClick) {
            var item = new TabContextMenuItem();
            item.MenuText.Text = text;
            if (image != null) {
                item.MenuIcon.Source = image;
            }

            item.MenuButton.Click += (sender, e) => {
                popupMenu.IsOpen = false;
                onClick();
            };

            itemsPanel.Children.Add(item);
            return item;
        }

        /// <summary>
        /// 新增分隔線。
        /// </summary>
        public void AddSeparator() {
            var item = new Border {
                Background = new SolidColorBrush(Color.FromArgb(76, 255, 255, 255)),
                Margin = new Thickness(20, 10, 20, 10),
                Height = 1
            };
            itemsPanel.Children.Add(item);
        }

        /// <summary>
        /// 在指定物件下方開啟選單。
        /// </summary>
        public void OpenBelowElement(FrameworkElement targetElement) {
            MainWindow.AnimateElement(popupMenuContainer, -10, 0, "Y", () => { });
            popupMenu.PlacementTarget = targetElement;
            popupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            popupMenu.IsOpen = true;
            popupMenu.HorizontalOffset = 0;
            popupMenu.VerticalOffset =
                (popupMenuContainer.ActualHeight / _mainWindow.DpiScaleY / 2) +
                (targetElement.ActualHeight / 2) - 15;
        }

        /// <summary>
        /// 在滑鼠下方開啟選單。
        /// </summary>
        public void OpenBelowCursor() {
            MainWindow.AnimateElement(popupMenuContainer, -10, 0, "Y", () => { });
            popupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
            popupMenu.IsOpen = true;

            var cursorPosition = System.Windows.Forms.Cursor.Position;
            popupMenu.HorizontalOffset =
                cursorPosition.X / _mainWindow.DpiScaleX -
                (popupMenuContainer.ActualWidth / _mainWindow.DpiScaleX / 2);
            popupMenu.VerticalOffset = cursorPosition.Y / _mainWindow.DpiScaleY - 35;
        }

        /// <summary>
        /// 在滑鼠旁邊開啟選單。
        /// </summary>
        public void OpenBesideCursor() {
            MainWindow.AnimateElement(popupMenuContainer, -10, 0, "Y", () => { });
            popupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
            popupMenu.IsOpen = true;

            var cursorPosition = System.Windows.Forms.Cursor.Position;
            popupMenu.HorizontalOffset = cursorPosition.X / _mainWindow.DpiScaleX;
            popupMenu.VerticalOffset = cursorPosition.Y / _mainWindow.DpiScaleY - 15;
        }
    }
}

