using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Fastest_FileExplorer.Core;

namespace Fastest_FileExplorer.UI
{
    public class PreviewPanel : Panel
    {
        private FileSystemItem _currentItem;
        private Image _previewImage;
        private string _textPreview;
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".webp" };
        private readonly string[] _textExtensions = { ".txt", ".cs", ".js", ".json", ".xml", ".html", ".css", ".md", ".log", ".py", ".java", ".cpp", ".c", ".h" };

        public PreviewPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            BackColor = Theme.Surface;
            AutoScroll = true;
        }

        public void ShowPreview(FileSystemItem item)
        {
            _currentItem = item;
            _previewImage?.Dispose();
            _previewImage = null;
            _textPreview = null;

            if (item == null)
            {
                Invalidate();
                return;
            }

            if (!item.IsDirectory)
            {
                var ext = item.Extension?.ToLowerInvariant();
                
                if (Array.Exists(_imageExtensions, e => e == ext))
                {
                    LoadImagePreview(item.FullPath);
                }
                else if (Array.Exists(_textExtensions, e => e == ext))
                {
                    LoadTextPreview(item.FullPath);
                }
            }

            Invalidate();
        }

        private void LoadImagePreview(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var original = Image.FromStream(stream);
                    
                    var maxSize = Math.Min(Width - 40, 300);
                    var scale = Math.Min((float)maxSize / original.Width, (float)maxSize / original.Height);
                    
                    if (scale < 1)
                    {
                        var newWidth = (int)(original.Width * scale);
                        var newHeight = (int)(original.Height * scale);
                        _previewImage = new Bitmap(original, newWidth, newHeight);
                        original.Dispose();
                    }
                    else
                    {
                        _previewImage = original;
                    }
                }
            }
            catch
            {
                _previewImage = null;
            }
        }

        private void LoadTextPreview(string path)
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    var buffer = new char[4096];
                    var read = reader.Read(buffer, 0, buffer.Length);
                    _textPreview = new string(buffer, 0, read);
                    
                    if (!reader.EndOfStream)
                    {
                        _textPreview += "\n\n... (truncated)";
                    }
                }
            }
            catch
            {
                _textPreview = null;
            }
        }

        public void Clear()
        {
            _currentItem = null;
            _previewImage?.Dispose();
            _previewImage = null;
            _textPreview = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_currentItem == null)
            {
                DrawEmptyState(g);
                return;
            }

            var y = 20;
            var padding = 20;

            // Draw icon
            var icon = _currentItem.IsDirectory ? Icons.Folder : Icons.GetFileIcon(_currentItem.Extension);
            var iconBounds = new Rectangle(padding, y, Width - padding * 2, 60);
            TextRenderer.DrawText(g, icon, new Font("Segoe UI", 32), iconBounds, Theme.TextPrimary,
                TextFormatFlags.HorizontalCenter);
            y += 70;

            // Draw name
            var nameBounds = new Rectangle(padding, y, Width - padding * 2, 40);
            TextRenderer.DrawText(g, _currentItem.Name, Theme.FontMedium, nameBounds, Theme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            y += 50;

            // Draw separator
            using (var pen = new Pen(Theme.Border))
            {
                g.DrawLine(pen, padding, y, Width - padding, y);
            }
            y += 20;

            // Draw details
            if (!_currentItem.IsDirectory)
            {
                // Size
                DrawDetailRow(g, "Size", Theme.FormatFileSize(_currentItem.Size), ref y, padding);
            }

            // Type
            var type = _currentItem.IsDirectory ? "Folder" : 
                      $"{_currentItem.Extension?.TrimStart('.').ToUpperInvariant()} File";
            DrawDetailRow(g, "Type", type, ref y, padding);

            // Modified
            DrawDetailRow(g, "Modified", _currentItem.LastModified.ToString("g"), ref y, padding);

            // Created
            DrawDetailRow(g, "Created", _currentItem.Created.ToString("g"), ref y, padding);

            // Location
            var location = Path.GetDirectoryName(_currentItem.FullPath);
            if (!string.IsNullOrEmpty(location))
            {
                DrawDetailRow(g, "Location", location, ref y, padding, true);
            }

            y += 20;

            // Draw preview
            if (_previewImage != null)
            {
                // Draw separator
                using (var pen = new Pen(Theme.Border))
                {
                    g.DrawLine(pen, padding, y, Width - padding, y);
                }
                y += 20;

                // Draw image
                var imageX = (Width - _previewImage.Width) / 2;
                g.DrawImage(_previewImage, imageX, y);
                y += _previewImage.Height + 10;
            }
            else if (!string.IsNullOrEmpty(_textPreview))
            {
                // Draw separator
                using (var pen = new Pen(Theme.Border))
                {
                    g.DrawLine(pen, padding, y, Width - padding, y);
                }
                y += 20;

                // Draw text preview
                var textBounds = new Rectangle(padding, y, Width - padding * 2, Height - y - 20);
                using (var brush = new SolidBrush(Theme.TextSecondary))
                {
                    g.DrawString(_textPreview, Theme.FontSmall, brush, textBounds);
                }
            }
        }

        private void DrawDetailRow(Graphics g, string label, string value, ref int y, int padding, bool wrap = false)
        {
            var labelBounds = new Rectangle(padding, y, 80, 20);
            TextRenderer.DrawText(g, label, Theme.FontRegular, labelBounds, Theme.TextSecondary,
                TextFormatFlags.Left);

            var valueWidth = Width - padding - 80 - padding;
            var valueBounds = new Rectangle(padding + 80, y, valueWidth, wrap ? 40 : 20);
            var flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
            if (wrap) flags |= TextFormatFlags.WordBreak;
            
            TextRenderer.DrawText(g, value, Theme.FontRegular, valueBounds, Theme.TextPrimary, flags);

            y += wrap ? 50 : 24;
        }

        private void DrawEmptyState(Graphics g)
        {
            var text = "Select a file to preview";
            var bounds = new Rectangle(0, 0, Width, Height);
            TextRenderer.DrawText(g, text, Theme.FontRegular, bounds, Theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
