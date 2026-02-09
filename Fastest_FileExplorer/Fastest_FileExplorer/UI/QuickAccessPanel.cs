using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class QuickAccessPanel : Panel
    {
        private readonly List<QuickAccessItem> _pinnedFolders = new List<QuickAccessItem>();
        private readonly List<QuickAccessItem> _recentFolders = new List<QuickAccessItem>();
        private readonly ListView _listView;
        private readonly Label _pinnedLabel;
        private readonly Label _recentLabel;
        private const int MaxRecentItems = 15;
        private string _settingsPath;

        public event EventHandler<string> PathSelected;

        public QuickAccessPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(8);

            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FastestFileExplorer",
                "quickaccess.dat");

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.Transparent
            };

            var titleLabel = new Label
            {
                Text = "? Quick Access",
                Font = Theme.FontMedium,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(4, 6)
            };
            headerPanel.Controls.Add(titleLabel);
            Controls.Add(headerPanel);

            _pinnedLabel = new Label
            {
                Text = "?? Pinned",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(4, 8, 0, 0)
            };

            _recentLabel = new Label
            {
                Text = "?? Recent",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(4, 8, 0, 0)
            };

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Background,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontRegular,
                OwnerDraw = true,
                MultiSelect = false
            };

            _listView.Columns.Add("", -2);
            _listView.DrawItem += ListView_DrawItem;
            _listView.DrawSubItem += ListView_DrawSubItem;
            _listView.DrawColumnHeader += (s, e) => { };
            _listView.MouseDoubleClick += ListView_MouseDoubleClick;
            _listView.KeyDown += ListView_KeyDown;
            _listView.MouseClick += ListView_MouseClick;

            Controls.Add(_listView);

            LoadSettings();
            InitializeDefaultPins();
            RefreshList();
        }

        private void InitializeDefaultPins()
        {
            if (_pinnedFolders.Count == 0)
            {
                var defaultPins = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                };

                foreach (var path in defaultPins.Where(Directory.Exists))
                {
                    _pinnedFolders.Add(new QuickAccessItem
                    {
                        Path = path,
                        Name = Path.GetFileName(path),
                        IsPinned = true,
                        LastAccessed = DateTime.Now
                    });
                }
            }
        }

        public void AddRecentFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            // Remove if already exists
            _recentFolders.RemoveAll(r => r.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

            // Add to top
            _recentFolders.Insert(0, new QuickAccessItem
            {
                Path = path,
                Name = Path.GetFileName(path) ?? path,
                IsPinned = false,
                LastAccessed = DateTime.Now
            });

            // Trim to max
            while (_recentFolders.Count > MaxRecentItems)
            {
                _recentFolders.RemoveAt(_recentFolders.Count - 1);
            }

            RefreshList();
            SaveSettings();
        }

        public void PinFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            if (_pinnedFolders.Any(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

            _pinnedFolders.Add(new QuickAccessItem
            {
                Path = path,
                Name = Path.GetFileName(path) ?? path,
                IsPinned = true,
                LastAccessed = DateTime.Now
            });

            RefreshList();
            SaveSettings();
        }

        public void UnpinFolder(string path)
        {
            _pinnedFolders.RemoveAll(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            RefreshList();
            SaveSettings();
        }

        public bool IsPinned(string path)
        {
            return _pinnedFolders.Any(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshList()
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();

            // Add pinned section header
            if (_pinnedFolders.Count > 0)
            {
                var pinnedHeader = new ListViewItem("?? PINNED") { Tag = "header", ForeColor = Theme.TextSecondary };
                _listView.Items.Add(pinnedHeader);

                foreach (var item in _pinnedFolders)
                {
                    var lvi = new ListViewItem($"  ?? {item.Name}") { Tag = item };
                    _listView.Items.Add(lvi);
                }
            }

            // Add recent section header
            if (_recentFolders.Count > 0)
            {
                var recentHeader = new ListViewItem("?? RECENT") { Tag = "header", ForeColor = Theme.TextSecondary };
                _listView.Items.Add(recentHeader);

                foreach (var item in _recentFolders)
                {
                    var lvi = new ListViewItem($"  ?? {item.Name}") { Tag = item };
                    _listView.Items.Add(lvi);
                }
            }

            _listView.EndUpdate();
            _listView.Columns[0].Width = -2;
        }

        private void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
            var isHeader = e.Item.Tag?.ToString() == "header";
            var isSelected = e.Item.Selected && !isHeader;

            // Background
            var bgColor = isSelected ? Theme.SurfaceSelected : Theme.Background;
            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Text
            var textColor = isHeader ? Theme.TextSecondary : (isSelected ? Theme.TextPrimary : Theme.TextPrimary);
            var font = isHeader ? Theme.FontSmall : Theme.FontRegular;
            
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 2, e.Bounds.Width - 16, e.Bounds.Height - 4);
                e.Graphics.DrawString(e.Item.Text, font, brush, textRect);
            }
        }

        private void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = false;
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            var item = _listView.SelectedItems[0].Tag as QuickAccessItem;
            if (item != null)
            {
                PathSelected?.Invoke(this, item.Path);
            }
        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _listView.SelectedItems.Count > 0)
            {
                var item = _listView.SelectedItems[0].Tag as QuickAccessItem;
                if (item != null)
                {
                    PathSelected?.Invoke(this, item.Path);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Delete && _listView.SelectedItems.Count > 0)
            {
                var item = _listView.SelectedItems[0].Tag as QuickAccessItem;
                if (item != null && item.IsPinned)
                {
                    UnpinFolder(item.Path);
                    e.Handled = true;
                }
            }
        }

        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _listView.SelectedItems.Count > 0)
            {
                var item = _listView.SelectedItems[0].Tag as QuickAccessItem;
                if (item != null)
                {
                    ShowContextMenu(item, e.Location);
                }
            }
        }

        private void ShowContextMenu(QuickAccessItem item, Point location)
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Theme.BackgroundLight;
            menu.ForeColor = Theme.TextPrimary;

            var openItem = new ToolStripMenuItem("Open");
            openItem.Click += (s, e) => PathSelected?.Invoke(this, item.Path);
            menu.Items.Add(openItem);

            menu.Items.Add(new ToolStripSeparator());

            if (item.IsPinned)
            {
                var unpinItem = new ToolStripMenuItem("Unpin from Quick Access");
                unpinItem.Click += (s, e) => UnpinFolder(item.Path);
                menu.Items.Add(unpinItem);
            }
            else
            {
                var pinItem = new ToolStripMenuItem("Pin to Quick Access");
                pinItem.Click += (s, e) => PinFolder(item.Path);
                menu.Items.Add(pinItem);

                var removeItem = new ToolStripMenuItem("Remove from Recent");
                removeItem.Click += (s, e) =>
                {
                    _recentFolders.RemoveAll(r => r.Path == item.Path);
                    RefreshList();
                    SaveSettings();
                };
                menu.Items.Add(removeItem);
            }

            menu.Show(_listView, location);
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;

                var lines = File.ReadAllLines(_settingsPath);
                var section = "";

                foreach (var line in lines)
                {
                    if (line == "[PINNED]") { section = "pinned"; continue; }
                    if (line == "[RECENT]") { section = "recent"; continue; }
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (Directory.Exists(line))
                    {
                        var item = new QuickAccessItem
                        {
                            Path = line,
                            Name = Path.GetFileName(line) ?? line,
                            IsPinned = section == "pinned",
                            LastAccessed = DateTime.Now
                        };

                        if (section == "pinned")
                            _pinnedFolders.Add(item);
                        else
                            _recentFolders.Add(item);
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var lines = new List<string> { "[PINNED]" };
                lines.AddRange(_pinnedFolders.Select(p => p.Path));
                lines.Add("[RECENT]");
                lines.AddRange(_recentFolders.Select(r => r.Path));

                File.WriteAllLines(_settingsPath, lines);
            }
            catch { }
        }
    }

    public class QuickAccessItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsPinned { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}