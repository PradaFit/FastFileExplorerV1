using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Fastest_FileExplorer.Core;

namespace Fastest_FileExplorer.UI
{
    public class VirtualFileListView : ListView
    {
        private List<FileSystemItem> _items = new List<FileSystemItem>();
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private int _hoveredIndex = -1;
        private SortColumn _sortColumn = SortColumn.Name;
        private bool _sortAscending = true;
        private bool _showHiddenFiles = false;
        private ViewMode _viewMode = ViewMode.Details;

        public event EventHandler<FileSystemItem> ItemActivated;

        public VirtualFileListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);

            VirtualMode = true;
            VirtualListSize = 0;
            View = View.Details;
            FullRowSelect = true;
            MultiSelect = true;
            OwnerDraw = true;
            HeaderStyle = ColumnHeaderStyle.Clickable;
            
            SetupColumns();

            RetrieveVirtualItem += OnRetrieveVirtualItem;
            DrawItem += OnDrawItem;
            DrawSubItem += OnDrawSubItem;
            DrawColumnHeader += OnDrawColumnHeader;
            ColumnClick += OnColumnClick;
            MouseDoubleClick += OnMouseDoubleClick;
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
            KeyDown += OnKeyDown;
        }

        public ViewMode CurrentViewMode
        {
            get => _viewMode;
            set
            {
                _viewMode = value;
                UpdateViewMode();
            }
        }

        public bool ShowHiddenFiles
        {
            get => _showHiddenFiles;
            set
            {
                _showHiddenFiles = value;
                RefreshItems();
            }
        }

        private void SetupColumns()
        {
            Columns.Clear();
            Columns.Add("Name", 350);
            Columns.Add("Date Modified", 150);
            Columns.Add("Type", 100);
            Columns.Add("Size", 100);
        }

        private void UpdateViewMode()
        {
            switch (_viewMode)
            {
                case ViewMode.LargeIcons:
                    View = View.LargeIcon;
                    break;
                case ViewMode.SmallIcons:
                    View = View.SmallIcon;
                    break;
                case ViewMode.List:
                    View = View.List;
                    break;
                case ViewMode.Details:
                default:
                    View = View.Details;
                    break;
            }
            Invalidate();
        }

        public void SetItems(IEnumerable<FileSystemItem> items)
        {
            BeginUpdate();
            try
            {
                _items = new List<FileSystemItem>(items);
                FilterAndSort();
                VirtualListSize = _items.Count;
                SelectedIndices.Clear();
                _selectedIndices.Clear();
            }
            finally
            {
                EndUpdate();
            }
            Invalidate();
        }

        public void AddItems(IEnumerable<FileSystemItem> items)
        {
            BeginUpdate();
            try
            {
                _items.AddRange(items);
                FilterAndSort();
                VirtualListSize = _items.Count;
            }
            finally
            {
                EndUpdate();
            }
        }

        public void ClearItems()
        {
            _items.Clear();
            VirtualListSize = 0;
            _selectedIndices.Clear();
            Invalidate();
        }

        public void SelectAll()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                SelectedIndices.Add(i);
            }
            Invalidate();
        }

        private void FilterAndSort()
        {
            if (!_showHiddenFiles)
            {
                _items.RemoveAll(item => item.IsHidden);
            }

            _items.Sort((a, b) =>
            {
                if (a.IsDirectory != b.IsDirectory)
                    return a.IsDirectory ? -1 : 1;

                int result;
                switch (_sortColumn)
                {
                    case SortColumn.DateModified:
                        result = a.LastModified.CompareTo(b.LastModified);
                        break;
                    case SortColumn.Type:
                        result = string.Compare(a.Extension, b.Extension, StringComparison.OrdinalIgnoreCase);
                        break;
                    case SortColumn.Size:
                        result = a.Size.CompareTo(b.Size);
                        break;
                    case SortColumn.Name:
                    default:
                        result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                return _sortAscending ? result : -result;
            });
        }

        private void RefreshItems()
        {
            var tempItems = new List<FileSystemItem>(_items);
            SetItems(tempItems);
        }

        private void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex >= 0 && e.ItemIndex < _items.Count)
            {
                var item = _items[e.ItemIndex];
                var listItem = new ListViewItem(item.Name)
                {
                    Tag = item
                };

                listItem.SubItems.Add(Theme.FormatDate(item.LastModified));
                listItem.SubItems.Add(item.IsDirectory ? "Folder" : GetFileType(item.Extension));
                listItem.SubItems.Add(item.IsDirectory ? "" : Theme.FormatFileSize(item.Size));

                e.Item = listItem;
            }
        }

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Theme.BackgroundLight), e.Bounds);
            
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, 
                                   e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            var text = Columns[e.ColumnIndex].Text;
            var sortIndicator = "";
            
            if ((int)_sortColumn == e.ColumnIndex)
            {
                sortIndicator = _sortAscending ? " [+]" : " [-]";
            }

            TextRenderer.DrawText(e.Graphics, text + sortIndicator, Theme.FontMedium,
                e.Bounds, Theme.TextSecondary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var item = _items[e.ItemIndex];
            var isSelected = e.Item.Selected;
            var isHovered = e.ItemIndex == _hoveredIndex;

            Color bgColor;
            if (isSelected)
                bgColor = Theme.SurfaceSelected;
            else if (isHovered)
                bgColor = Theme.SurfaceHover;
            else
                bgColor = e.ItemIndex % 2 == 0 ? Theme.Background : Theme.BackgroundLight;

            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            var textColor = isSelected ? Theme.TextPrimary : 
                           (item.IsHidden ? Theme.TextDisabled : Theme.TextPrimary);
            
            var text = e.SubItem.Text;
            var bounds = e.Bounds;

            if (e.ColumnIndex == 0)
            {
                var icon = item.IsDirectory ? Icons.Folder : Icons.GetFileIcon(item.Extension);
                var iconBounds = new Rectangle(bounds.Left + 4, bounds.Top + 2, 24, bounds.Height - 4);
                TextRenderer.DrawText(e.Graphics, icon, Theme.FontLarge, iconBounds, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                bounds = new Rectangle(bounds.Left + 30, bounds.Top, bounds.Width - 30, bounds.Height);
            }

            TextRenderer.DrawText(e.Graphics, text, Theme.FontRegular, bounds, textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private void OnColumnClick(object sender, ColumnClickEventArgs e)
        {
            var newColumn = (SortColumn)e.Column;
            if (_sortColumn == newColumn)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = newColumn;
                _sortAscending = true;
            }

            RefreshItems();
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            var hit = HitTest(e.Location);
            if (hit.Item != null && hit.Item.Tag is FileSystemItem item)
            {
                ItemActivated?.Invoke(this, item);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var hit = HitTest(e.Location);
            var newIndex = hit.Item?.Index ?? -1;
            
            if (newIndex != _hoveredIndex)
            {
                var oldIndex = _hoveredIndex;
                _hoveredIndex = newIndex;
                
                if (oldIndex >= 0 && oldIndex < VirtualListSize)
                    RedrawItems(oldIndex, oldIndex, false);
                if (newIndex >= 0 && newIndex < VirtualListSize)
                    RedrawItems(newIndex, newIndex, false);
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (_hoveredIndex >= 0)
            {
                var oldIndex = _hoveredIndex;
                _hoveredIndex = -1;
                if (oldIndex < VirtualListSize)
                    RedrawItems(oldIndex, oldIndex, false);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && SelectedIndices.Count > 0)
            {
                var index = SelectedIndices[0];
                if (index >= 0 && index < _items.Count)
                {
                    ItemActivated?.Invoke(this, _items[index]);
                }
                e.Handled = true;
            }
        }

        public FileSystemItem GetItemAt(int index)
        {
            if (index >= 0 && index < _items.Count)
                return _items[index];
            return null;
        }

        public List<FileSystemItem> GetSelectedItems()
        {
            var selected = new List<FileSystemItem>();
            foreach (int index in SelectedIndices)
            {
                if (index >= 0 && index < _items.Count)
                    selected.Add(_items[index]);
            }
            return selected;
        }

        private string GetFileType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "File";

            extension = extension.TrimStart('.').ToUpperInvariant();
            return $"{extension} File";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_items.Count == 0)
            {
                var text = "This folder is empty";
                var size = e.Graphics.MeasureString(text, Theme.FontLarge);
                var x = (Width - size.Width) / 2;
                var y = (Height - size.Height) / 2;
                
                using (var brush = new SolidBrush(Theme.TextSecondary))
                {
                    e.Graphics.DrawString(text, Theme.FontLarge, brush, x, y);
                }
            }
        }
    }

    public enum SortColumn
    {
        Name = 0,
        DateModified = 1,
        Type = 2,
        Size = 3
    }

    public enum ViewMode
    {
        LargeIcons,
        SmallIcons,
        List,
        Details
    }
}
