using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class BreadcrumbBar : Panel
    {
        private List<BreadcrumbItem> _breadcrumbs = new List<BreadcrumbItem>();
        private Stack<string> _backHistory = new Stack<string>();
        private Stack<string> _forwardHistory = new Stack<string>();
        private string _currentPath;
        private int _hoveredIndex = -1;
        private bool _isEditMode = false;
        private TextBox _pathTextBox;
        private Button _backButton;
        private Button _forwardButton;
        private Button _upButton;
        private Button _refreshButton;
        private Panel _breadcrumbContainer;

        public event EventHandler<string> PathChanged;
        public event EventHandler RefreshRequested;

        public string CurrentPath => _currentPath;

        public BreadcrumbBar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);

            Height = 40;
            BackColor = Theme.BackgroundLight;
            Padding = new Padding(8, 4, 8, 4);

            InitializeControls();
        }

        private void InitializeControls()
        {
            var navPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 120,
                BackColor = Color.Transparent
            };

            _backButton = CreateNavButton(Icons.Back, "Back");
            _backButton.Location = new Point(0, 4);
            _backButton.Click += (s, e) => GoBack();
            _backButton.Enabled = false;

            _forwardButton = CreateNavButton(Icons.Forward, "Forward");
            _forwardButton.Location = new Point(32, 4);
            _forwardButton.Click += (s, e) => GoForward();
            _forwardButton.Enabled = false;

            _upButton = CreateNavButton(Icons.Up, "Up");
            _upButton.Location = new Point(64, 4);
            _upButton.Click += (s, e) => GoUp();

            _refreshButton = CreateNavButton(Icons.Refresh, "Refresh");
            _refreshButton.Location = new Point(96, 4);
            _refreshButton.Click += (s, e) => RefreshRequested?.Invoke(this, EventArgs.Empty);

            navPanel.Controls.AddRange(new Control[] { _backButton, _forwardButton, _upButton, _refreshButton });
            Controls.Add(navPanel);

            _breadcrumbContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BackgroundLighter,
                Cursor = Cursors.IBeam
            };
            _breadcrumbContainer.Paint += BreadcrumbContainer_Paint;
            _breadcrumbContainer.MouseMove += BreadcrumbContainer_MouseMove;
            _breadcrumbContainer.MouseLeave += BreadcrumbContainer_MouseLeave;
            _breadcrumbContainer.MouseClick += BreadcrumbContainer_MouseClick;
            _breadcrumbContainer.DoubleClick += BreadcrumbContainer_DoubleClick;
            Controls.Add(_breadcrumbContainer);

            _pathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontPath,
                Visible = false
            };
            _pathTextBox.KeyDown += PathTextBox_KeyDown;
            _pathTextBox.LostFocus += PathTextBox_LostFocus;
            _breadcrumbContainer.Controls.Add(_pathTextBox);
        }

        private Button CreateNavButton(string text, string tooltip)
        {
            var btn = new Button
            {
                Size = new Size(28, 28),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontRegular,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            btn.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;

            var toolTip = new ToolTip();
            toolTip.SetToolTip(btn, tooltip);

            return btn;
        }

        public void NavigateTo(string path, bool addToHistory = true, bool raiseEvent = true)
        {
            if (string.IsNullOrEmpty(path)) return;

            path = Path.GetFullPath(path).TrimEnd('\\');
            if (path.Length == 2 && path[1] == ':')
                path += "\\";

            if (addToHistory && !string.IsNullOrEmpty(_currentPath) && _currentPath != path)
            {
                _backHistory.Push(_currentPath);
                _forwardHistory.Clear();
            }

            _currentPath = path;
            UpdateBreadcrumbs();
            UpdateNavigationButtons();

            _isEditMode = false;
            _pathTextBox.Visible = false;

            if (raiseEvent)
            {
                PathChanged?.Invoke(this, path);
            }
        }

        private void UpdateBreadcrumbs()
        {
            _breadcrumbs.Clear();

            if (string.IsNullOrEmpty(_currentPath)) return;

            try
            {
                var parts = _currentPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var currentFullPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    
                    if (i == 0 && part.Length == 2 && part[1] == ':')
                    {
                        currentFullPath = part + "\\";
                        var driveName = GetDriveName(currentFullPath);
                        _breadcrumbs.Add(new BreadcrumbItem
                        {
                            Text = driveName,
                            Path = currentFullPath,
                            Icon = Icons.DriveFixed
                        });
                    }
                    else
                    {
                        currentFullPath = Path.Combine(currentFullPath, part);
                        _breadcrumbs.Add(new BreadcrumbItem
                        {
                            Text = part,
                            Path = currentFullPath,
                            Icon = ""
                        });
                    }
                }
            }
            catch
            {
                // Invalid path
            }

            _breadcrumbContainer.Invalidate();
        }

        private string GetDriveName(string drivePath)
        {
            try
            {
                var drive = new DriveInfo(drivePath);
                if (drive.IsReady && !string.IsNullOrEmpty(drive.VolumeLabel))
                {
                    return $"{drive.VolumeLabel} ({drivePath.TrimEnd('\\')})";
                }
            }
            catch { }

            return $"Local Disk ({drivePath.TrimEnd('\\')})";
        }

        private void UpdateNavigationButtons()
        {
            _backButton.Enabled = _backHistory.Count > 0;
            _forwardButton.Enabled = _forwardHistory.Count > 0;
            _upButton.Enabled = !string.IsNullOrEmpty(_currentPath) && 
                               Directory.GetParent(_currentPath) != null;
        }

        public void GoBack()
        {
            if (_backHistory.Count > 0)
            {
                _forwardHistory.Push(_currentPath);
                var previousPath = _backHistory.Pop();
                NavigateTo(previousPath, addToHistory: false);
            }
        }

        public void GoForward()
        {
            if (_forwardHistory.Count > 0)
            {
                _backHistory.Push(_currentPath);
                var nextPath = _forwardHistory.Pop();
                NavigateTo(nextPath, addToHistory: false);
            }
        }

        public void GoUp()
        {
            if (!string.IsNullOrEmpty(_currentPath))
            {
                var parent = Directory.GetParent(_currentPath);
                if (parent != null)
                {
                    NavigateTo(parent.FullName);
                }
            }
        }

        private void BreadcrumbContainer_Paint(object sender, PaintEventArgs e)
        {
            if (_isEditMode) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int x = 8;
            int y = (_breadcrumbContainer.Height - 20) / 2;

            for (int i = 0; i < _breadcrumbs.Count; i++)
            {
                var crumb = _breadcrumbs[i];
                var isHovered = i == _hoveredIndex;
                var isLast = i == _breadcrumbs.Count - 1;

                var textSize = TextRenderer.MeasureText(crumb.Text, Theme.FontRegular);
                var itemWidth = textSize.Width + 16;

                if (isHovered)
                {
                    var bgBounds = new Rectangle(x - 4, y - 2, itemWidth, 24);
                    using (var brush = new SolidBrush(Theme.SurfaceHover))
                    using (var path = Theme.CreateRoundedRectangle(bgBounds, 3))
                    {
                        g.FillPath(brush, path);
                    }
                }

                if (!string.IsNullOrEmpty(crumb.Icon))
                {
                    TextRenderer.DrawText(g, crumb.Icon, Theme.FontRegular,
                        new Point(x, y), Theme.TextPrimary);
                    x += 20;
                    itemWidth += 20;
                }

                var textColor = isLast ? Theme.TextPrimary : Theme.TextSecondary;
                TextRenderer.DrawText(g, crumb.Text, Theme.FontRegular,
                    new Point(x, y), textColor);

                crumb.Bounds = new Rectangle(x - 4, y - 2, textSize.Width + 8, 24);
                x += textSize.Width + 8;

                if (!isLast)
                {
                    TextRenderer.DrawText(g, ">", Theme.FontLarge,
                        new Point(x, y - 2), Theme.TextDisabled);
                    x += 16;
                }
            }
        }

        private void BreadcrumbContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isEditMode) return;

            var newHovered = -1;
            for (int i = 0; i < _breadcrumbs.Count; i++)
            {
                if (_breadcrumbs[i].Bounds.Contains(e.Location))
                {
                    newHovered = i;
                    break;
                }
            }

            if (newHovered != _hoveredIndex)
            {
                _hoveredIndex = newHovered;
                _breadcrumbContainer.Cursor = newHovered >= 0 ? Cursors.Hand : Cursors.IBeam;
                _breadcrumbContainer.Invalidate();
            }
        }

        private void BreadcrumbContainer_MouseLeave(object sender, EventArgs e)
        {
            if (_hoveredIndex >= 0)
            {
                _hoveredIndex = -1;
                _breadcrumbContainer.Invalidate();
            }
        }

        private void BreadcrumbContainer_MouseClick(object sender, MouseEventArgs e)
        {
            if (_isEditMode) return;

            for (int i = 0; i < _breadcrumbs.Count; i++)
            {
                if (_breadcrumbs[i].Bounds.Contains(e.Location))
                {
                    NavigateTo(_breadcrumbs[i].Path);
                    return;
                }
            }
        }

        private void BreadcrumbContainer_DoubleClick(object sender, EventArgs e)
        {
            EnterEditMode();
        }

        private void EnterEditMode()
        {
            _isEditMode = true;
            _pathTextBox.Text = _currentPath;
            _pathTextBox.Visible = true;
            _pathTextBox.SelectAll();
            _pathTextBox.Focus();
            _breadcrumbContainer.Invalidate();
        }

        private void ExitEditMode()
        {
            _isEditMode = false;
            _pathTextBox.Visible = false;
            _breadcrumbContainer.Invalidate();
        }

        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var newPath = _pathTextBox.Text.Trim();
                if (Directory.Exists(newPath))
                {
                    NavigateTo(newPath);
                }
                else
                {
                    MessageBox.Show($"The path '{newPath}' does not exist.", "Invalid Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ExitEditMode();
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                ExitEditMode();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void PathTextBox_LostFocus(object sender, EventArgs e)
        {
            ExitEditMode();
        }

        private class BreadcrumbItem
        {
            public string Text { get; set; }
            public string Path { get; set; }
            public string Icon { get; set; }
            public Rectangle Bounds { get; set; }
        }
    }
}
