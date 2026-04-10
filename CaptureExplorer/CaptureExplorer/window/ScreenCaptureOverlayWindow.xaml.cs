using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace CaptureExplorer.window {
    /// <summary>
    /// 目前選取框的操作模式。
    /// 包含初次建立、拖曳整塊移動，以及拖拉各邊 / 各角調整大小。
    /// </summary>
    internal enum DragMode {
        None,
        Center,
        Top,
        Bottom,
        Right,
        Left,
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    /// <summary>
    /// 單一螢幕對應的截圖 overlay 視窗。
    /// 這個視窗負責：
    /// 1. 顯示該螢幕的背景截圖
    /// 2. 呈現十字線、放大鏡、遮罩、選取框與操作按鈕
    /// 3. 把使用者的拖曳結果轉換成該螢幕內的像素範圍
    /// 4. 透過 session controller 與其他螢幕的 overlay 協調 active 狀態
    /// </summary>
    public partial class ScreenCaptureOverlayWindow : Window {
        private readonly MainWindow mainWindow;
        private readonly ScreenCaptureSessionController sessionController;
        private readonly ScreenCaptureContext context;
        private readonly System.Windows.Forms.Timer dragTimer = new System.Windows.Forms.Timer();
        private readonly SolidColorBrush hoverBrush = new SolidColorBrush { Color = System.Windows.Media.Color.FromArgb(255, 100, 250, 255) };
        private readonly SolidColorBrush defaultBrush = new SolidColorBrush { Color = System.Windows.Media.Color.FromArgb(50, 100, 250, 255) };
        private ShapeRectangle currentDragHandle;
        private DragMode dragMode = DragMode.None;
        private System.Drawing.Point dragStartMouse;
        private double[] dragStartMetrics = { 0, 0, 0, 0 };
        private bool isInitialSelection = true;
        private DragMode recordedSpaceDragMode = DragMode.Center;
        private bool isSpaceHeld;
        private double scaleX = 1;
        private double scaleY = 1;
        private bool suppressControllerSync;

        public ScreenCaptureOverlayWindow(MainWindow mainWindow, ScreenCaptureSessionController sessionController, ScreenCaptureContext context) {
            this.mainWindow = mainWindow;
            this.sessionController = sessionController;
            this.context = context;

            InitializeComponent();

            dragTimer.Interval = 10;
            dragTimer.Tick += DragTimer_Tick;

            SourceInitialized += OnSourceInitialized;
            Closed += OnClosed;

            InitializeEvents();
        }

        public bool HasActiveSelection =>
            dragMode != DragMode.None ||
            (isInitialSelection == false && selectionMaskGeometry.Rect.Width > 0 && selectionMaskGeometry.Rect.Height > 0);

        /// <summary>
        /// 處理來自全域鍵盤 hook 的快捷鍵。
        /// 這裡只關心和截圖互動有關的命令，例如取消、全選、儲存、複製與空白鍵拖曳。
        /// </summary>
        public void HandleGlobalKeyDown(FormsKeyEventArgs e) {
            var isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (e.KeyCode == Keys.Escape) {
                sessionController.Close();
                e.Handled = true;
                return;
            }

            if ((e.KeyCode == Keys.C || e.KeyCode == Keys.X) && isCtrlPressed) {
                e.Handled = true;
                SaveSelection("copy");
                return;
            }

            if (e.KeyCode == Keys.D && isCtrlPressed) {
                if (dragMode != DragMode.None) {
                    ResetSelection();
                }

                return;
            }

            if (e.KeyCode == Keys.S && isCtrlPressed && dragMode != DragMode.None) {
                SaveSelection("png");
                return;
            }

            if (e.KeyCode == Keys.A && isCtrlPressed) {
                SelectAll();
                return;
            }

            if (e.KeyCode == Keys.Enter && dragMode != DragMode.None) {
                SaveSelection("png");
                return;
            }

            if (dragMode == DragMode.None) {
                return;
            }

            if (isSpaceHeld == false && e.KeyCode == Keys.Space) {
                isSpaceHeld = true;
                dragStartMetrics = new[] { centerDragArea.Margin.Left, centerDragArea.Margin.Top, centerDragArea.ActualWidth, centerDragArea.ActualHeight };
                dragStartMouse = GetMousePositionDip();
                recordedSpaceDragMode = dragMode;
                dragMode = DragMode.Center;
            }
        }

        /// <summary>
        /// 由 controller 呼叫，強制清除這個 overlay 的選取結果。
        /// 使用 suppressControllerSync 是為了避免「controller 叫我清掉，我又反過來通知 controller」的循環。
        /// </summary>
        public void ClearSelectionFromSession() {
            suppressControllerSync = true;

            try {
                ResetSelection();
            } finally {
                suppressControllerSync = false;
            }
        }

        /// <summary>
        /// 視窗來源建立完成後，根據目前螢幕自己的 DPI 與像素 bounds 來設定 overlay 大小，
        /// 並把背景圖貼上去。最後用 Win32 MoveWindow 把 HWND 對到實際像素座標，
        /// 避免混合縮放下 WPF 視窗尺寸和螢幕實際像素對不齊。
        /// </summary>
        private void OnSourceInitialized(object sender, EventArgs e) {
            scaleX = context.DpiScaleX;
            scaleY = context.DpiScaleY;

            Width = context.PixelBounds.Width / scaleX;
            Height = context.PixelBounds.Height / scaleY;
            Left = context.PixelBounds.Left / scaleX;
            Top = context.PixelBounds.Top / scaleY;
            backgroundImage.Source = ToBitmapSource(context.Bitmap);
            actionPanel.Visibility = Visibility.Hidden;
            infoPanel.Visibility = Visibility.Hidden;

            Dispatcher.BeginInvoke(new Action(() => {
                var handle = new WindowInteropHelper(this).Handle;
                MoveWindow(handle, context.PixelBounds.Left, context.PixelBounds.Top, context.PixelBounds.Width, context.PixelBounds.Height, true);
                Focus();
                dragTimer.Start();
                DragTimer_Tick(null, EventArgs.Empty);
            }));
        }

        /// <summary>
        /// overlay 關閉時，通知 controller 更新 session 狀態。
        /// </summary>
        private void OnClosed(object sender, EventArgs e) {
            dragTimer.Stop();
            sessionController.NotifyOverlayClosed(this);
        }

        /// <summary>
        /// 集中註冊所有 UI 互動事件。
        /// 這樣建構式只保留初始化流程，事件邏輯則統一整理在這裡。
        /// </summary>
        private void InitializeEvents() {
            ResetHandleColors();

            RegisterDragHandle(centerDragArea, DragMode.Center);
            RegisterDragHandle(topHandle, DragMode.Top);
            RegisterDragHandle(bottomHandle, DragMode.Bottom);
            RegisterDragHandle(rightHandle, DragMode.Right);
            RegisterDragHandle(leftHandle, DragMode.Left);
            RegisterDragHandle(topLeftHandle, DragMode.TopLeft);
            RegisterDragHandle(topRightHandle, DragMode.TopRight);
            RegisterDragHandle(bottomRightHandle, DragMode.BottomRight);
            RegisterDragHandle(bottomLeftHandle, DragMode.BottomLeft);

            MouseEnter += OnMouseEnter;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseRightButtonUp += OnMouseRightButtonUp;
            KeyUp += OnKeyUp;

            closeButton.Click += (sender, e) => sessionController.Close();
            resetButton.Click += (sender, e) => ResetSelection();
            savePngButton.Click += (sender, e) => SaveSelection("png");
            saveJpgButton.Click += (sender, e) => SaveSelection("jpg");
            copyButton.Click += (sender, e) => SaveSelection("copy");
            editButton.Click += (sender, e) => SaveSelection("edit");
            copyHexButton.Click += (sender, e) => {
                System.Windows.Clipboard.SetText(hexLabel.Content + "");
                sessionController.Close();
            };
            copyRgbButton.Click += (sender, e) => {
                System.Windows.Clipboard.SetText(rgbLabel.Content + "");
                sessionController.Close();
            };
        }

        /// <summary>
        /// 游標進入這個 overlay 時，若目前沒有其他螢幕保留作用中的選取，
        /// 就把目前螢幕切成 active，並顯示 idle 狀態下的十字線與放大鏡。
        /// </summary>
        private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sessionController.ActiveOverlay != null &&
                ReferenceEquals(sessionController.ActiveOverlay, this) == false &&
                sessionController.ActiveOverlay.HasActiveSelection) {
                return;
            }

            if (dragMode == DragMode.None) {
                ActivateThisOverlay();
                dragTimer.Start();
                DragTimer_Tick(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 為各個拖曳控制點註冊互動。
        /// 這裡同時處理「跨螢幕改選」的情境：如果原本是別的螢幕有選取，
        /// 點到這個螢幕的控制點時，會先清掉舊選取，再用這次操作接手。
        /// </summary>
        private void RegisterDragHandle(ShapeRectangle rectangle, DragMode mode) {
            rectangle.MouseLeftButtonDown += (sender, e) => {
                var hadSelectionOnAnotherOverlay =
                    sessionController.ActiveOverlay != null &&
                    ReferenceEquals(sessionController.ActiveOverlay, this) == false &&
                    sessionController.ActiveOverlay.HasActiveSelection;

                ActivateThisOverlay();

                if (hadSelectionOnAnotherOverlay) {
                    ResetSelection();
                }

                currentDragHandle = rectangle;
                actionPanel.Visibility = Visibility.Hidden;
                dragStartMouse = GetMousePositionDip();
                dragStartMetrics = new[] { rectangle.Margin.Left, rectangle.Margin.Top, rectangle.ActualWidth, rectangle.ActualHeight };
                dragMode = mode;
                dragTimer.Start();
                CaptureMouse();
                e.Handled = true;
            };

            rectangle.MouseEnter += (sender, e) => {
                if (dragTimer.Enabled) {
                    return;
                }

                if (rectangle == topHandle || rectangle == bottomHandle || rectangle == rightHandle || rectangle == leftHandle) {
                    rectangle.Fill = hoverBrush;
                } else if (rectangle != centerDragArea) {
                    rectangle.Stroke = hoverBrush;
                }
            };

            rectangle.MouseLeave += (sender, e) => {
                if (dragTimer.Enabled) {
                    return;
                }

                if (rectangle == topHandle || rectangle == bottomHandle || rectangle == rightHandle || rectangle == leftHandle) {
                    rectangle.Fill = defaultBrush;
                } else if (rectangle != centerDragArea) {
                    rectangle.Stroke = defaultBrush;
                }
            };
        }

        /// <summary>
        /// 處理在空白區域首次開始框選。
        /// 如果是從別的螢幕帶著舊選取切過來，會先清掉舊狀態，
        /// 然後直接用這次按下的位置作為新矩形的起點。
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var hadSelectionOnAnotherOverlay =
                sessionController.ActiveOverlay != null &&
                ReferenceEquals(sessionController.ActiveOverlay, this) == false &&
                sessionController.ActiveOverlay.HasActiveSelection;

            ActivateThisOverlay();

            if (hadSelectionOnAnotherOverlay) {
                ResetSelection();
            }

            if (isInitialSelection == false) {
                return;
            }

            var mouse = GetMousePositionDip();
            InitializeSelectionAnchors(mouse);
            dragStartMetrics = new[] { bottomRightHandle.Margin.Left, bottomRightHandle.Margin.Top, bottomRightHandle.ActualWidth, bottomRightHandle.ActualHeight };
            dragStartMouse = mouse;
            dragMode = DragMode.BottomRight;
            currentDragHandle = bottomRightHandle;
            dragTimer.Start();
            CaptureMouse();
            isInitialSelection = false;
        }

        /// <summary>
        /// 滑鼠放開時完成本次拖曳。
        /// </summary>
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            CompleteSelectionInteraction();
        }

        /// <summary>
        /// 右鍵的規則是 session 級別：
        /// 先嘗試取消現有選取；如果整個 session 都沒有選取，才真正離開截圖模式。
        /// </summary>
        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (sessionController.CancelActiveSelection()) {
                ActivateThisOverlay();
                dragTimer.Start();
                DragTimer_Tick(null, EventArgs.Empty);
                return;
            }

            if (dragMode == DragMode.None) {
                sessionController.Close();
            } else {
                ResetSelection();
            }
        }

        /// <summary>
        /// 放開空白鍵時，恢復原本記錄的拖曳模式。
        /// 空白鍵期間使用者可以暫時把整塊選取框當作可移動物件來拖曳。
        /// </summary>
        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key != Key.Space) {
                return;
            }

            isSpaceHeld = false;
            var rectangle = GetRectangleForMode(recordedSpaceDragMode);
            if (rectangle == null) {
                return;
            }

            dragStartMetrics = new[] { rectangle.Margin.Left, rectangle.Margin.Top, rectangle.ActualWidth, rectangle.ActualHeight };
            dragStartMouse = GetMousePositionDip();
            dragMode = recordedSpaceDragMode;
            currentDragHandle = rectangle;
        }

        /// <summary>
        /// 根據 DragMode 找到對應的控制點。
        /// 主要用在空白鍵拖曳結束後，恢復到原本那個被操作的邊或角。
        /// </summary>
        private ShapeRectangle GetRectangleForMode(DragMode mode) {
            switch (mode) {
                case DragMode.TopLeft:
                    return topLeftHandle;
                case DragMode.TopRight:
                    return topRightHandle;
                case DragMode.BottomRight:
                    return bottomRightHandle;
                case DragMode.BottomLeft:
                    return bottomLeftHandle;
                case DragMode.Top:
                    return topHandle;
                case DragMode.Bottom:
                    return bottomHandle;
                case DragMode.Right:
                    return rightHandle;
                case DragMode.Left:
                    return leftHandle;
                case DragMode.Center:
                    return centerDragArea;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 通知 controller：目前這個 overlay 應成為 active。
        /// </summary>
        private void ActivateThisOverlay() {
            if (suppressControllerSync == false) {
                sessionController.ActivateOverlay(this);
            }
        }

        /// <summary>
        /// 顯示選取完成後的操作按鈕。
        /// 如果選到的是 1x1，代表使用者想取色；否則就是一般截圖操作。
        /// </summary>
        private void ShowActionPanel() {
            infoPanel.Visibility = Visibility.Hidden;
            ResetHandleColors();

            var rect = selectionMaskGeometry.Rect;
            var mouse = GetMousePositionDip();

            if (rect.Width == 1 && rect.Height == 1) {
                var point = ToPixelPoint(rect.Left, rect.Top);
                var pixelColor = context.Bitmap.GetPixel(point.X, point.Y);
                hexLabel.Content = $"#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}";
                rgbLabel.Content = $"rgb({pixelColor.R},{pixelColor.G},{pixelColor.B})";
                colorPreviewRect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(pixelColor.R, pixelColor.G, pixelColor.B));

                copyButton.Visibility = Visibility.Collapsed;
                saveJpgButton.Visibility = Visibility.Collapsed;
                savePngButton.Visibility = Visibility.Collapsed;
                editButton.Visibility = Visibility.Collapsed;
                colorToolPanel.Visibility = Visibility.Visible;
            } else {
                copyButton.Visibility = Visibility.Visible;
                saveJpgButton.Visibility = Visibility.Visible;
                savePngButton.Visibility = Visibility.Visible;
                editButton.Visibility = Visibility.Visible;
                colorToolPanel.Visibility = Visibility.Collapsed;
            }

            var margin = 10;
            var x = rect.Width + rect.Left + margin;
            var y = rect.Height + rect.Top + margin;

            if (dragMode == DragMode.Top || dragMode == DragMode.Bottom) {
                x = mouse.X + margin;
            }

            if (dragMode == DragMode.Left || dragMode == DragMode.Right) {
                y = mouse.Y + margin;
            }

            var panelWidth = actionPanel.ActualWidth;
            var panelHeight = actionPanel.ActualHeight;

            if (mouse.X + 15 >= rect.Left && mouse.X <= rect.Left + rect.Width - 15) {
                x = dragMode == DragMode.Top || dragMode == DragMode.Bottom ? mouse.X + margin : rect.Left - panelWidth - margin;
            }

            if (mouse.Y + 15 >= rect.Top && mouse.Y <= rect.Top + rect.Height - 15) {
                y = dragMode == DragMode.Left || dragMode == DragMode.Right ? mouse.Y + margin : rect.Top - panelHeight - margin;
            }

            if (x + panelWidth + margin >= ActualWidth) {
                x = mouse.X - panelWidth - margin;
            }

            if (y + panelHeight + margin >= ActualHeight) {
                y = mouse.Y - panelHeight - margin;
            }

            if (x < 0) {
                x = mouse.X + margin;
            }

            if (y < 0) {
                y = mouse.Y + margin;
            }

            actionPanel.Visibility = Visibility.Visible;
            actionPanel.Margin = new Thickness(x, y, 0, 0);
        }

        /// <summary>
        /// 根據目前選取內容執行複製、儲存或進入編輯。
        /// 在真正輸出前，會先把 WPF 座標轉成螢幕像素範圍再進行裁切。
        /// </summary>
        private void SaveSelection(string type) {
            if (dragMode == DragMode.None) {
                return;
            }

            var rect = selectionMaskGeometry.Rect;
            if ((int)rect.Width == 0 || (int)rect.Height == 0) {
                return;
            }

            using (var image = CropBitmap(context.Bitmap, rect)) {
                if (image == null) {
                    return;
                }

                if (type == "png" || type == "jpg") {
                    var path = mainWindow.func_取得儲存檔名(type);
                    SaveBitmap(image, type, path);

                    try {
                        if (mainWindow.checkBox_自動存入剪貼簿.IsChecked.Value) {
                            System.Windows.Clipboard.SetData(System.Windows.DataFormats.Bitmap, image);
                        }
                    } catch { }
                } else if (type == "edit") {
                    var path = mainWindow.func_取得儲存檔名("");
                    var editWindow = new C_edit_img(new Bitmap(image), path);
                    if (mainWindow.Topmost) {
                        editWindow.TopMost = true;
                    }
                } else {
                    try {
                        System.Windows.Clipboard.SetData(System.Windows.DataFormats.Bitmap, image);
                    } catch { }
                }
            }

            sessionController.Close();
        }

        /// <summary>
        /// 回到未選取狀態。
        /// 這裡會同時清除選取框、放大鏡、按鈕列與拖曳狀態，讓這個 overlay 回到初始可重新框選的模式。
        /// </summary>
        private void ResetSelection() {
            ReleaseMouseCapture();
            selectionMaskGeometry.Rect = new Rect(0, 0, 0, 0);
            topRightHandle.Margin = new Thickness(-100, 0, 0, 0);
            bottomRightHandle.Margin = new Thickness(-100, 0, 0, 0);
            topLeftHandle.Margin = new Thickness(-100, 0, 0, 0);
            bottomLeftHandle.Margin = new Thickness(-100, 0, 0, 0);
            bottomHandle.Margin = new Thickness(0, -100, 0, 0);
            rightHandle.Margin = new Thickness(-100, -100, 0, 0);

            isInitialSelection = true;
            isSpaceHeld = false;
            dragMode = DragMode.None;
            currentDragHandle = null;
            dragTimer.Start();
            actionPanel.Visibility = Visibility.Hidden;
            magnifierBorder.Visibility = Visibility.Collapsed;
            magnifierImage.Source = null;

            if (IsActiveOverlay == false) {
                HideIdleIndicators();
            }
        }

        /// <summary>
        /// 全選目前這個螢幕，直接把選取框撐滿整個 overlay。
        /// </summary>
        private void SelectAll() {
            ActivateThisOverlay();
            dragTimer.Enabled = false;

            topRightHandle.Margin = new Thickness(ActualWidth - 10, -10, 0, 0);
            bottomRightHandle.Margin = new Thickness(ActualWidth - 10, ActualHeight - 10, -10, 0);
            topLeftHandle.Margin = new Thickness(-10, -10, 0, 0);
            bottomLeftHandle.Margin = new Thickness(-10, ActualHeight - 10, 0, 0);

            bottomHandle.Margin = new Thickness(0, ActualHeight - 11, 0, 0);
            rightHandle.Margin = new Thickness(ActualWidth - 11, 0, 0, 0);
            topHandle.Margin = new Thickness(0, -10, 0, 0);
            leftHandle.Margin = new Thickness(-10, 0, 0, 0);

            dragMode = DragMode.Center;
            isInitialSelection = false;

            var left = leftHandle.Margin.Left + 10;
            var top = topHandle.Margin.Top + 10;
            var width = rightHandle.Margin.Left - leftHandle.Margin.Left;
            var height = bottomHandle.Margin.Top - topHandle.Margin.Top;
            centerDragArea.Width = Math.Abs(width) - 10;
            centerDragArea.Height = Math.Abs(height) - 10;
            centerDragArea.Margin = new Thickness(left + 5, top + 5, 0, 0);
            selectionMaskGeometry.Rect = new Rect(left, top, Math.Abs(width) + 1, Math.Abs(height) + 1);

            ShowActionPanel();
        }

        /// <summary>
        /// 這是整個選取互動的核心更新迴圈。
        /// 它負責三件事：
        /// 1. 在 idle 狀態下更新十字線與放大鏡
        /// 2. 在拖曳中根據滑鼠位置更新各邊 / 各角 / 中心區塊
        /// 3. 持續把控制點位置整理成一個合法的選取矩形
        /// 
        /// 因為滑鼠可能在別的螢幕放開，所以除了 WPF 事件外，這裡也會檢查全域滑鼠按鍵狀態，
        /// 避免 MouseLeftButtonUp 沒有回到原視窗時，拖曳被卡住。
        /// </summary>
        private void DragTimer_Tick(object sender, EventArgs e) {
            if (dragMode == DragMode.None && IsActiveOverlay == false) {
                HideIdleIndicators();
                magnifierBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (dragMode != DragMode.None && dragTimer.Enabled && System.Windows.Forms.Control.MouseButtons.HasFlag(MouseButtons.Left) == false) {
                CompleteSelectionInteraction();
                return;
            }

            if (dragMode != DragMode.Center) {
                var point = GetCurrentDragHandlePosition();
                using (var image = CropBitmapPreserveSize(context.Bitmap, new Rect(point.X - 15, point.Y - 15, 31, 31))) {
                    if (image != null) {
                        using (var scaledImage = new Bitmap(image, Math.Max(1, (int)(image.Width / scaleX)), Math.Max(1, (int)(image.Height / scaleY)))) {
                            magnifierImage.Source = ToBitmapSource(scaledImage);
                        }
                    }
                }

                magnifierBorder.Visibility = Visibility.Visible;
            } else {
                magnifierBorder.Visibility = Visibility.Collapsed;
            }

            if (dragMode == DragMode.None) {
                leftHandle.Margin = new Thickness(GetMousePositionDip().X - 10, 0, 0, 0);
                topHandle.Margin = new Thickness(0, GetMousePositionDip().Y - 10, 0, 0);
                cursorProxy.Margin = new Thickness(GetMousePositionDip().X - 30, GetMousePositionDip().Y - 30, 0, 0);
                UpdateInfoPanel();
                return;
            }

            if (dragMode == DragMode.Center) {
                var leftOffset = leftHandle.Margin.Left - centerDragArea.Margin.Left;
                var rightOffset = rightHandle.Margin.Left - centerDragArea.Margin.Left;
                var bottomOffset = bottomHandle.Margin.Top - centerDragArea.Margin.Top;
                var topOffset = topHandle.Margin.Top - centerDragArea.Margin.Top;

                centerDragArea.Margin = new Thickness(
                    GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0],
                    GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1],
                    0,
                    0);

                leftHandle.Margin = new Thickness(centerDragArea.Margin.Left + leftOffset, 0, 0, 0);
                rightHandle.Margin = new Thickness(centerDragArea.Margin.Left + rightOffset, 0, 0, 0);
                bottomHandle.Margin = new Thickness(0, centerDragArea.Margin.Top + bottomOffset, 0, 0);
                topHandle.Margin = new Thickness(0, centerDragArea.Margin.Top + topOffset, 0, 0);
            }

            if (dragMode == DragMode.Top) {
                topHandle.Margin = new Thickness(0, GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1], 0, 0);
            } else if (dragMode == DragMode.Bottom) {
                bottomHandle.Margin = new Thickness(0, GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1], 0, 0);
            }

            if (dragMode == DragMode.Right) {
                rightHandle.Margin = new Thickness(GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0], 0, 0, 0);
            } else if (dragMode == DragMode.Left) {
                leftHandle.Margin = new Thickness(GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0], 0, 0, 0);
            }

            if (dragMode == DragMode.TopLeft) {
                topLeftHandle.Margin = new Thickness(GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0], GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1], 0, 0);
                topHandle.Margin = new Thickness(0, topLeftHandle.Margin.Top, 0, 0);
                leftHandle.Margin = new Thickness(topLeftHandle.Margin.Left, 0, 0, 0);
            } else if (dragMode == DragMode.TopRight) {
                topRightHandle.Margin = new Thickness(GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0], GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1], 0, 0);
                topHandle.Margin = new Thickness(0, topRightHandle.Margin.Top, 0, 0);
                rightHandle.Margin = new Thickness(topRightHandle.Margin.Left, 0, 0, 0);
            } else if (dragMode == DragMode.BottomRight) {
                bottomRightHandle.Margin = new Thickness(GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0], GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1], 0, 0);
                bottomHandle.Margin = new Thickness(0, bottomRightHandle.Margin.Top, 0, 0);
                rightHandle.Margin = new Thickness(bottomRightHandle.Margin.Left, 0, 0, 0);
            } else if (dragMode == DragMode.BottomLeft) {
                bottomLeftHandle.Margin = new Thickness(GetMousePositionDip().X - dragStartMouse.X + dragStartMetrics[0], GetMousePositionDip().Y - dragStartMouse.Y + dragStartMetrics[1], 0, 0);
                bottomHandle.Margin = new Thickness(0, bottomLeftHandle.Margin.Top, 0, 0);
                leftHandle.Margin = new Thickness(bottomLeftHandle.Margin.Left, 0, 0, 0);
            }

            topLeftHandle.Margin = new Thickness(leftHandle.Margin.Left, topHandle.Margin.Top, 0, 0);
            bottomRightHandle.Margin = new Thickness(rightHandle.Margin.Left, bottomHandle.Margin.Top, 0, 0);
            bottomLeftHandle.Margin = new Thickness(leftHandle.Margin.Left, bottomHandle.Margin.Top, 0, 0);
            topRightHandle.Margin = new Thickness(rightHandle.Margin.Left, topHandle.Margin.Top, 0, 0);

            var selectionLeft = leftHandle.Margin.Left + 10;
            var selectionTop = topHandle.Margin.Top + 10;
            var selectionWidth = rightHandle.Margin.Left - leftHandle.Margin.Left;
            var selectionHeight = bottomHandle.Margin.Top - topHandle.Margin.Top;

            centerDragArea.Width = Math.Max(0, Math.Abs(selectionWidth) - 10);
            centerDragArea.Height = Math.Max(0, Math.Abs(selectionHeight) - 10);

            if (topHandle.Margin.Top > bottomHandle.Margin.Top) {
                selectionTop += selectionHeight;
            }

            if (leftHandle.Margin.Left > rightHandle.Margin.Left) {
                selectionLeft += selectionWidth;
            }

            centerDragArea.Margin = new Thickness(selectionLeft + 5, selectionTop + 5, 0, 0);
            selectionMaskGeometry.Rect = new Rect(selectionLeft, selectionTop, Math.Abs(selectionWidth) + 1, Math.Abs(selectionHeight) + 1);

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                if (selectionMaskGeometry.Rect.Width > selectionMaskGeometry.Rect.Height) {
                    selectionMaskGeometry.Rect = new Rect(selectionLeft, selectionTop, Math.Abs(selectionHeight) + 1, Math.Abs(selectionHeight) + 1);
                } else {
                    selectionMaskGeometry.Rect = new Rect(selectionLeft, selectionTop, Math.Abs(selectionWidth) + 1, Math.Abs(selectionWidth) + 1);
                }
            }

            UpdateInfoPanel();
        }

        /// <summary>
        /// 更新尺寸資訊與放大鏡面板的位置。
        /// 位置策略會盡量貼近滑鼠或選取框，但同時避開螢幕邊界。
        /// </summary>
        private void UpdateInfoPanel() {
            var rect = selectionMaskGeometry.Rect;
            var mouse = GetMousePositionDip();
            sizeLabel.Content = dragMode == DragMode.None
                ? $"l:{mouse.X}  t:{mouse.Y}"
                : $"l:{(int)rect.Left}  t:{(int)rect.Top}  w:{(int)rect.Width}  h:{(int)rect.Height}";

            var margin = 5;
            var x = rect.Width + rect.Left + margin;
            var y = rect.Height + rect.Top + margin;

            if (dragMode == DragMode.Top || dragMode == DragMode.Bottom || dragMode == DragMode.None) {
                x = mouse.X + margin;
            }

            if (dragMode == DragMode.Left || dragMode == DragMode.Right || dragMode == DragMode.None) {
                y = mouse.Y + margin;
            }

            var panelWidth = infoPanel.ActualWidth;
            var panelHeight = infoPanel.ActualHeight;

            if (mouse.X + 15 >= rect.Left && mouse.X <= rect.Left + rect.Width - 15) {
                x = dragMode == DragMode.Top || dragMode == DragMode.Bottom ? mouse.X + margin : rect.Left - panelWidth - margin;
            }

            if (mouse.Y + 15 >= rect.Top && mouse.Y <= rect.Top + rect.Height - 15) {
                y = dragMode == DragMode.Left || dragMode == DragMode.Right ? mouse.Y + margin : rect.Top - panelHeight - margin;
            }

            if (mouse.X + panelWidth + margin >= ActualWidth) {
                x = mouse.X - panelWidth - margin;
            }

            if (mouse.Y + panelHeight + margin >= ActualHeight) {
                y = mouse.Y - panelHeight - margin;
            }

            if (x < 0) {
                x = mouse.X + margin;
            }

            if (y < 0) {
                y = mouse.Y + margin;
            }

            infoPanel.Margin = new Thickness(x, y, 0, 0);
            infoPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 取得目前滑鼠在「這個 overlay 內部」的 DIP 座標。
        /// 先扣掉螢幕左上角像素偏移，再除以該螢幕 DPI 比例，最後夾回目前螢幕邊界內。
        /// </summary>
        private System.Drawing.Point GetMousePositionDip() {
            var point = System.Windows.Forms.Cursor.Position;
            point.X -= context.PixelBounds.Left;
            point.Y -= context.PixelBounds.Top;
            point.X = Clamp((int)(point.X / scaleX), 0, Math.Max(0, (int)Math.Ceiling(ActualWidth) - 1));
            point.Y = Clamp((int)(point.Y / scaleY), 0, Math.Max(0, (int)Math.Ceiling(ActualHeight) - 1));
            return point;
        }

        private bool IsActiveOverlay => ReferenceEquals(sessionController.ActiveOverlay, this);

        /// <summary>
        /// 結束一次拖曳互動。
        /// 如果最後沒有形成有效矩形，就回到未選取狀態；否則顯示操作按鈕。
        /// </summary>
        private void CompleteSelectionInteraction() {
            ReleaseMouseCapture();

            if (selectionMaskGeometry.Rect.Width == 0 || selectionMaskGeometry.Rect.Height == 0) {
                ResetSelection();
                return;
            }

            dragTimer.Enabled = false;
            cursorProxy.Margin = new Thickness(-100, -100, 0, 0);
            ShowActionPanel();
        }

        /// <summary>
        /// 隱藏 idle 狀態下的十字線與資訊面板。
        /// 當這個 overlay 不是 active 時使用。
        /// </summary>
        private void HideIdleIndicators() {
            leftHandle.Margin = new Thickness(-100, 0, 0, 0);
            topHandle.Margin = new Thickness(0, -100, 0, 0);
            cursorProxy.Margin = new Thickness(-100, -100, 0, 0);
            infoPanel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 用滑鼠當前位置建立一個 1x1 的初始選取框。
        /// 這是第一次框選時的起點。
        /// </summary>
        private void InitializeSelectionAnchors(System.Drawing.Point mouse) {
            var x = mouse.X - 10;
            var y = mouse.Y - 10;

            leftHandle.Margin = new Thickness(x, 0, 0, 0);
            rightHandle.Margin = new Thickness(x, 0, 0, 0);
            topHandle.Margin = new Thickness(0, y, 0, 0);
            bottomHandle.Margin = new Thickness(0, y, 0, 0);

            topLeftHandle.Margin = new Thickness(x, y, 0, 0);
            topRightHandle.Margin = new Thickness(x, y, 0, 0);
            bottomLeftHandle.Margin = new Thickness(x, y, 0, 0);
            bottomRightHandle.Margin = new Thickness(x, y, 0, 0);

            centerDragArea.Width = 0;
            centerDragArea.Height = 0;
            centerDragArea.Margin = new Thickness(mouse.X, mouse.Y, 0, 0);
            selectionMaskGeometry.Rect = new Rect(mouse.X, mouse.Y, 1, 1);
        }

        /// <summary>
        /// 取得目前被拖曳控制點對應的座標，供放大鏡取樣使用。
        /// 依照拖曳的是邊、角還是整塊，決定要用哪一個參考點。
        /// </summary>
        private System.Drawing.Point GetCurrentDragHandlePosition() {
            var point = GetMousePositionDip();
            if (currentDragHandle == null) {
                return point;
            }

            if (currentDragHandle == topHandle || currentDragHandle == bottomHandle) {
                point.Y = (int)(currentDragHandle.Margin.Top + (currentDragHandle.ActualHeight / 2));
            } else if (currentDragHandle == rightHandle || currentDragHandle == leftHandle) {
                point.X = (int)(currentDragHandle.Margin.Left + (currentDragHandle.ActualWidth / 2));
            } else if (currentDragHandle != centerDragArea) {
                point.X = (int)(currentDragHandle.Margin.Left + (currentDragHandle.ActualWidth / 2));
                point.Y = (int)(currentDragHandle.Margin.Top + (currentDragHandle.ActualHeight / 2));
            }

            return point;
        }

        /// <summary>
        /// 把所有控制點恢復成預設顏色。
        /// </summary>
        private void ResetHandleColors() {
            topHandle.Fill = defaultBrush;
            bottomHandle.Fill = defaultBrush;
            rightHandle.Fill = defaultBrush;
            leftHandle.Fill = defaultBrush;
            topRightHandle.Stroke = defaultBrush;
            bottomRightHandle.Stroke = defaultBrush;
            topLeftHandle.Stroke = defaultBrush;
            bottomLeftHandle.Stroke = defaultBrush;
        }

        /// <summary>
        /// 把 WPF DIP 座標轉成實際像素點。
        /// 主要用於取色與放大鏡。
        /// </summary>
        private System.Drawing.Point ToPixelPoint(double x, double y) {
            var pixelX = Clamp((int)Math.Round(x * scaleX), 0, context.Bitmap.Width - 1);
            var pixelY = Clamp((int)Math.Round(y * scaleY), 0, context.Bitmap.Height - 1);
            return new System.Drawing.Point(pixelX, pixelY);
        }

        /// <summary>
        /// 把 WPF 中的選取矩形轉成背景 bitmap 上的實際像素範圍。
        /// 這是最後裁切圖片時最重要的座標換算步驟。
        /// </summary>
        private System.Drawing.Rectangle ToPixelRectangle(Rect rect) {
            var left = Clamp((int)Math.Round(rect.Left * scaleX), 0, context.Bitmap.Width - 1);
            var top = Clamp((int)Math.Round(rect.Top * scaleY), 0, context.Bitmap.Height - 1);
            var right = Clamp((int)Math.Round((rect.Left + rect.Width) * scaleX), left + 1, context.Bitmap.Width);
            var bottom = Clamp((int)Math.Round((rect.Top + rect.Height) * scaleY), top + 1, context.Bitmap.Height);
            return new System.Drawing.Rectangle(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// 依照目前選取矩形，從背景圖中裁出對應範圍。
        /// </summary>
        private Bitmap CropBitmap(Bitmap source, Rect rect) {
            if (source == null) {
                return null;
            }

            var pixelRect = ToPixelRectangle(rect);
            if (pixelRect.Width <= 0 || pixelRect.Height <= 0) {
                return null;
            }

            var output = new Bitmap(pixelRect.Width, pixelRect.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var graphics = Graphics.FromImage(output)) {
                graphics.DrawImage(source, new System.Drawing.Rectangle(0, 0, pixelRect.Width, pixelRect.Height), pixelRect, GraphicsUnit.Pixel);
            }

            return output;
        }

        /// <summary>
        /// 供放大鏡使用的裁切版本。
        /// 與一般裁切不同的是，它會固定輸出尺寸；若超出螢幕邊界，超出的部分以黑底補齊，
        /// 這樣放大鏡在螢幕邊緣時仍可維持正方形，中央準心也不會跑掉。
        /// </summary>
        private Bitmap CropBitmapPreserveSize(Bitmap source, Rect rect) {
            if (source == null) {
                return null;
            }

            var targetWidth = Math.Max(1, (int)Math.Round(rect.Width * scaleX));
            var targetHeight = Math.Max(1, (int)Math.Round(rect.Height * scaleY));
            var output = new Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var graphics = Graphics.FromImage(output)) {
                graphics.Clear(System.Drawing.Color.Black);

                var requestedLeft = (int)Math.Round(rect.Left * scaleX);
                var requestedTop = (int)Math.Round(rect.Top * scaleY);
                var requestedRight = requestedLeft + targetWidth;
                var requestedBottom = requestedTop + targetHeight;

                var sourceLeft = Clamp(requestedLeft, 0, source.Width);
                var sourceTop = Clamp(requestedTop, 0, source.Height);
                var sourceRight = Clamp(requestedRight, 0, source.Width);
                var sourceBottom = Clamp(requestedBottom, 0, source.Height);

                var sourceWidth = sourceRight - sourceLeft;
                var sourceHeight = sourceBottom - sourceTop;

                if (sourceWidth <= 0 || sourceHeight <= 0) {
                    return output;
                }

                var destinationLeft = sourceLeft - requestedLeft;
                var destinationTop = sourceTop - requestedTop;

                graphics.DrawImage(
                    source,
                    new System.Drawing.Rectangle(destinationLeft, destinationTop, sourceWidth, sourceHeight),
                    new System.Drawing.Rectangle(sourceLeft, sourceTop, sourceWidth, sourceHeight),
                    GraphicsUnit.Pixel);
            }

            return output;
        }

        /// <summary>
        /// 依指定格式把裁切結果存檔。
        /// PNG 直接存，JPG 則額外帶入品質參數。
        /// </summary>
        private void SaveBitmap(Bitmap bitmap, string type, string path) {
            if (type == "png") {
                bitmap.Save(path);
                return;
            }

            var jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
            var qualityEncoder = System.Drawing.Imaging.Encoder.Quality;
            var encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
            encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(qualityEncoder, 98L);
            bitmap.Save(path, jpgEncoder, encoderParameters);
        }

        /// <summary>
        /// 尋找指定圖片格式對應的 encoder。
        /// </summary>
        private static System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format) {
            foreach (var codec in System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders()) {
                if (codec.FormatID == format.Guid) {
                    return codec;
                }
            }

            return null;
        }

        /// <summary>
        /// 把 GDI+ Bitmap 轉成 WPF 可顯示的 BitmapSource。
        /// 這裡主要用於背景圖與放大鏡預覽。
        /// </summary>
        private static BitmapSource ToBitmapSource(Bitmap bitmap) {
            var handle = bitmap.GetHbitmap();

            try {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            } finally {
                DeleteObject(handle);
            }
        }

        /// <summary>
        /// 將數值夾在指定範圍內，避免滑鼠或裁切結果超出螢幕 / 圖片邊界。
        /// </summary>
        private static int Clamp(int value, int min, int max) {
            if (value < min) {
                return min;
            }

            if (value > max) {
                return max;
            }

            return value;
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    }
}
