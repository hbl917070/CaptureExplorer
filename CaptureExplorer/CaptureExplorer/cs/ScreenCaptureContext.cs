using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CaptureExplorer {
    /// <summary>
    /// 單一螢幕在截圖 session 期間需要共用的資料。
    /// 這裡只負責保存「這個螢幕的截圖結果」與「這個螢幕自己的 DPI / bounds」，
    /// 讓 overlay 視窗可以只關注互動邏輯，不必再自己查詢系統資訊。
    /// </summary>
    public sealed class ScreenCaptureContext {
        /// <summary>
        /// 建立某個螢幕對應的截圖上下文，並在建立時就把該螢幕的 DPI 縮放比例記錄下來。
        /// 這樣後續在 WPF DIP 與實際像素間轉換時，會有穩定的基準可用。
        /// </summary>
        public ScreenCaptureContext(Screen screen, Bitmap bitmap) {
            Screen = screen;
            Bitmap = bitmap;

            var dpiScale = GetMonitorDpiScale(screen);
            DpiScaleX = dpiScale.X;
            DpiScaleY = dpiScale.Y;
        }

        public Screen Screen { get; }

        public Bitmap Bitmap { get; }

        public string DeviceName => Screen.DeviceName;

        public Rectangle PixelBounds => Screen.Bounds;

        public double DpiScaleX { get; }

        public double DpiScaleY { get; }

        /// <summary>
        /// 直接向 Windows 查詢指定螢幕的實際 DPI。
        /// 混合縮放時不能只依賴 WPF 視窗本身回報的 DPI，
        /// 否則背景圖、滑鼠位置和裁切座標都可能落在不同基準上。
        /// </summary>
        private static DpiScale GetMonitorDpiScale(Screen screen) {
            try {
                var point = new NativePoint {
                    X = screen.Bounds.Left + (screen.Bounds.Width / 2),
                    Y = screen.Bounds.Top + (screen.Bounds.Height / 2)
                };

                var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
                if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY) == 0) {
                    return new DpiScale(dpiX / 96d, dpiY / 96d);
                }
            } catch { }

            return new DpiScale(1, 1);
        }

        /// <summary>
        /// 小型結構，用來成對保存 X / Y 方向的 DPI 比例。
        /// </summary>
        private readonly struct DpiScale {
            public DpiScale(double x, double y) {
                X = x;
                Y = y;
            }

            public double X { get; }

            public double Y { get; }
        }

        private const uint MonitorDefaultToNearest = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint {
            public int X;
            public int Y;
        }

        private enum MonitorDpiType {
            EffectiveDpi = 0,
            AngularDpi = 1,
            RawDpi = 2,
            Default = EffectiveDpi
        }
    }
}
