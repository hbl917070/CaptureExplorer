using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace CaptureExplorer.Windows {
    /// <summary>
    /// 截圖完成後顯示的簡易閃光特效視窗。
    /// </summary>
    public partial class ScreenshotFlashWindow : Window {
        private double _screenOriginX;
        private double _screenOriginY;
        private double _dpiScaleX;
        private double _dpiScaleY;
        private double _screenWidth;
        private double _screenHeight;

        public ScreenshotFlashWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// 在指定區域顯示局部截圖特效。
        /// </summary>
        public void ShowPartial(int x, int y, int w, int h) {
            PresentationSource source = PresentationSource.FromVisual(this);
            _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;

            _screenWidth = w / _dpiScaleX;
            _screenHeight = h / _dpiScaleY;

            Width = _screenWidth;
            Height = _screenHeight;
            Left = x / _dpiScaleX;
            Top = y / _dpiScaleY;

            PlayClickAnimation();
        }

        /// <summary>
        /// 在整個桌面範圍顯示全螢幕截圖特效。
        /// </summary>
        public void ShowFullscreen() {
            PresentationSource source = PresentationSource.FromVisual(this);
            _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;

            _screenOriginX = 0;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
                if (screen.Bounds.X < _screenOriginX) {
                    _screenOriginX = screen.Bounds.X;
                }
                if (screen.Bounds.Y < _screenOriginY) {
                    _screenOriginY = screen.Bounds.Y;
                }

                int yy = screen.Bounds.Y + screen.Bounds.Height;
                if (yy > _screenHeight) {
                    _screenHeight = yy;
                }

                int xx = screen.Bounds.X + screen.Bounds.Width;
                if (xx > _screenWidth) {
                    _screenWidth = xx;
                }
            }

            _screenWidth -= _screenOriginX;
            _screenHeight -= _screenOriginY;

            Width = _screenWidth / _dpiScaleX;
            Height = _screenHeight / _dpiScaleY;
            Left = _screenOriginX;
            Top = _screenOriginY;

            PlayClickAnimation();
        }

        /// <summary>
        /// 播放簡單的閃光收縮動畫。
        /// </summary>
        public void PlayClickAnimation() {
            flashRectangle.Opacity = 0.3;

            Storyboard opacityStoryboard = new Storyboard();
            DoubleAnimation opacityAnimation = new DoubleAnimation();
            opacityAnimation.Duration = (Duration)TimeSpan.FromSeconds(0.2);
            opacityAnimation.AccelerationRatio = 0.99;
            opacityAnimation.From = 0.15;
            opacityAnimation.To = 0;
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity", new object[0]));
            Storyboard.SetTarget(opacityAnimation, flashRectangle);
            opacityStoryboard.Children.Add(opacityAnimation);
            opacityStoryboard.Begin();

            Storyboard widthStoryboard = new Storyboard();
            DoubleAnimation widthAnimation = new DoubleAnimation();
            widthAnimation.Duration = (Duration)TimeSpan.FromSeconds(0.2);
            widthAnimation.From = _screenWidth;
            widthAnimation.To = 0;
            Storyboard.SetTarget(widthAnimation, flashRectangle);
            widthStoryboard.Children.Add(widthAnimation);
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("Width"));
            widthStoryboard.Begin();

            Storyboard heightStoryboard = new Storyboard();
            DoubleAnimation heightAnimation = new DoubleAnimation();
            heightAnimation.Duration = (Duration)TimeSpan.FromSeconds(0.2);
            heightAnimation.From = _screenHeight;
            heightAnimation.To = 0;
            Storyboard.SetTarget(heightAnimation, flashRectangle);
            heightStoryboard.Children.Add(heightAnimation);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
            heightStoryboard.Begin();

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += (sender, e) => {
                Close();
                timer.Stop();
            };
            timer.Start();
        }
    }
}

