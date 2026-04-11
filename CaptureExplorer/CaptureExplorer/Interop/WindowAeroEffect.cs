using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CaptureExplorer.Interop {
    /// <summary>
    /// Applies the older Windows blur and glass effects used by the app shell.
    /// </summary>
    class WindowAeroEffect {
        public WindowAeroEffect() { }

        /// <summary>
        /// Enables the blur effect when the current Windows version supports it.
        /// </summary>
        public bool TryEnableBlur(Window M) {
            if (IsWindows10()) {
                try {
                    new WindowAeroEffect().EnableBlur(M);
                } catch {
                    return false;
                }

                return true;
            }

            if (IsWindows7()) {
                try {
                    int h = 0;
                    foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
                        int xx = screen.Bounds.Y + screen.Bounds.Height;
                        if (xx > h) {
                            h = xx;
                        }
                    }

                    h += 50;

                    IntPtr mainWindowPtr = new WindowInteropHelper(M).Handle;
                    HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                    mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);

                    using (var desktop = System.Drawing.Graphics.FromHwnd(mainWindowPtr)) {
                        float desktopDpiX = desktop.DpiX;

                        MARGINS margins = new MARGINS {
                            cxLeftWidth = Convert.ToInt32(0 * (desktopDpiX / 96)),
                            cxRightWidth = Convert.ToInt32(0 * (desktopDpiX / 96)),
                            cyTopHeight = Convert.ToInt32(h * (desktopDpiX / 96)),
                            cyBottomHeight = Convert.ToInt32(0 * (desktopDpiX / 96))
                        };

                        int hr = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
                        if (hr < 0) {
                            return false;
                        }

                        M.BorderThickness = new Thickness(10, 0, 10, h);
                    }
                } catch {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether the current OS reports itself as Windows 10.
        /// </summary>
        public bool IsWindows10() {
            try {
                var reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                string productName = (string)reg.GetValue("ProductName");
                return productName.StartsWith("Windows 10");
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Returns whether the current OS reports itself as Windows 7.
        /// </summary>
        public bool IsWindows7() {
            try {
                string version = Environment.OSVersion.Version.ToString();
                return version.Length > 3 && version.Substring(0, 3) == "6.1";
            } catch {
                return false;
            }
        }

        #region win10毛玻璃

        internal enum AccentState {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute {
            WCA_ACCENT_POLICY = 19
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        /// <summary>
        /// Applies the Windows 10 blur-behind accent to the target window.
        /// </summary>
        public void EnableBlur(Window w) {
            var windowHelper = new WindowInteropHelper(w);
            var accent = new AccentPolicy {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        #endregion

        #region win7毛玻璃

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("DwmApi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd,
            ref MARGINS pMarInset);

        #endregion
    }
}
