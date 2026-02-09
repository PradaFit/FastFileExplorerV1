using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class CommandPalette : Form
    {
        private readonly TextBox _searchBox;
        private readonly ListView _resultsList;
        private readonly List<CommandItem> _allCommands = new List<CommandItem>();
        private List<CommandItem> _filteredCommands = new List<CommandItem>();
        private Action<CommandItem> _onCommandSelected;

        public CommandPalette()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.BackgroundLight;
            Size = new Size(600, 400);
            KeyPreview = true;

            var container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BackgroundLight,
                Padding = new Padding(1)
            };

            var innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.Background,
                Padding = new Padding(12, 12, 12, 8)
            };

            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 14f),
                Text = ""
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.KeyDown += SearchBox_KeyDown;

            var searchIcon = new Label
            {
                Text = "??",
                Dock = DockStyle.Left,
                Width = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 12f)
            };

            var searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BackgroundLighter,
                Padding = new Padding(8, 6, 8, 6)
            };

            searchPanel.Controls.Add(_searchBox);
            searchPanel.Controls.Add(searchIcon);
            headerPanel.Controls.Add(searchPanel);

            var hintLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = "  Type to search commands, paths, or actions...",
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontSmall,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.Background
            };

            _resultsList = new ListView
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

            _resultsList.Columns.Add("Command", 400);
            _resultsList.Columns.Add("Shortcut", 150);
            _resultsList.DrawItem += ResultsList_DrawItem;
            _resultsList.DrawSubItem += ResultsList_DrawSubItem;
            _resultsList.DrawColumnHeader += (s, e) => { };
            _resultsList.MouseDoubleClick += ResultsList_MouseDoubleClick;

            innerPanel.Controls.Add(_resultsList);
            innerPanel.Controls.Add(hintLabel);
            innerPanel.Controls.Add(headerPanel);

            container.Controls.Add(innerPanel);
            Controls.Add(container);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };

            Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            };

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _allCommands.AddRange(new[]
            {
                new CommandItem("Go Back", "Navigate to previous location", "Alt+Left", CommandCategory.Navigation, "back"),
                new CommandItem("Go Forward", "Navigate to next location", "Alt+Right", CommandCategory.Navigation, "forward"),
                new CommandItem("Go Up", "Navigate to parent folder", "Alt+Up", CommandCategory.Navigation, "up"),
                new CommandItem("Go to Home", "Navigate to user folder", "Alt+Home", CommandCategory.Navigation, "home"),
                new CommandItem("Go to Desktop", "Navigate to Desktop", "", CommandCategory.Navigation, "desktop"),
                new CommandItem("Go to Documents", "Navigate to Documents", "", CommandCategory.Navigation, "documents"),
                new CommandItem("Go to Downloads", "Navigate to Downloads", "", CommandCategory.Navigation, "downloads"),
                
                new CommandItem("New Folder", "Create a new folder", "Ctrl+Shift+N", CommandCategory.File, "new_folder"),
                new CommandItem("New File", "Create a new file", "Ctrl+N", CommandCategory.File, "new_file"),
                new CommandItem("Copy", "Copy selected items", "Ctrl+C", CommandCategory.File, "copy"),
                new CommandItem("Cut", "Cut selected items", "Ctrl+X", CommandCategory.File, "cut"),
                new CommandItem("Paste", "Paste items", "Ctrl+V", CommandCategory.File, "paste"),
                new CommandItem("Delete", "Delete selected items", "Delete", CommandCategory.File, "delete"),
                new CommandItem("Rename", "Rename selected item", "F2", CommandCategory.File, "rename"),
                new CommandItem("Batch Rename", "Rename multiple files", "Ctrl+F2", CommandCategory.File, "batch_rename"),
                new CommandItem("Select All", "Select all items", "Ctrl+A", CommandCategory.File, "select_all"),
                
                new CommandItem("Toggle Dual Pane", "Split view on/off", "F6", CommandCategory.View, "dual_pane"),
                new CommandItem("Toggle Preview", "Show/hide preview panel", "Alt+P", CommandCategory.View, "preview"),
                new CommandItem("Toggle Hidden Files", "Show/hide hidden files", "Ctrl+H", CommandCategory.View, "hidden"),
                new CommandItem("Refresh", "Refresh current view", "F5", CommandCategory.View, "refresh"),
                new CommandItem("Details View", "Switch to details view", "", CommandCategory.View, "view_details"),
                new CommandItem("Large Icons", "Switch to large icons", "", CommandCategory.View, "view_icons"),
                
                new CommandItem("New Tab", "Open a new tab", "Ctrl+T", CommandCategory.Tabs, "new_tab"),
                new CommandItem("Close Tab", "Close current tab", "Ctrl+W", CommandCategory.Tabs, "close_tab"),
                new CommandItem("Next Tab", "Switch to next tab", "Ctrl+Tab", CommandCategory.Tabs, "next_tab"),
                new CommandItem("Previous Tab", "Switch to previous tab", "Ctrl+Shift+Tab", CommandCategory.Tabs, "prev_tab"),
                
                new CommandItem("Open Terminal Here", "Open command prompt in current folder", "Ctrl+`", CommandCategory.Tools, "terminal"),
                new CommandItem("Open in VS Code", "Open folder in Visual Studio Code", "", CommandCategory.Tools, "vscode"),
                new CommandItem("Copy Path", "Copy current path to clipboard", "Ctrl+Shift+C", CommandCategory.Tools, "copy_path"),
                new CommandItem("Properties", "Show properties of selected item", "Alt+Enter", CommandCategory.Tools, "properties"),
                
                new CommandItem("Focus Search", "Focus the search box", "Ctrl+F", CommandCategory.Search, "focus_search"),
                new CommandItem("Search Everywhere", "Search across all drives", "Ctrl+Shift+F", CommandCategory.Search, "search_all"),
                new CommandItem("Clear Search", "Clear search results", "Escape", CommandCategory.Search, "clear_search"),
                
                new CommandItem("Pin to Quick Access", "Pin current folder", "Ctrl+D", CommandCategory.QuickAccess, "pin"),
                new CommandItem("Unpin from Quick Access", "Unpin current folder", "Ctrl+Shift+D", CommandCategory.QuickAccess, "unpin"),
            });

            RefreshResults();
        }

        public void Show(Form parent, Action<CommandItem> onSelect)
        {
            _onCommandSelected = onSelect;
            _searchBox.Text = "";
            RefreshResults();

            if (_resultsList.Items.Count > 0)
            {
                _resultsList.Items[0].Selected = true;
            }

            Location = new Point(
                parent.Location.X + (parent.Width - Width) / 2,
                parent.Location.Y + 100);

            Show(parent);
            _searchBox.Focus();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            RefreshResults();
        }

        private void RefreshResults()
        {
            var query = _searchBox.Text.ToLowerInvariant().Trim();

            if (string.IsNullOrEmpty(query))
            {
                _filteredCommands = _allCommands.ToList();
            }
            else
            {
                _filteredCommands = _allCommands
                    .Where(c => c.Name.ToLowerInvariant().Contains(query) ||
                                c.Description.ToLowerInvariant().Contains(query) ||
                                c.Category.ToString().ToLowerInvariant().Contains(query))
                    .OrderByDescending(c => c.Name.ToLowerInvariant().StartsWith(query))
                    .ThenBy(c => c.Name)
                    .ToList();
            }

            _resultsList.BeginUpdate();
            _resultsList.Items.Clear();

            foreach (var cmd in _filteredCommands)
            {
                var item = new ListViewItem(new[] { $"{GetCategoryIcon(cmd.Category)} {cmd.Name}", cmd.Shortcut });
                item.Tag = cmd;
                item.ToolTipText = cmd.Description;
                _resultsList.Items.Add(item);
            }

            _resultsList.EndUpdate();

            if (_resultsList.Items.Count > 0)
            {
                _resultsList.Items[0].Selected = true;
                _resultsList.EnsureVisible(0);
            }
        }

        private string GetCategoryIcon(CommandCategory category)
        {
            switch (category)
            {
                case CommandCategory.Navigation: return "??";
                case CommandCategory.File: return "??";
                case CommandCategory.View: return "??";
                case CommandCategory.Tabs: return "??";
                case CommandCategory.Tools: return "??";
                case CommandCategory.Search: return "??";
                case CommandCategory.QuickAccess: return "?";
                default: return "•";
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (_resultsList.Items.Count > 0)
                {
                    var idx = _resultsList.SelectedIndices.Count > 0 ? _resultsList.SelectedIndices[0] : -1;
                    var newIdx = Math.Min(idx + 1, _resultsList.Items.Count - 1);
                    _resultsList.Items[newIdx].Selected = true;
                    _resultsList.EnsureVisible(newIdx);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (_resultsList.Items.Count > 0)
                {
                    var idx = _resultsList.SelectedIndices.Count > 0 ? _resultsList.SelectedIndices[0] : 1;
                    var newIdx = Math.Max(idx - 1, 0);
                    _resultsList.Items[newIdx].Selected = true;
                    _resultsList.EnsureVisible(newIdx);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ExecuteSelected();
                e.Handled = true;
            }
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ExecuteSelected();
        }

        private void ExecuteSelected()
        {
            if (_resultsList.SelectedItems.Count > 0)
            {
                var cmd = _resultsList.SelectedItems[0].Tag as CommandItem;
                if (cmd != null)
                {
                    Close();
                    _onCommandSelected?.Invoke(cmd);
                }
            }
        }

        private void ResultsList_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
            var isSelected = e.Item.Selected;

            // Background
            var bgColor = isSelected ? Theme.SurfaceSelected : Theme.Background;
            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Draw name
            var nameRect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 4, e.Bounds.Width - 170, e.Bounds.Height - 8);
            using (var brush = new SolidBrush(Theme.TextPrimary))
            {
                e.Graphics.DrawString(e.Item.Text, Theme.FontRegular, brush, nameRect);
            }

            // Draw shortcut
            var cmd = e.Item.Tag as CommandItem;
            if (cmd != null && !string.IsNullOrEmpty(cmd.Shortcut))
            {
                var shortcutRect = new Rectangle(e.Bounds.Right - 150, e.Bounds.Y + 4, 140, e.Bounds.Height - 8);
                using (var brush = new SolidBrush(Theme.TextSecondary))
                using (var format = new StringFormat { Alignment = StringAlignment.Far })
                {
                    e.Graphics.DrawString(cmd.Shortcut, Theme.FontSmall, brush, shortcutRect, format);
                }
            }
        }

        private void ResultsList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = false;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public class CommandItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Shortcut { get; set; }
        public CommandCategory Category { get; set; }
        public string ActionId { get; set; }

        public CommandItem(string name, string description, string shortcut, CommandCategory category, string actionId)
        {
            Name = name;
            Description = description;
            Shortcut = shortcut;
            Category = category;
            ActionId = actionId;
        }
    }

    public enum CommandCategory
    {
        Navigation,
        File,
        View,
        Tabs,
        Tools,
        Search,
        QuickAccess
    }
}