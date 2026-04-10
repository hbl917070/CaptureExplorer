using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace CaptureExplorer {
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            TryEnablePerMonitorDpi();
            base.OnStartup(e);
        }

        private static void TryEnablePerMonitorDpi() {
            try {
                if (Environment.OSVersion.Version.Major >= 10) {
                    SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
                    return;
                }
            } catch { }

            try {
                SetProcessDpiAwareness(ProcessDpiAwareness.PerMonitorDpiAware);
                return;
            } catch { }

            try {
                SetProcessDPIAware();
            } catch { }
        }

        private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new IntPtr(-4);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(ProcessDpiAwareness awareness);

        private enum ProcessDpiAwareness {
            ProcessDpiUnaware = 0,
            SystemDpiAware = 1,
            PerMonitorDpiAware = 2
        }
    }
}
