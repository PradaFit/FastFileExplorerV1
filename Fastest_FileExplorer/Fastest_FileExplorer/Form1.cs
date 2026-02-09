using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fastest_FileExplorer.Core;
using Fastest_FileExplorer.UI;

namespace Fastest_FileExplorer
{
    public partial class Form1 : Form
    {
        private FileIndexer _indexer;
        private SearchEngine _searchEngine;
        private FileSystemCache _cache;
        private CancellationTokenSource _cts;
        private string _currentPath;
        private List<string> _clipboardFiles = new List<string>();
        private bool _isCut = false;
        private bool _isSearchMode = false;
        private DiscordRpcClient _discordRpc;
        private CommandPalette _commandPalette;

        // discord app id
        private const string DISCORD_CLIENT_ID = "123456";

        public Form1()
        {
            _cache = new FileSystemCache();
            _indexer = new FileIndexer();
            _searchEngine = new SearchEngine(_indexer);
            _cts = new CancellationTokenSource();

            InitializeComponent();
            InitializeNavigationPanel();
            SetupEventHandlers();
            ApplyTheme();
            InitializeDiscordRpc();

            var startPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            NavigateTo(startPath);

            StartBackgroundIndexing();
        }

        private void InitializeDiscordRpc()
        {
            try
            {
                _discordRpc = new DiscordRpcClient(DISCORD_CLIENT_ID);
                Task.Run(() =>
                {
                    try
                    {
                        if (_discordRpc.Connect())
                        {
                            UpdateDiscordPresence("Starting up...", "Fastest File Explorer");
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }

        private void UpdateDiscordPresence(string details, string state)
        {
            try
            {
                if (_discordRpc == null || !_discordRpc.IsConnected) return;

                _discordRpc.SetPresence(new DiscordPresence
                {
                    Details = TruncateString(details, 128),
                    State = TruncateString(state, 128),
                    LargeImageKey = "file_explorer",
                    LargeImageText = "Fastest File Explorer - by PradaFit",
                    SmallImageKey = "Folder",
                    SmallImageText = "Developed by PradaFit",
                    ShowTimestamp = true
                });
            }
            catch { }
        }

        private static string TruncateString(string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= maxLength ? s : s.Substring(0, maxLength - 3) + "...";
        }

        private void InitializeNavigationPanel()
        {
            navigationPanel = new NavigationPanel(_cache);
            navigationPanel.Dock = DockStyle.Fill;
            navigationPanel.PathSelected += NavigationPanel_PathSelected;
            navigationPanelContainer.Controls.Add(navigationPanel);
        }

        private void SetupEventHandlers()
        {
            breadcrumbBar.PathChanged += BreadcrumbBar_PathChanged;
            breadcrumbBar.RefreshRequested += (s, e) => RefreshCurrentDirectory();

            searchBox.SearchTextChanged += SearchBox_SearchTextChanged;
            searchBox.SearchSubmitted += SearchBox_SearchSubmitted;

            fileListView.ItemActivated += FileListView_ItemActivated;
            fileListView.SelectedIndexChanged += FileListView_SelectedIndexChanged;
            fileListView.KeyDown += FileListView_KeyDown;

            _cache.DirectoryChanged += Cache_DirectoryChanged;

            _indexer.ProgressChanged += Indexer_ProgressChanged;
            _indexer.IndexingCompleted += Indexer_IndexingCompleted;

            openMenuItem.Click += OpenMenuItem_Click;
            openWithMenuItem.Click += OpenWithMenuItem_Click;
            copyMenuItem.Click += CopyMenuItem_Click;
            cutMenuItem.Click += CutMenuItem_Click;
            pasteMenuItem.Click += PasteMenuItem_Click;
            deleteMenuItem.Click += DeleteMenuItem_Click;
            renameMenuItem.Click += RenameMenuItem_Click;
            propertiesMenuItem.Click += PropertiesMenuItem_Click;

            this.KeyDown += Form1_KeyDown;
            this.FormClosing += Form1_FormClosing;
        }

        private void ApplyTheme()
        {
            Theme.ApplyTheme(this);
        }

        private void NavigateTo(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            if (!PathSecurity.IsValidPath(path))
            {
                ShowError("Invalid path specified.");
                return;
            }

            try
            {
                var normalizedPath = PathSecurity.NormalizePath(path);
                if (string.IsNullOrEmpty(normalizedPath) || !Directory.Exists(normalizedPath))
                {
                    ShowError("Path does not exist.");
                    return;
                }

                _isSearchMode = false;
                _currentPath = normalizedPath;

                breadcrumbBar.NavigateTo(normalizedPath, addToHistory: true, raiseEvent: false);
                searchBox.SetSearchPath(normalizedPath);
                searchBox.Clear();
                navigationPanel.SelectPath(normalizedPath);

                LoadDirectoryContents(normalizedPath);
                _cache.WatchDirectory(normalizedPath);
                UpdateStatus();

                var folderName = Path.GetFileName(normalizedPath);
                if (string.IsNullOrEmpty(folderName)) folderName = normalizedPath;
                UpdateDiscordPresence($"Browsing: {folderName}", normalizedPath);
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("Access denied to the specified path.");
            }
            catch (Exception ex)
            {
                ShowError($"Navigation failed: {ex.Message}");
            }
        }

        private async void LoadDirectoryContents(string path)
        {
            statusLabel.Text = "Loading...";
            fileListView.ClearItems();
            previewPanel.Clear();

            try
            {
                var contents = await _cache.GetDirectoryContentsAsync(path);

                if (contents.AccessDenied)
                {
                    statusLabel.Text = "Access denied";
                    return;
                }

                if (!contents.Exists)
                {
                    statusLabel.Text = "Path not found";
                    return;
                }

                fileListView.SetItems(contents.AllItems);
                UpdateStatus(contents.Directories.Count, contents.Files.Count);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void RefreshCurrentDirectory()
        {
            if (!string.IsNullOrEmpty(_currentPath))
            {
                _cache.InvalidateCache(_currentPath);
                LoadDirectoryContents(_currentPath);
            }
        }

        private void NavigationPanel_PathSelected(object sender, string path)
        {
            NavigateTo(path);
        }

        private void BreadcrumbBar_PathChanged(object sender, string path)
        {
            if (path != _currentPath)
            {
                NavigateTo(path);
            }
        }

        private async void SearchBox_SearchTextChanged(object sender, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                if (_isSearchMode)
                {
                    _isSearchMode = false;
                    LoadDirectoryContents(_currentPath);
                }
                return;
            }

            _isSearchMode = true;
            await PerformSearch(searchText);
        }

        private async void SearchBox_SearchSubmitted(object sender, string searchText)
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                _isSearchMode = true;
                await PerformSearch(searchText);
            }
        }

        private async Task PerformSearch(string searchText)
        {
            var sanitizedQuery = PathSecurity.SanitizeSearchQuery(searchText);
            if (string.IsNullOrEmpty(sanitizedQuery))
            {
                return;
            }

            statusLabel.Text = "Searching...";
            fileListView.ClearItems();
            UpdateDiscordPresence($"Searching: {sanitizedQuery}", "Finding files...");

            try
            {
                var query = new SearchQuery
                {
                    SearchText = sanitizedQuery,
                    SearchPath = _currentPath,
                    IncludeSubdirectories = true,
                    MaxResults = 5000
                };

                var stopwatch = Stopwatch.StartNew();
                var results = await _searchEngine.SearchAsync(query, _cts.Token);
                stopwatch.Stop();

                var items = results.Select(r => new FileSystemItem
                {
                    Name = r.Name,
                    FullPath = r.FullPath,
                    Extension = r.Extension,
                    Size = r.Size,
                    LastModified = r.LastModified,
                    IsDirectory = r.IsDirectory
                }).ToList();

                fileListView.SetItems(items);
                statusLabel.Text = $"Found {results.Count:N0} items in {stopwatch.ElapsedMilliseconds}ms";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                statusLabel.Text = $"Search error: {ex.Message}";
            }
        }

        private void FileListView_ItemActivated(object sender, FileSystemItem item)
        {
            if (item == null) return;

            if (!PathSecurity.IsValidPath(item.FullPath))
            {
                return;
            }

            if (item.IsDirectory)
            {
                NavigateTo(item.FullPath);
            }
            else
            {
                OpenFile(item.FullPath);
            }
        }

        private void FileListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems().FirstOrDefault();
            previewPanel.ShowPreview(selected);
        }

        private void OpenFile(string path)
        {
            if (!PathSecurity.IsValidPath(path) || !File.Exists(path))
            {
                ShowError("Invalid file path.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError($"Cannot open file: {ex.Message}");
            }
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems().FirstOrDefault();
            if (selected != null)
            {
                FileListView_ItemActivated(this, selected);
            }
        }

        private void OpenWithMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems().FirstOrDefault();
            if (selected != null && !selected.IsDirectory)
            {
                if (!PathSecurity.IsValidPath(selected.FullPath))
                {
                    return;
                }

                try
                {
                    var escapedPath = selected.FullPath.Replace("\"", "");
                    Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{escapedPath}\"");
                }
                catch (Exception ex)
                {
                    ShowError($"Cannot open file: {ex.Message}");
                }
            }
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems();
            if (selected.Count > 0)
            {
                _clipboardFiles = selected
                    .Where(s => PathSecurity.IsValidPath(s.FullPath))
                    .Select(s => s.FullPath)
                    .ToList();
                _isCut = false;
                statusLabel.Text = $"Copied {_clipboardFiles.Count} item(s)";
            }
        }

        private void CutMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems();
            if (selected.Count > 0)
            {
                _clipboardFiles = selected
                    .Where(s => PathSecurity.IsValidPath(s.FullPath))
                    .Select(s => s.FullPath)
                    .ToList();
                _isCut = true;
                statusLabel.Text = $"Cut {_clipboardFiles.Count} item(s)";
            }
        }

        private async void PasteMenuItem_Click(object sender, EventArgs e)
        {
            if (_clipboardFiles.Count == 0 || string.IsNullOrEmpty(_currentPath))
                return;

            statusLabel.Text = "Pasting...";

            try
            {
                await Task.Run(() =>
                {
                    foreach (var sourcePath in _clipboardFiles)
                    {
                        if (!PathSecurity.IsValidPath(sourcePath))
                            continue;

                        var fileName = PathSecurity.SanitizeFileName(Path.GetFileName(sourcePath));
                        var destPath = Path.Combine(_currentPath, fileName);
                        destPath = GetUniqueFileName(destPath);

                        if (Directory.Exists(sourcePath))
                        {
                            if (_isCut)
                                Directory.Move(sourcePath, destPath);
                            else
                                CopyDirectory(sourcePath, destPath);
                        }
                        else if (File.Exists(sourcePath))
                        {
                            if (_isCut)
                                File.Move(sourcePath, destPath);
                            else
                                File.Copy(sourcePath, destPath);
                        }
                    }
                });

                if (_isCut)
                {
                    _clipboardFiles.Clear();
                    _isCut = false;
                }

                RefreshCurrentDirectory();
                statusLabel.Text = "Paste complete";
            }
            catch (Exception ex)
            {
                ShowError($"Paste failed: {ex.Message}");
            }
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems();
            if (selected.Count == 0) return;

            var message = selected.Count == 1
                ? $"Are you sure you want to delete '{selected[0].Name}'?"
                : $"Are you sure you want to delete {selected.Count} items?";

            var result = MessageBox.Show(message, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    foreach (var item in selected)
                    {
                        if (!PathSecurity.IsValidPath(item.FullPath))
                            continue;

                        if (item.IsDirectory)
                            Directory.Delete(item.FullPath, true);
                        else
                            File.Delete(item.FullPath);
                    }

                    RefreshCurrentDirectory();
                    statusLabel.Text = $"Deleted {selected.Count} item(s)";
                }
                catch (Exception ex)
                {
                    ShowError($"Delete failed: {ex.Message}");
                }
            }
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems().FirstOrDefault();
            if (selected == null) return;

            var newName = ShowInputDialog("Rename", "Enter new name:", selected.Name);
            if (!string.IsNullOrWhiteSpace(newName) && newName != selected.Name)
            {
                var sanitizedName = PathSecurity.SanitizeFileName(newName);
                if (!PathSecurity.IsValidFileName(sanitizedName))
                {
                    ShowError("Invalid file name.");
                    return;
                }

                try
                {
                    var parentDir = Path.GetDirectoryName(selected.FullPath);
                    var newPath = Path.Combine(parentDir, sanitizedName);

                    if (selected.IsDirectory)
                        Directory.Move(selected.FullPath, newPath);
                    else
                        File.Move(selected.FullPath, newPath);

                    RefreshCurrentDirectory();
                }
                catch (Exception ex)
                {
                    ShowError($"Rename failed: {ex.Message}");
                }
            }
        }

        private void PropertiesMenuItem_Click(object sender, EventArgs e)
        {
            var selected = fileListView.GetSelectedItems().FirstOrDefault();
            if (selected != null && PathSecurity.IsValidPath(selected.FullPath))
            {
                ShowFileProperties(selected.FullPath);
            }
        }

        private void ShowFileProperties(string path)
        {
            try
            {
                var escapedPath = path.Replace("\"", "");
                var info = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{escapedPath}\"",
                    UseShellExecute = true
                };
                Process.Start(info);
            }
            catch { }
        }

        private string GetUniqueFileName(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return path;

            var directory = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var counter = 1;
            const int maxAttempts = 1000;

            while ((File.Exists(path) || Directory.Exists(path)) && counter < maxAttempts)
            {
                path = Path.Combine(directory, $"{name} ({counter}){extension}");
                counter++;
            }

            return path;
        }

        private void CopyDirectory(string source, string destination)
        {
            if (!PathSecurity.IsValidPath(source) || !PathSecurity.IsValidPath(destination))
                return;

            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var destDir = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private async void StartBackgroundIndexing()
        {
            indexProgressBar.Visible = true;
            indexStatusLabel.Text = "Indexing files...";

            try
            {
                await _indexer.StartFullIndexAsync(_cts.Token);
            }
            catch (Exception) { }
        }

        private void Indexer_ProgressChanged(object sender, IndexProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Indexer_ProgressChanged(sender, e)));
                return;
            }

            indexStatusLabel.Text = $"Indexed {e.FilesIndexed:N0} files";
        }

        private void Indexer_IndexingCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Indexer_IndexingCompleted(sender, e)));
                return;
            }

            indexProgressBar.Visible = false;
            indexStatusLabel.Text = $"Index: {_indexer.TotalFilesIndexed:N0} files";
        }

        private void FileListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                breadcrumbBar.GoUp();
                e.Handled = true;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.P)
            {
                ShowCommandPalette();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.F2)
            {
                ShowBatchRename();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                searchBox.Focus();
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.Left)
            {
                breadcrumbBar.GoBack();
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.Right)
            {
                breadcrumbBar.GoForward();
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.Up)
            {
                breadcrumbBar.GoUp();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                RefreshCurrentDirectory();
                e.Handled = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.N)
            {
                CreateNewFolder();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.T)
            {
                e.Handled = true;
            }
        }

        private void ShowCommandPalette()
        {
            if (_commandPalette == null || _commandPalette.IsDisposed)
            {
                _commandPalette = new CommandPalette();
            }

            _commandPalette.Show(this, cmd => ExecuteCommand(cmd));
        }

        private void ExecuteCommand(CommandItem cmd)
        {
            switch (cmd.ActionId)
            {
                case "back": breadcrumbBar.GoBack(); break;
                case "forward": breadcrumbBar.GoForward(); break;
                case "up": breadcrumbBar.GoUp(); break;
                case "home": NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)); break;
                case "desktop": NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)); break;
                case "documents": NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); break;
                case "downloads": NavigateTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); break;
                case "new_folder": CreateNewFolder(); break;
                case "copy": CopyMenuItem_Click(this, EventArgs.Empty); break;
                case "cut": CutMenuItem_Click(this, EventArgs.Empty); break;
                case "paste": PasteMenuItem_Click(this, EventArgs.Empty); break;
                case "delete": DeleteMenuItem_Click(this, EventArgs.Empty); break;
                case "rename": RenameMenuItem_Click(this, EventArgs.Empty); break;
                case "batch_rename": ShowBatchRename(); break;
                case "refresh": RefreshCurrentDirectory(); break;
                case "focus_search": searchBox.Focus(); break;
                case "terminal": OpenTerminalHere(); break;
                case "copy_path": CopyPathToClipboard(); break;
                case "properties": PropertiesMenuItem_Click(this, EventArgs.Empty); break;
                case "select_all": fileListView.SelectAll(); break;
                case "pin": statusLabel.Text = "Pin to Quick Access - coming soon"; break;
                case "unpin": statusLabel.Text = "Unpin from Quick Access - coming soon"; break;
            }
        }

        private void ShowBatchRename()
        {
            var selected = fileListView.GetSelectedItems()
                .Where(s => !s.IsDirectory && PathSecurity.IsValidPath(s.FullPath))
                .Select(s => s.FullPath)
                .ToList();

            if (selected.Count < 2)
            {
                MessageBox.Show("Select at least 2 files to batch rename.", "Batch Rename", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BatchRenameDialog.ShowDialog(this, selected);
            RefreshCurrentDirectory();
        }

        private void OpenTerminalHere()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = _currentPath,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void CopyPathToClipboard()
        {
            if (!string.IsNullOrEmpty(_currentPath))
            {
                Clipboard.SetText(_currentPath);
                statusLabel.Text = "Path copied to clipboard";
            }
        }

        private void CreateNewFolder()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;

            var folderName = ShowInputDialog("New Folder", "Enter folder name:", "New Folder");
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                var sanitizedName = PathSecurity.SanitizeFileName(folderName);
                if (!PathSecurity.IsValidFileName(sanitizedName))
                {
                    ShowError("Invalid folder name.");
                    return;
                }

                try
                {
                    var path = Path.Combine(_currentPath, sanitizedName);
                    Directory.CreateDirectory(path);
                    RefreshCurrentDirectory();
                }
                catch (Exception ex)
                {
                    ShowError($"Cannot create folder: {ex.Message}");
                }
            }
        }

        private void Cache_DirectoryChanged(object sender, DirectoryChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Cache_DirectoryChanged(sender, e)));
                return;
            }

            var changedDir = Path.GetDirectoryName(e.Path);
            if (string.Equals(changedDir, _currentPath, StringComparison.OrdinalIgnoreCase))
            {
                RefreshCurrentDirectory();
            }
        }

        private void UpdateStatus()
        {
            if (!string.IsNullOrEmpty(_currentPath))
            {
                statusLabel.Text = _currentPath;
            }
        }

        private void UpdateStatus(int folderCount, int fileCount)
        {
            var parts = new List<string>();
            if (folderCount > 0)
                parts.Add($"{folderCount:N0} folder{(folderCount == 1 ? "" : "s")}");
            if (fileCount > 0)
                parts.Add($"{fileCount:N0} file{(fileCount == 1 ? "" : "s")}");

            statusLabel.Text = parts.Count > 0 ? string.Join(", ", parts) : "Empty folder";
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.ClientSize = new System.Drawing.Size(400, 120);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = Theme.Background;
                form.ForeColor = Theme.TextPrimary;

                var label = new Label
                {
                    Text = prompt,
                    Location = new System.Drawing.Point(12, 15),
                    AutoSize = true,
                    ForeColor = Theme.TextPrimary
                };

                var textBox = new TextBox
                {
                    Text = defaultValue,
                    Location = new System.Drawing.Point(12, 40),
                    Size = new System.Drawing.Size(376, 24),
                    BackColor = Theme.BackgroundLighter,
                    ForeColor = Theme.TextPrimary,
                    MaxLength = 255
                };
                textBox.SelectAll();

                var okButton = new Button
                {
                    Text = "OK",
                    Location = new System.Drawing.Point(232, 75),
                    Size = new System.Drawing.Size(75, 28),
                    DialogResult = DialogResult.OK
                };
                Theme.StyleAccentButton(okButton);

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Location = new System.Drawing.Point(313, 75),
                    Size = new System.Drawing.Size(75, 28),
                    DialogResult = DialogResult.Cancel
                };
                Theme.StyleButton(cancelButton);

                form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                return form.ShowDialog(this) == DialogResult.OK ? textBox.Text : null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                _indexer?.StopIndexing();
                _cache?.StopWatching();
                _discordRpc?.Dispose();

                ToastNotification.ShowNotification(
                    "Fastest File Explorer",
                    "Developed By: PradaFit");

                MessageBox.Show(
                    "Developed By: PradaFit",
                    "Fastest File Explorer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch { }
        }
    }
}

