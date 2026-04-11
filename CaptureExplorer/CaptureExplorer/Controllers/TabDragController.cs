using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CaptureExplorer.Controls;

namespace CaptureExplorer.Controllers {
    /// <summary>
    /// 維護分頁列的選取、拖曳與滑鼠移入樣式。
    /// </summary>
    public class TabDragController {
        private bool _isDragging;
        private readonly StackPanel _tabPanel;
        private readonly SolidColorBrush _defaultTabBrush;
        private readonly SolidColorBrush _selectedTabBrush;
        private readonly SolidColorBrush _hoverTabBrush;

        public TabHeader SelectedTab;
        public Action<string> OnTabClick = _ => { };
        public Action<string> OnTabChanged = _ => { };

        public TabDragController(StackPanel tabPanel) {
            _tabPanel = tabPanel;
            _defaultTabBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            _selectedTabBrush = new SolidColorBrush(Color.FromArgb(255, 15, 100, 155));
            _hoverTabBrush = new SolidColorBrush(Color.FromArgb(100, 100, 100, 100));
        }

        private void ApplyDefaultTabColor(TabHeader sender) {
            sender.Background = _defaultTabBrush;
        }

        private void ApplySelectedTabColor(TabHeader sender) {
            sender.Background = _selectedTabBrush;
        }

        private void ApplyHoverTabColor(TabHeader sender) {
            sender.Background = _hoverTabBrush;
        }

        /// <summary>
        /// 切換目前選取的分頁並更新樣式。
        /// </summary>
        public void SetSelectedItem(TabHeader tab) {
            if (SelectedTab != null) {
                ApplyDefaultTabColor(SelectedTab);
            }

            if (SelectedTab != tab) {
                OnTabClick(tab.Text);
            }

            SelectedTab = tab;
            ApplySelectedTabColor(tab);
        }

        /// <summary>
        /// 清空所有分頁項目。
        /// </summary>
        public void DeleteAllItems() {
            var children = _tabPanel.Children;
            if (children.Count == 0) {
                return;
            }

            children.RemoveRange(0, children.Count);
            SelectedTab = null;
        }

        /// <summary>
        /// 刪除目前分頁，並自動選取鄰近分頁。
        /// </summary>
        public void DeleteSelectedItem() {
            var children = _tabPanel.Children;
            var selectedIndex = 0;

            if (children.Count == 1) {
                _tabPanel.Children.RemoveAt(0);
                return;
            }

            if (children.Count == 0) {
                return;
            }

            for (int i = 0; i < children.Count; i++) {
                if ((TabHeader)children[i] == SelectedTab) {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex == children.Count - 1) {
                SetSelectedItem((TabHeader)children[children.Count - 2]);
            } else if (selectedIndex == 0) {
                SetSelectedItem((TabHeader)children[1]);
            } else {
                SetSelectedItem((TabHeader)children[selectedIndex + 1]);
            }

            _tabPanel.Children.RemoveAt(selectedIndex);
        }

        /// <summary>
        /// 註冊分頁的點擊、拖曳與滑鼠事件。
        /// </summary>
        public void RegisterItemEvents(TabHeader tab) {
            _tabPanel.Children.Add(tab);

            tab.MouseDown += (sender, e) => {
                _isDragging = true;

                if (SelectedTab == null) {
                    SelectedTab = (TabHeader)sender;
                } else {
                    OnTabChanged(SelectedTab.Text);
                }

                SetSelectedItem((TabHeader)sender);
            };

            tab.MouseUp += (sender, e) => {
                _isDragging = false;
            };

            tab.MouseEnter += (sender, e) => {
                if (_isDragging && e.LeftButton == MouseButtonState.Pressed) {
                    var hoveredTab = (TabHeader)sender;
                    var selectedText = SelectedTab.Text;
                    SelectedTab.Text = hoveredTab.Text;
                    ((TabHeader)sender).Text = selectedText;

                    ApplyDefaultTabColor(SelectedTab);
                    ApplySelectedTabColor((TabHeader)sender);
                    SelectedTab = hoveredTab;
                } else if (sender != SelectedTab) {
                    ApplyHoverTabColor((TabHeader)sender);
                }
            };

            tab.MouseLeave += (sender, e) => {
                if (sender == SelectedTab) {
                    ApplySelectedTabColor((TabHeader)sender);
                } else {
                    ApplyDefaultTabColor((TabHeader)sender);
                }
            };
        }
    }
}

