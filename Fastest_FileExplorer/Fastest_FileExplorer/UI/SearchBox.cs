using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class SearchBox : Panel
    {
        private TextBox _textBox;
        private Button _clearButton;
        private Button _filterButton;
        private Label _iconLabel;
        private Timer _searchTimer;
        private bool _isFocused;
        private string _placeholderText = "Search";

        public event EventHandler<string> SearchTextChanged;
        public event EventHandler<string> SearchSubmitted;
        public event EventHandler FilterClicked;

        public string SearchText => _textBox.Text;

        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                _placeholderText = value;
                UpdatePlaceholder();
            }
        }

        public int SearchDelayMs { get; set; } = 300;

        public SearchBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            Height = 32;
            BackColor = Theme.BackgroundLighter;
            Padding = new Padding(8, 4, 8, 4);

            InitializeControls();
            InitializeTimer();
        }

        private void InitializeControls()
        {
            _iconLabel = new Label
            {
                Text = Icons.Search,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left,
                BackColor = Color.Transparent
            };
            Controls.Add(_iconLabel);

            _filterButton = new Button
            {
                Text = "?",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(24, 24),
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _filterButton.FlatAppearance.BorderSize = 0;
            _filterButton.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            _filterButton.Click += (s, e) => FilterClicked?.Invoke(this, EventArgs.Empty);
            
            var filterTooltip = new ToolTip();
            filterTooltip.SetToolTip(_filterButton, "Search filters");
            Controls.Add(_filterButton);

            _clearButton = new Button
            {
                Text = "?",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(24, 24),
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                Visible = false,
                TabStop = false
            };
            _clearButton.FlatAppearance.BorderSize = 0;
            _clearButton.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            _clearButton.Click += ClearButton_Click;
            Controls.Add(_clearButton);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontRegular,
                Dock = DockStyle.Fill,
                Text = _placeholderText
            };
            _textBox.GotFocus += TextBox_GotFocus;
            _textBox.LostFocus += TextBox_LostFocus;
            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyDown += TextBox_KeyDown;
            Controls.Add(_textBox);

            _textBox.BringToFront();
        }

        private void InitializeTimer()
        {
            _searchTimer = new Timer
            {
                Interval = SearchDelayMs
            };
            _searchTimer.Tick += SearchTimer_Tick;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var borderColor = _isFocused ? Theme.BorderFocused : Theme.Border;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            
            using (var path = Theme.CreateRoundedRectangle(bounds, Theme.BorderRadius))
            using (var pen = new Pen(borderColor, _isFocused ? 2 : 1))
            {
                g.DrawPath(pen, path);
            }
        }

        private void TextBox_GotFocus(object sender, EventArgs e)
        {
            _isFocused = true;
            
            if (_textBox.Text == _placeholderText)
            {
                _textBox.Text = "";
                _textBox.ForeColor = Theme.TextPrimary;
            }
            
            Invalidate();
        }

        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            _isFocused = false;
            UpdatePlaceholder();
            Invalidate();
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            var hasText = !string.IsNullOrEmpty(_textBox.Text) && _textBox.Text != _placeholderText;
            _clearButton.Visible = hasText;

            _searchTimer.Stop();
            if (hasText)
            {
                _searchTimer.Start();
            }
            else
            {
                SearchTextChanged?.Invoke(this, "");
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _searchTimer.Stop();
                var searchText = _textBox.Text != _placeholderText ? _textBox.Text : "";
                SearchSubmitted?.Invoke(this, searchText);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Clear();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            var searchText = _textBox.Text != _placeholderText ? _textBox.Text : "";
            SearchTextChanged?.Invoke(this, searchText);
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            Clear();
            _textBox.Focus();
        }

        public void Clear()
        {
            _textBox.Text = "";
            _clearButton.Visible = false;
            SearchTextChanged?.Invoke(this, "");
        }

        public new void Focus()
        {
            _textBox.Focus();
        }

        private void UpdatePlaceholder()
        {
            if (!_isFocused && string.IsNullOrEmpty(_textBox.Text))
            {
                _textBox.Text = _placeholderText;
                _textBox.ForeColor = Theme.TextSecondary;
            }
            else if (_textBox.Text != _placeholderText)
            {
                _textBox.ForeColor = Theme.TextPrimary;
            }
        }

        public void SetSearchPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _placeholderText = $"Search in {System.IO.Path.GetFileName(path)}";
            }
            else
            {
                _placeholderText = "Search";
            }
            
            if (!_isFocused && (string.IsNullOrEmpty(_textBox.Text) || _textBox.ForeColor == Theme.TextSecondary))
            {
                _textBox.Text = _placeholderText;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchTimer?.Stop();
                _searchTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
