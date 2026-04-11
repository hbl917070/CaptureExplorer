using System;
using System.Drawing;
using System.Windows;
using System.Windows.Resources;

namespace CaptureExplorer.Controllers {
    /// <summary>
    /// Wraps the notify icon shown in the Windows system tray for the main window.
    /// </summary>
    public class TrayIconController {
        private readonly System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        private readonly MainWindow mainWindow;

        /// <summary>
        /// Creates the tray icon and wires its shortcuts back to the main window.
        /// </summary>
        public TrayIconController(MainWindow mainWindow) {
            this.mainWindow = mainWindow;

            var uri = new Uri("Imgs/Icon.ico", UriKind.RelativeOrAbsolute);
            StreamResourceInfo streamResourceInfo = Application.GetResourceStream(uri);
            notifyIcon.Icon = new Icon(streamResourceInfo.Stream);
            notifyIcon.Text = "Capture Explorer";
            notifyIcon.Visible = true;

            notifyIcon.DoubleClick += (sender2, e2) => {
                this.mainWindow.SetMinimizedToTray(false);
            };

            var contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.Add("Show", new EventHandler((sender2, e2) => {
                this.mainWindow.SetMinimizedToTray(false);
            }));
            contextMenu.MenuItems.Add("Screenshot", new EventHandler((sender2, e2) => {
                this.mainWindow.CaptureScreenshot();
            }));

            notifyIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// Shows the tray icon.
        /// </summary>
        public void ShowTrayIcon() {
            notifyIcon.Visible = true;
        }

        /// <summary>
        /// Hides the tray icon.
        /// </summary>
        public void HideTrayIcon() {
            notifyIcon.Visible = false;
        }
    }
}
