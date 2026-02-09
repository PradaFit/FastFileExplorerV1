using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Fastest_FileExplorer.Core;

namespace Fastest_FileExplorer.UI
{
    public class NavigationPanel : Panel
    {
        private List<NavItem> _items = new List<NavItem>();
        private int _hoveredIndex = -1;
        private int _selectedIndex = -1;
        private int _itemHeight = 36;
        private int _sectionHeaderHeight = 28;
        private readonly FileSystemCache _cache;

        public event EventHandler<string> PathSelected;

        public NavigationPanel(FileSystemCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            BackColor = Theme.Surface;
            AutoScroll = true;
            Padding = new Padding(0, 8, 0, 8);

            LoadItems();
        }

        private void LoadItems()
        {
            _items.Clear();

            _items.Add(new NavItem { Text = "Quick Access", IsHeader = true });
            _items.Add(new NavItem 
            { 
                Text = "Desktop", 
                Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Icon = "[D]"
            });
            _items.Add(new NavItem 
            { 
                Text = "Downloads", 
                Path = GetDownloadsPath(),
                Icon = "[v]"
            });
            _items.Add(new NavItem 
            { 
                Text = "Documents", 
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Icon = "[F]"
            });
            _items.Add(new NavItem 
            { 
                Text = "Pictures", 
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Icon = "[I]"
            });
            _items.Add(new NavItem 
            { 
                Text = "Music", 
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Icon = "[A]"
            });
            _items.Add(new NavItem 
            { 
                Text = "Videos", 
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Icon = "[V]"
            });

            _items.Add(new NavItem { Text = "This PC", IsHeader = true });
            
            foreach (var drive in _cache.GetDrives())
            {
                if (!drive.IsReady) continue;

                var icon = GetDriveIcon(drive.DriveType);
                _items.Add(new NavItem
                {
                    Text = drive.DisplayName,
                    Path = drive.Name,
                    Icon = icon,
                    IsDrive = true,
                    DriveInfo = drive
                });
            }

            _items.Add(new NavItem { Text = "Network", IsHeader = true });
            _items.Add(new NavItem
            {
                Text = "Network",
                Path = "",
                Icon = "[N]",
                IsEnabled = false
            });

            Invalidate();
        }

        public void RefreshDrives()
        {
            LoadItems();
        }

        private string GetDownloadsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        private string GetDriveIcon(DriveType driveType)
        {
            switch (driveType)
            {
                case DriveType.Fixed:
                    return Icons.DriveFixed;
                case DriveType.Removable:
                    return Icons.DriveRemovable;
                case DriveType.Network:
                    return Icons.DriveNetwork;
                case DriveType.CDRom:
                    return "[O]";
                default:
                    return Icons.Drive;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int y = AutoScrollPosition.Y + Padding.Top;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var itemHeight = item.IsHeader ? _sectionHeaderHeight : _itemHeight;
                var bounds = new Rectangle(0, y, Width, itemHeight);

                if (bounds.Bottom > 0 && bounds.Top < Height)
                {
                    DrawItem(g, item, bounds, i);
                }

                y += itemHeight;
            }
        }

        private void DrawItem(Graphics g, NavItem item, Rectangle bounds, int index)
        {
            if (item.IsHeader)
            {
                var textBounds = new Rectangle(bounds.Left + 16, bounds.Top, bounds.Width - 16, bounds.Height);
                TextRenderer.DrawText(g, item.Text, Theme.FontSmall, textBounds, Theme.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                return;
            }

            var isHovered = index == _hoveredIndex;
            var isSelected = index == _selectedIndex;

            if (isSelected || isHovered)
            {
                var bgColor = isSelected ? Theme.SurfaceSelected : Theme.SurfaceHover;
                using (var brush = new SolidBrush(bgColor))
                {
                    var bgBounds = new Rectangle(bounds.Left + 4, bounds.Top + 2, bounds.Width - 8, bounds.Height - 4);
                    using (var path = Theme.CreateRoundedRectangle(bgBounds, Theme.BorderRadius))
                    {
                        g.FillPath(brush, path);
                    }
                }
            }

            var iconBounds = new Rectangle(bounds.Left + 16, bounds.Top, 24, bounds.Height);
            TextRenderer.DrawText(g, item.Icon, Theme.FontLarge, iconBounds, 
                item.IsEnabled ? Theme.TextPrimary : Theme.TextDisabled,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            var textBoundsItem = new Rectangle(bounds.Left + 44, bounds.Top, bounds.Width - 60, bounds.Height);
            TextRenderer.DrawText(g, item.Text, Theme.FontRegular, textBoundsItem,
                item.IsEnabled ? Theme.TextPrimary : Theme.TextDisabled,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            if (item.IsDrive && item.DriveInfo != null)
            {
                var barWidth = bounds.Width - 60;
                var barHeight = 4;
                var barX = bounds.Left + 44;
                var barY = bounds.Bottom - 8;

                using (var brush = new SolidBrush(Theme.BackgroundLighter))
                {
                    g.FillRectangle(brush, barX, barY, barWidth, barHeight);
                }

                var usedWidth = (int)(barWidth * (item.DriveInfo.UsedPercentage / 100.0));
                var usageColor = item.DriveInfo.UsedPercentage > 90 ? Theme.Error :
                                item.DriveInfo.UsedPercentage > 70 ? Theme.Warning : Theme.Accent;
                
                using (var brush = new SolidBrush(usageColor))
                {
                    g.FillRectangle(brush, barX, barY, usedWidth, barHeight);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            var index = GetItemIndexAtPoint(e.Location);
            if (index != _hoveredIndex)
            {
                _hoveredIndex = index;
                Invalidate();
                
                if (index >= 0 && index < _items.Count && !_items[index].IsHeader && _items[index].IsEnabled)
                {
                    Cursor = Cursors.Hand;
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            var index = GetItemIndexAtPoint(e.Location);
            if (index >= 0 && index < _items.Count)
            {
                var item = _items[index];
                if (!item.IsHeader && item.IsEnabled && !string.IsNullOrEmpty(item.Path))
                {
                    _selectedIndex = index;
                    Invalidate();
                    PathSelected?.Invoke(this, item.Path);
                }
            }
        }

        private int GetItemIndexAtPoint(Point point)
        {
            int y = AutoScrollPosition.Y + Padding.Top;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var itemHeight = item.IsHeader ? _sectionHeaderHeight : _itemHeight;
                
                if (point.Y >= y && point.Y < y + itemHeight)
                    return i;

                y += itemHeight;
            }

            return -1;
        }

        public void SelectPath(string path)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedIndex = i;
                    Invalidate();
                    return;
                }
            }

            _selectedIndex = -1;
            Invalidate();
        }

        private class NavItem
        {
            public string Text { get; set; }
            public string Path { get; set; }
            public string Icon { get; set; } = "";
            public bool IsHeader { get; set; }
            public bool IsDrive { get; set; }
            public bool IsEnabled { get; set; } = true;
            public DriveInfoItem DriveInfo { get; set; }
        }
    }
}
