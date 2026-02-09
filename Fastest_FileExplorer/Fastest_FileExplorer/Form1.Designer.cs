using Fastest_FileExplorer.UI;

namespace Fastest_FileExplorer
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // UI Controls
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Splitter leftSplitter;
        private System.Windows.Forms.Splitter rightSplitter;
        private System.Windows.Forms.Panel navigationPanelContainer;
        private System.Windows.Forms.Panel fileListContainer;
        private System.Windows.Forms.Panel previewPanelContainer;
        private BreadcrumbBar breadcrumbBar;
        private SearchBox searchBox;
        private NavigationPanel navigationPanel;
        private VirtualFileListView fileListView;
        private PreviewPanel previewPanel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label indexStatusLabel;
        private System.Windows.Forms.ProgressBar indexProgressBar;
        private System.Windows.Forms.ContextMenuStrip fileContextMenu;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openWithMenuItem;
        private System.Windows.Forms.ToolStripSeparator separator1;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
        private System.Windows.Forms.ToolStripSeparator separator2;
        private System.Windows.Forms.ToolStripMenuItem deleteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameMenuItem;
        private System.Windows.Forms.ToolStripSeparator separator3;
        private System.Windows.Forms.ToolStripMenuItem propertiesMenuItem;
        private System.Windows.Forms.Timer refreshTimer;

        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _indexer?.Dispose();
                _cache?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.topPanel = new System.Windows.Forms.Panel();
            this.breadcrumbBar = new Fastest_FileExplorer.UI.BreadcrumbBar();
            this.searchBox = new Fastest_FileExplorer.UI.SearchBox();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.indexProgressBar = new System.Windows.Forms.ProgressBar();
            this.indexStatusLabel = new System.Windows.Forms.Label();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.fileListContainer = new System.Windows.Forms.Panel();
            this.fileListView = new Fastest_FileExplorer.UI.VirtualFileListView();
            this.fileContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openWithMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator1 = new System.Windows.Forms.ToolStripSeparator();
            this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator2 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator3 = new System.Windows.Forms.ToolStripSeparator();
            this.propertiesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rightSplitter = new System.Windows.Forms.Splitter();
            this.previewPanelContainer = new System.Windows.Forms.Panel();
            this.previewPanel = new Fastest_FileExplorer.UI.PreviewPanel();
            this.leftSplitter = new System.Windows.Forms.Splitter();
            this.navigationPanelContainer = new System.Windows.Forms.Panel();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.topPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.fileListContainer.SuspendLayout();
            this.fileContextMenu.SuspendLayout();
            this.previewPanelContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // topPanel
            // 
            this.topPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(35)))));
            this.topPanel.Controls.Add(this.breadcrumbBar);
            this.topPanel.Controls.Add(this.searchBox);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Padding = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.topPanel.Size = new System.Drawing.Size(1400, 50);
            this.topPanel.TabIndex = 3;
            // 
            // breadcrumbBar
            // 
            this.breadcrumbBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(35)))));
            this.breadcrumbBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.breadcrumbBar.Location = new System.Drawing.Point(0, 5);
            this.breadcrumbBar.Name = "breadcrumbBar";
            this.breadcrumbBar.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.breadcrumbBar.Size = new System.Drawing.Size(1090, 40);
            this.breadcrumbBar.TabIndex = 0;
            // 
            // searchBox
            // 
            this.searchBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.searchBox.Dock = System.Windows.Forms.DockStyle.Right;
            this.searchBox.Location = new System.Drawing.Point(1090, 5);
            this.searchBox.Name = "searchBox";
            this.searchBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.searchBox.PlaceholderText = "Search files...";
            this.searchBox.SearchDelayMs = 300;
            this.searchBox.Size = new System.Drawing.Size(300, 40);
            this.searchBox.TabIndex = 1;
            // 
            // statusPanel
            // 
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(35)))));
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Controls.Add(this.indexProgressBar);
            this.statusPanel.Controls.Add(this.indexStatusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.Location = new System.Drawing.Point(0, 772);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.statusPanel.Size = new System.Drawing.Size(1400, 28);
            this.statusPanel.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.statusLabel.Location = new System.Drawing.Point(10, 4);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 15);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Ready";
            // 
            // indexProgressBar
            // 
            this.indexProgressBar.Dock = System.Windows.Forms.DockStyle.Right;
            this.indexProgressBar.Location = new System.Drawing.Point(1230, 4);
            this.indexProgressBar.MarqueeAnimationSpeed = 30;
            this.indexProgressBar.Name = "indexProgressBar";
            this.indexProgressBar.Size = new System.Drawing.Size(150, 20);
            this.indexProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.indexProgressBar.TabIndex = 1;
            this.indexProgressBar.Visible = false;
            // 
            // indexStatusLabel
            // 
            this.indexStatusLabel.AutoSize = true;
            this.indexStatusLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.indexStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.indexStatusLabel.Location = new System.Drawing.Point(1380, 4);
            this.indexStatusLabel.Name = "indexStatusLabel";
            this.indexStatusLabel.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.indexStatusLabel.Size = new System.Drawing.Size(10, 15);
            this.indexStatusLabel.TabIndex = 2;
            // 
            // mainPanel
            // 
            this.mainPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.mainPanel.Controls.Add(this.fileListContainer);
            this.mainPanel.Controls.Add(this.rightSplitter);
            this.mainPanel.Controls.Add(this.previewPanelContainer);
            this.mainPanel.Controls.Add(this.leftSplitter);
            this.mainPanel.Controls.Add(this.navigationPanelContainer);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 50);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1400, 722);
            this.mainPanel.TabIndex = 1;
            // 
            // fileListContainer
            // 
            this.fileListContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.fileListContainer.Controls.Add(this.fileListView);
            this.fileListContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileListContainer.Location = new System.Drawing.Point(223, 0);
            this.fileListContainer.Name = "fileListContainer";
            this.fileListContainer.Size = new System.Drawing.Size(894, 722);
            this.fileListContainer.TabIndex = 0;
            // 
            // fileListView
            // 
            this.fileListView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.fileListView.ContextMenuStrip = this.fileContextMenu;
            this.fileListView.CurrentViewMode = Fastest_FileExplorer.UI.ViewMode.Details;
            this.fileListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileListView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.fileListView.FullRowSelect = true;
            this.fileListView.HideSelection = false;
            this.fileListView.Location = new System.Drawing.Point(0, 0);
            this.fileListView.Name = "fileListView";
            this.fileListView.OwnerDraw = true;
            this.fileListView.ShowHiddenFiles = false;
            this.fileListView.Size = new System.Drawing.Size(894, 722);
            this.fileListView.TabIndex = 0;
            this.fileListView.UseCompatibleStateImageBehavior = false;
            this.fileListView.View = System.Windows.Forms.View.Details;
            this.fileListView.VirtualMode = true;
            // 
            // fileContextMenu
            // 
            this.fileContextMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.fileContextMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.fileContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMenuItem,
            this.openWithMenuItem,
            this.separator1,
            this.copyMenuItem,
            this.cutMenuItem,
            this.pasteMenuItem,
            this.separator2,
            this.deleteMenuItem,
            this.renameMenuItem,
            this.separator3,
            this.propertiesMenuItem});
            this.fileContextMenu.Name = "fileContextMenu";
            this.fileContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.fileContextMenu.Size = new System.Drawing.Size(185, 198);
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Size = new System.Drawing.Size(184, 22);
            this.openMenuItem.Text = "Open";
            // 
            // openWithMenuItem
            // 
            this.openWithMenuItem.Name = "openWithMenuItem";
            this.openWithMenuItem.Size = new System.Drawing.Size(184, 22);
            this.openWithMenuItem.Text = "Open with...";
            // 
            // separator1
            // 
            this.separator1.Name = "separator1";
            this.separator1.Size = new System.Drawing.Size(181, 6);
            // 
            // copyMenuItem
            // 
            this.copyMenuItem.Name = "copyMenuItem";
            this.copyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyMenuItem.Size = new System.Drawing.Size(184, 22);
            this.copyMenuItem.Text = "Copy";
            // 
            // cutMenuItem
            // 
            this.cutMenuItem.Name = "cutMenuItem";
            this.cutMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.cutMenuItem.Size = new System.Drawing.Size(184, 22);
            this.cutMenuItem.Text = "Cut";
            // 
            // pasteMenuItem
            // 
            this.pasteMenuItem.Name = "pasteMenuItem";
            this.pasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            this.pasteMenuItem.Size = new System.Drawing.Size(184, 22);
            this.pasteMenuItem.Text = "Paste";
            // 
            // separator2
            // 
            this.separator2.Name = "separator2";
            this.separator2.Size = new System.Drawing.Size(181, 6);
            // 
            // deleteMenuItem
            // 
            this.deleteMenuItem.Name = "deleteMenuItem";
            this.deleteMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteMenuItem.Size = new System.Drawing.Size(184, 22);
            this.deleteMenuItem.Text = "Delete";
            // 
            // renameMenuItem
            // 
            this.renameMenuItem.Name = "renameMenuItem";
            this.renameMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.renameMenuItem.Size = new System.Drawing.Size(184, 22);
            this.renameMenuItem.Text = "Rename";
            // 
            // separator3
            // 
            this.separator3.Name = "separator3";
            this.separator3.Size = new System.Drawing.Size(181, 6);
            // 
            // propertiesMenuItem
            // 
            this.propertiesMenuItem.Name = "propertiesMenuItem";
            this.propertiesMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Return)));
            this.propertiesMenuItem.Size = new System.Drawing.Size(184, 22);
            this.propertiesMenuItem.Text = "Properties";
            // 
            // rightSplitter
            // 
            this.rightSplitter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.rightSplitter.Dock = System.Windows.Forms.DockStyle.Right;
            this.rightSplitter.Location = new System.Drawing.Point(1117, 0);
            this.rightSplitter.MinExtra = 400;
            this.rightSplitter.MinSize = 200;
            this.rightSplitter.Name = "rightSplitter";
            this.rightSplitter.Size = new System.Drawing.Size(3, 722);
            this.rightSplitter.TabIndex = 1;
            this.rightSplitter.TabStop = false;
            // 
            // previewPanelContainer
            // 
            this.previewPanelContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.previewPanelContainer.Controls.Add(this.previewPanel);
            this.previewPanelContainer.Dock = System.Windows.Forms.DockStyle.Right;
            this.previewPanelContainer.Location = new System.Drawing.Point(1120, 0);
            this.previewPanelContainer.Name = "previewPanelContainer";
            this.previewPanelContainer.Size = new System.Drawing.Size(280, 722);
            this.previewPanelContainer.TabIndex = 2;
            // 
            // previewPanel
            // 
            this.previewPanel.AutoScroll = true;
            this.previewPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.previewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewPanel.Location = new System.Drawing.Point(0, 0);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(280, 722);
            this.previewPanel.TabIndex = 0;
            // 
            // leftSplitter
            // 
            this.leftSplitter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.leftSplitter.Location = new System.Drawing.Point(220, 0);
            this.leftSplitter.MinExtra = 400;
            this.leftSplitter.MinSize = 150;
            this.leftSplitter.Name = "leftSplitter";
            this.leftSplitter.Size = new System.Drawing.Size(3, 722);
            this.leftSplitter.TabIndex = 3;
            this.leftSplitter.TabStop = false;
            // 
            // navigationPanelContainer
            // 
            this.navigationPanelContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.navigationPanelContainer.Dock = System.Windows.Forms.DockStyle.Left;
            this.navigationPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.navigationPanelContainer.Name = "navigationPanelContainer";
            this.navigationPanelContainer.Size = new System.Drawing.Size(220, 722);
            this.navigationPanelContainer.TabIndex = 4;
            // 
            // refreshTimer
            // 
            this.refreshTimer.Interval = 1000;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.ClientSize = new System.Drawing.Size(1400, 800);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.topPanel);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Fast File Explorer";
            this.topPanel.ResumeLayout(false);
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.mainPanel.ResumeLayout(false);
            this.fileListContainer.ResumeLayout(false);
            this.fileContextMenu.ResumeLayout(false);
            this.previewPanelContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

