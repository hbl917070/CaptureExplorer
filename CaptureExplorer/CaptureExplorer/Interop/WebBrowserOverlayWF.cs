using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace CaptureExplorer.Interop {
    /// <summary>
    /// Hosts a WinForms WebBrowser in a separate transparent form so it can visually track a WPF element.
    /// </summary>
    class WebBrowserOverlayWF {
        private readonly Window _owner;
        private readonly FrameworkElement _placementTarget;
        private readonly Form _form;
        private readonly WebBrowser _wb = new WebBrowser();
        private DispatcherOperation _repositionCallback;

        public WebBrowser WebBrowser => _wb;

        public WebBrowserOverlayWF(FrameworkElement placementTarget) {
            _placementTarget = placementTarget;
            Window owner = Window.GetWindow(placementTarget);
            Debug.Assert(owner != null);
            _owner = owner;

            _form = new Form {
                Opacity = owner.Opacity,
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None
            };
            _wb.Dock = DockStyle.Fill;
            _form.Controls.Add(_wb);

            owner.LocationChanged += delegate { OnSizeLocationChanged(); };
            _placementTarget.SizeChanged += delegate { OnSizeLocationChanged(); };

            if (owner.IsVisible) {
                InitialShow();
            } else {
                owner.SourceInitialized += delegate { InitialShow(); };
            }

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(UIElement.OpacityProperty, typeof(Window));
            dpd.AddValueChanged(owner, delegate { _form.Opacity = _owner.Opacity; });
            _form.FormClosing += delegate { _owner.Close(); };
        }

        private void InitialShow() {
            NativeWindow owner = new NativeWindow();
            owner.AssignHandle(((HwndSource)HwndSource.FromVisual(_owner)).Handle);
            _form.Show(owner);
            owner.ReleaseHandle();
        }

        private void OnSizeLocationChanged() {
            if (_repositionCallback == null) {
                _repositionCallback = _owner.Dispatcher.BeginInvoke(
                    new Action(Reposition),
                    DispatcherPriority.Input);
            }
        }

        private void Reposition() {
            _repositionCallback = null;

            Point offset = _placementTarget.TranslatePoint(new Point(), _owner);
            Point size = new Point(_placementTarget.ActualWidth, _placementTarget.ActualHeight);
            HwndSource hwndSource = (HwndSource)HwndSource.FromVisual(_owner);
            CompositionTarget ct = hwndSource.CompositionTarget;
            offset = ct.TransformToDevice.Transform(offset);
            size = ct.TransformToDevice.Transform(size);

            Win32.POINT screenLocation = new Win32.POINT(offset);
            Win32.ClientToScreen(hwndSource.Handle, ref screenLocation);
            Win32.POINT screenSize = new Win32.POINT(size);

            Win32.MoveWindow(_form.Handle, screenLocation.X, screenLocation.Y, screenSize.X, screenSize.Y, true);
        }
    }
}

static class Win32 {
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;

        public POINT(int x, int y) {
            X = x;
            Y = y;
        }

        public POINT(Point pt) {
            X = Convert.ToInt32(pt.X);
            Y = Convert.ToInt32(pt.Y);
        }
    }

    [DllImport("user32.dll")]
    internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
}
