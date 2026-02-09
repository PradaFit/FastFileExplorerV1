using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class TabManager : Panel
    {
        private readonly List<FileTab> _tabs = new List<FileTab>();
        private int _selectedIndex = -1;
        private int _hoveredIndex = -1;
        private int _dragTabIndex = -1;
        private Point _dragStartPoint;
        private bool _isDragging;
        private readonly Button _newTabButton;
        private const int TabHeight = 32;
        private const int TabMinWidth = 120;
        private const int TabMaxWidth = 200;
        private const int CloseButtonSize = 16;

        public event EventHandler<TabEventArgs> TabSelected;
        public event EventHandler<TabEventArgs> TabClosed;
        public event EventHandler<TabEventArgs> TabPathChanged;
        public event EventHandler NewTabRequested;

        public int SelectedIndex => _selectedIndex;
        public int TabCount => _tabs.Count;
        public FileTab SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabs.Count ? _tabs[_selectedIndex] : null;

        public TabManager()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            Height = TabHeight + 4;
            BackColor = Theme.Background;
            Dock = DockStyle.Top;

            _newTabButton = new Button
            {
                Text = "+",
                Size = new Size(28, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _newTabButton.FlatAppearance.BorderSize = 0;
            _newTabButton.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            _newTabButton.Click += (s, e) => NewTabRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(_newTabButton);

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseLeave += OnMouseLeave;
            MouseDoubleClick += OnMouseDoubleClick;
        }

        public FileTab AddTab(string path, string title = null, bool select = true)
        {
            var tab = new FileTab
            {
                Path = path,
                Title = title ?? System.IO.Path.GetFileName(path) ?? path,
                Id = Guid.NewGuid()
            };

            _tabs.Add(tab);

            if (select || _tabs.Count == 1)
            {
                SelectTab(_tabs.Count - 1);
            }

            Invalidate();
            return tab;
        }

        public void UpdateTabPath(int index, string path, string title = null)
        {
            if (index < 0 || index >= _tabs.Count) return;

            _tabs[index].Path = path;
            _tabs[index].Title = title ?? System.IO.Path.GetFileName(path) ?? path;

            if (string.IsNullOrEmpty(_tabs[index].Title))
                _tabs[index].Title = path;

            TabPathChanged?.Invoke(this, new TabEventArgs(_tabs[index], index));
            Invalidate();
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (_selectedIndex == index) return;

            _selectedIndex = index;
            TabSelected?.Invoke(this, new TabEventArgs(_tabs[index], index));
            Invalidate();
        }

        public void CloseTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (_tabs.Count <= 1) return; // Keep at least one tab

            var closedTab = _tabs[index];
            _tabs.RemoveAt(index);

            if (_selectedIndex >= _tabs.Count)
                _selectedIndex = _tabs.Count - 1;
            else if (_selectedIndex > index)
                _selectedIndex--;

            TabClosed?.Invoke(this, new TabEventArgs(closedTab, index));

            if (_selectedIndex >= 0)
                TabSelected?.Invoke(this, new TabEventArgs(_tabs[_selectedIndex], _selectedIndex));

            Invalidate();
        }

        public void CloseAllTabsExcept(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;

            var keepTab = _tabs[index];
            _tabs.Clear();
            _tabs.Add(keepTab);
            _selectedIndex = 0;

            TabSelected?.Invoke(this, new TabEventArgs(keepTab, 0));
            Invalidate();
        }

        private Rectangle GetTabRect(int index)
        {
            var tabWidth = Math.Min(TabMaxWidth, Math.Max(TabMinWidth, (Width - 40) / Math.Max(1, _tabs.Count)));
            return new Rectangle(index * tabWidth, 2, tabWidth - 2, TabHeight);
        }

        private int GetTabIndexAt(Point location)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (GetTabRect(i).Contains(location))
                    return i;
            }
            return -1;
        }

        private Rectangle GetCloseButtonRect(Rectangle tabRect)
        {
            return new Rectangle(
                tabRect.Right - CloseButtonSize - 6,
                tabRect.Y + (tabRect.Height - CloseButtonSize) / 2,
                CloseButtonSize,
                CloseButtonSize);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw tabs
            for (int i = 0; i < _tabs.Count; i++)
            {
                DrawTab(g, i);
            }

            // Position new tab button
            var lastTabRect = _tabs.Count > 0 ? GetTabRect(_tabs.Count - 1) : Rectangle.Empty;
            _newTabButton.Location = new Point(lastTabRect.Right + 4, 3);

            // Draw bottom border
            using (var pen = new Pen(Theme.Border))
            {
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }
        }

        private void DrawTab(Graphics g, int index)
        {
            var rect = GetTabRect(index);
            var tab = _tabs[index];
            var isSelected = index == _selectedIndex;
            var isHovered = index == _hoveredIndex;

            // Background
            Color bgColor;
            if (isSelected)
                bgColor = Theme.BackgroundLighter;
            else if (isHovered)
                bgColor = Theme.SurfaceHover;
            else
                bgColor = Theme.BackgroundLight;

            using (var brush = new SolidBrush(bgColor))
            {
                var path = CreateRoundedTop(rect, 6);
                g.FillPath(brush, path);
            }

            // Selected indicator
            if (isSelected)
            {
                using (var pen = new Pen(Theme.Accent, 2))
                {
                    g.DrawLine(pen, rect.X + 2, rect.Bottom, rect.Right - 2, rect.Bottom);
                }
            }

            // Icon
            var iconRect = new Rectangle(rect.X + 8, rect.Y + 8, 16, 16);
            using (var brush = new SolidBrush(Theme.TextSecondary))
            {
                g.DrawString(Icons.Folder, Theme.FontRegular, brush, iconRect.Location);
            }

            // Title
            var titleRect = new Rectangle(rect.X + 28, rect.Y + 6, rect.Width - 50, rect.Height - 12);
            using (var brush = new SolidBrush(isSelected ? Theme.TextPrimary : Theme.TextSecondary))
            using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(tab.Title, Theme.FontRegular, brush, titleRect, format);
            }

            // Close button (only show on hover or selected)
            if (isSelected || isHovered)
            {
                var closeRect = GetCloseButtonRect(rect);
                var closeHovered = closeRect.Contains(PointToClient(MousePosition));

                if (closeHovered)
                {
                    using (var brush = new SolidBrush(Theme.Error))
                    {
                        g.FillEllipse(brush, closeRect);
                    }
                }

                using (var pen = new Pen(closeHovered ? Theme.TextPrimary : Theme.TextSecondary, 1.5f))
                {
                    var cx = closeRect.X + closeRect.Width / 2;
                    var cy = closeRect.Y + closeRect.Height / 2;
                    var s = 3;
                    g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                    g.DrawLine(pen, cx + s, cy - s, cx - s, cy + s);
                }
            }
        }

        private GraphicsPath CreateRoundedTop(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
            path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + radius);
            path.CloseFigure();
            return path;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            var index = GetTabIndexAt(e.Location);
            if (index < 0) return;

            var rect = GetTabRect(index);
            var closeRect = GetCloseButtonRect(rect);

            if (e.Button == MouseButtons.Left)
            {
                if (closeRect.Contains(e.Location))
                {
                    CloseTab(index);
                }
                else
                {
                    SelectTab(index);
                    _dragTabIndex = index;
                    _dragStartPoint = e.Location;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                CloseTab(index);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var index = GetTabIndexAt(e.Location);

            if (_dragTabIndex >= 0 && e.Button == MouseButtons.Left)
            {
                if (!_isDragging && Math.Abs(e.X - _dragStartPoint.X) > 5)
                {
                    _isDragging = true;
                }

                if (_isDragging && index >= 0 && index != _dragTabIndex)
                {
                    // Reorder tabs
                    var tab = _tabs[_dragTabIndex];
                    _tabs.RemoveAt(_dragTabIndex);
                    _tabs.Insert(index, tab);

                    if (_selectedIndex == _dragTabIndex)
                        _selectedIndex = index;
                    else if (_selectedIndex > _dragTabIndex && _selectedIndex <= index)
                        _selectedIndex--;
                    else if (_selectedIndex < _dragTabIndex && _selectedIndex >= index)
                        _selectedIndex++;

                    _dragTabIndex = index;
                    Invalidate();
                }
            }

            if (_hoveredIndex != index)
            {
                _hoveredIndex = index;
                Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _dragTabIndex = -1;
            _isDragging = false;
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (_hoveredIndex >= 0)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            var index = GetTabIndexAt(e.Location);
            if (index < 0)
            {
                NewTabRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class FileTab
    {
        public Guid Id { get; set; }
        public string Path { get; set; }
        public string Title { get; set; }
        public List<string> BackHistory { get; set; } = new List<string>();
        public List<string> ForwardHistory { get; set; } = new List<string>();
    }

    public class TabEventArgs : EventArgs
    {
        public FileTab Tab { get; }
        public int Index { get; }

        public TabEventArgs(FileTab tab, int index)
        {
            Tab = tab;
            Index = index;
        }
    }
}