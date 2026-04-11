using GlobalInputHooks;
using System.Windows.Forms;

namespace CaptureExplorer.Services {
    /// <summary>
    /// 快速鍵
    /// </summary>
    public class GlobalHotkeyMonitor {
        private readonly MainWindow mainWindow;

        public GlobalHotkeyMonitor(MainWindow m) {
            mainWindow = m;
            HookManager.KeyDown += HookManager_KeyDown;
        }

        /// <summary>
        /// Handles global key presses for both screenshot shortcuts and settings shortcut capture.
        /// </summary>
        private void HookManager_KeyDown(object sender, KeyEventArgs e) {
            if (mainWindow.ScreenCaptureSession != null) {
                mainWindow.ScreenCaptureSession.HandleGlobalKeyDown(e);
                return;
            }

            if (mainWindow.SettingsWindow != null && mainWindow.SettingsWindow.IsActive) {
                string shortcutText = GetPressedKeyText(e);
                if (mainWindow.SettingsWindow.screenshotHotkeyTextBox.IsFocused && mainWindow.FolderWebBrowser.Focused == false) {
                    mainWindow.SettingsWindow.screenshotHotkeyTextBox.Text = shortcutText;
                    mainWindow.ScreenshotHotkey = shortcutText;
                    return;
                }

                if (mainWindow.SettingsWindow.fullscreenHotkeyTextBox.IsFocused && mainWindow.FolderWebBrowser.Focused == false) {
                    mainWindow.SettingsWindow.fullscreenHotkeyTextBox.Text = shortcutText;
                    mainWindow.FullscreenHotkey = shortcutText;
                    return;
                }

                if (mainWindow.SettingsWindow.activeWindowHotkeyTextBox.IsFocused && mainWindow.FolderWebBrowser.Focused == false) {
                    mainWindow.SettingsWindow.activeWindowHotkeyTextBox.Text = shortcutText;
                    mainWindow.ActiveWindowHotkey = shortcutText;
                    return;
                }
            }

            string pressedKey = GetPressedKeyText(e);
            if (pressedKey == mainWindow.ScreenshotHotkey) {
                mainWindow.CaptureScreenshot();
                e.Handled = true;
            }
            else if (pressedKey == mainWindow.FullscreenHotkey) {
                mainWindow.CaptureFullscreenScreenshot();
                e.Handled = true;
            }
            else if (pressedKey == mainWindow.ActiveWindowHotkey) {
                mainWindow.CaptureActiveWindowScreenshot();
                e.Handled = true;
            }
        }

        private string GetPressedKeyText(KeyEventArgs e) {
            string keyText = e.KeyCode.ToString();

            keyText = keyText.Replace("LShiftKey", "LShift").Replace("RShiftKey", "RShift")
                             .Replace("LControlKey", "LCtrl").Replace("RControlKey", "RCtrl")
                             .Replace("LMenu", "LAlt").Replace("RMenu", "RAlt")
                             .Replace("PrintScreen", "PrtScrSysRq");

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) && e.KeyCode != Keys.LShiftKey) {
                keyText = "LShift + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift) && e.KeyCode != Keys.RShiftKey) {
                keyText = "RShift + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && e.KeyCode != Keys.LControlKey) {
                keyText = "LCtrl + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl) && e.KeyCode != Keys.RControlKey) {
                keyText = "RCtrl + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) && e.KeyCode != Keys.LMenu) {
                keyText = "LAlt + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt) && e.KeyCode != Keys.RMenu) {
                keyText = "RAlt + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LWin) && e.KeyCode != Keys.LWin) {
                keyText = "LWin + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RWin) && e.KeyCode != Keys.RWin) {
                keyText = "RWin + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F1) && e.KeyCode != Keys.F1) {
                keyText = "F1 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F2) && e.KeyCode != Keys.F2) {
                keyText = "F2 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F3) && e.KeyCode != Keys.F3) {
                keyText = "F3 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F4) && e.KeyCode != Keys.F4) {
                keyText = "F4 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F5) && e.KeyCode != Keys.F5) {
                keyText = "F5 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F6) && e.KeyCode != Keys.F6) {
                keyText = "F6 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F7) && e.KeyCode != Keys.F7) {
                keyText = "F7 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F8) && e.KeyCode != Keys.F8) {
                keyText = "F8 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F9) && e.KeyCode != Keys.F9) {
                keyText = "F9 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F10) && e.KeyCode != Keys.F10) {
                keyText = "F10 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F11) && e.KeyCode != Keys.F11) {
                keyText = "F11 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.F12) && e.KeyCode != Keys.F12) {
                keyText = "F12 + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.PrintScreen) && e.KeyCode != Keys.PrintScreen) {
                keyText = "PrtScrSysRq + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Insert) && e.KeyCode != Keys.Insert) {
                keyText = "Insert + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Home) && e.KeyCode != Keys.Home) {
                keyText = "Home + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.PageUp) && e.KeyCode != Keys.PageUp) {
                keyText = "PageUp + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.PageDown) && e.KeyCode != Keys.PageDown) {
                keyText = "PageDown + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Delete) && e.KeyCode != Keys.Delete) {
                keyText = "Delete + " + keyText;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.End) && e.KeyCode != Keys.End) {
                keyText = "End + " + keyText;
            }

            return keyText;
        }
    }
}
