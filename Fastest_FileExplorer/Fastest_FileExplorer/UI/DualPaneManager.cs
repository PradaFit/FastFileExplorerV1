using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class DualPaneManager : Panel
    {
        private Panel _leftPane;
        private Panel _rightPane;
        private Splitter _splitter;
        private bool _isDualPaneMode;
        private int _activePane; // 0 = left, 1 = right

        public event EventHandler<PaneEventArgs> ActivePaneChanged;
        public event EventHandler<PaneEventArgs> PanePathChangeRequested;

        public Panel LeftPane => _leftPane;
        public Panel RightPane => _rightPane;
        public bool IsDualPaneMode => _isDualPaneMode;
        public int ActivePane => _activePane;

        public DualPaneManager()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            InitializePanes();
            SetSinglePaneMode();
        }

        private void InitializePanes()
        {
            _leftPane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Tag = 0
            };
            _leftPane.Enter += (s, e) => SetActivePane(0);
            _leftPane.Click += (s, e) => SetActivePane(0);

            _rightPane = new Panel
            {
                Dock = DockStyle.Right,
                BackColor = Theme.Background,
                Width = 0,
                Visible = false,
                Tag = 1
            };
            _rightPane.Enter += (s, e) => SetActivePane(1);
            _rightPane.Click += (s, e) => SetActivePane(1);

            _splitter = new Splitter
            {
                Dock = DockStyle.Right,
                Width = 4,
                BackColor = Theme.Border,
                Visible = false,
                MinExtra = 200,
                MinSize = 200
            };

            // Add pane indicators
            AddPaneIndicator(_leftPane);
            AddPaneIndicator(_rightPane);

            Controls.Add(_leftPane);
            Controls.Add(_splitter);
            Controls.Add(_rightPane);
        }

        private void AddPaneIndicator(Panel pane)
        {
            var indicator = new Panel
            {
                Height = 3,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Tag = "indicator"
            };
            pane.Controls.Add(indicator);
        }

        private void UpdatePaneIndicators()
        {
            UpdateIndicator(_leftPane, _activePane == 0);
            UpdateIndicator(_rightPane, _activePane == 1);
        }

        private void UpdateIndicator(Panel pane, bool isActive)
        {
            foreach (Control c in pane.Controls)
            {
                if (c.Tag?.ToString() == "indicator")
                {
                    c.BackColor = isActive ? Theme.Accent : Color.Transparent;
                    break;
                }
            }
        }

        public void SetActivePane(int pane)
        {
            if (pane < 0 || pane > 1) return;
            if (!_isDualPaneMode && pane == 1) return;

            if (_activePane != pane)
            {
                _activePane = pane;
                UpdatePaneIndicators();
                ActivePaneChanged?.Invoke(this, new PaneEventArgs(pane));
            }
        }

        public void ToggleDualPane()
        {
            if (_isDualPaneMode)
                SetSinglePaneMode();
            else
                SetDualPaneMode();
        }

        public void SetDualPaneMode()
        {
            if (_isDualPaneMode) return;

            _isDualPaneMode = true;
            _rightPane.Width = Width / 2;
            _rightPane.Visible = true;
            _splitter.Visible = true;

            UpdatePaneIndicators();
            PanePathChangeRequested?.Invoke(this, new PaneEventArgs(1));
        }

        public void SetSinglePaneMode()
        {
            if (!_isDualPaneMode) return;

            _isDualPaneMode = false;
            _rightPane.Visible = false;
            _splitter.Visible = false;
            _rightPane.Width = 0;

            if (_activePane == 1)
            {
                SetActivePane(0);
            }

            UpdatePaneIndicators();
        }

        public Panel GetActivePanel()
        {
            return _activePane == 0 ? _leftPane : _rightPane;
        }

        public Panel GetInactivePanel()
        {
            return _activePane == 0 ? _rightPane : _leftPane;
        }

        public void SwapPanes()
        {
            if (!_isDualPaneMode) return;

            // Swap content will be handled by Form1
            SetActivePane(_activePane == 0 ? 1 : 0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_isDualPaneMode && _rightPane.Width < 100)
            {
                _rightPane.Width = Width / 2;
            }
        }
    }

    public class PaneEventArgs : EventArgs
    {
        public int PaneIndex { get; }

        public PaneEventArgs(int paneIndex)
        {
            PaneIndex = paneIndex;
        }
    }
}