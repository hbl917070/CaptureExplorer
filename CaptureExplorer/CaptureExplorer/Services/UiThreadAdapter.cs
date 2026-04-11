using System;
using System.Threading;

namespace CaptureExplorer.Services {
    /// <summary>
    /// 保存 UI 執行緒內容，讓背景工作能安全切回主執行緒。
    /// </summary>
    public static class UiThreadAdapter {
        public static SynchronizationContext Dispacher { get; private set; }

        /// <summary>
        /// 在程式啟動時記錄目前的 UI 執行緒內容。
        /// </summary>
        public static void Initialize() {
            if (Dispacher == null) {
                Dispacher = SynchronizationContext.Current;
            }
        }

        /// <summary>
        /// 以同步方式切回 UI 執行緒執行。
        /// </summary>
        public static void Invoke(SendOrPostCallback d, object state) {
            Dispacher.Send(d, state);
        }

        /// <summary>
        /// 以非同步方式切回 UI 執行緒執行。
        /// </summary>
        public static void BeginInvoke(SendOrPostCallback d, object state) {
            Dispacher.Post(d, state);
        }

        /// <summary>
        /// 執行不需要參數的 UI 執行緒工作。
        /// </summary>
        public static void RunOnUiThread(Action ac) {
            Invoke(_ => {
                ac();
            }, null);
        }
    }
}
