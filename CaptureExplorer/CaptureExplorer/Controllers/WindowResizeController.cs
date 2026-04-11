using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace CaptureExplorer.Controllers {
    /// <summary>
    /// Provides the native resize hooks used by the borderless main window.
    /// </summary>
    public class WindowResizeController {
        private const int WM_SYSCOMMAND = 0x112;

        private readonly Window mainWindow;
        private HwndSource hwndSource;

        public WindowResizeController(Window m) {
            mainWindow = m;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            return IntPtr.Zero;
        }

        /// <summary>
        /// Hooks the native window procedure after the WPF window handle is created.
        /// </summary>
        public void MainWindow_SourceInitialized(object sender, EventArgs e) {
            hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            hwndSource.AddHook(WndProc);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public enum ResizeDirection {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
            Move = 9
        }

        public void ResizeWindow(ResizeDirection direction) {
            SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
        }

        public void ResetCursor(object sender, MouseEventArgs e) {
            if (Mouse.LeftButton != MouseButtonState.Pressed) {
                mainWindow.Cursor = Cursors.Arrow;
            }
        }
    }
}
