using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public static class Theme
    {
        public static Color Background = Color.FromArgb(25, 25, 25);
        public static Color BackgroundLight = Color.FromArgb(35, 35, 35);
        public static Color BackgroundLighter = Color.FromArgb(45, 45, 45);
        public static Color Surface = Color.FromArgb(30, 30, 30);
        public static Color SurfaceHover = Color.FromArgb(50, 50, 50);
        public static Color SurfaceSelected = Color.FromArgb(0, 120, 212);
        
        public static Color Accent = Color.FromArgb(0, 120, 212);
        public static Color AccentHover = Color.FromArgb(0, 140, 232);
        public static Color AccentPressed = Color.FromArgb(0, 100, 192);
        
        public static Color TextPrimary = Color.FromArgb(255, 255, 255);
        public static Color TextSecondary = Color.FromArgb(180, 180, 180);
        public static Color TextDisabled = Color.FromArgb(100, 100, 100);
        public static Color TextAccent = Color.FromArgb(100, 180, 255);
        
        public static Color Border = Color.FromArgb(60, 60, 60);
        public static Color BorderLight = Color.FromArgb(80, 80, 80);
        public static Color BorderFocused = Color.FromArgb(0, 120, 212);
        
        public static Color Success = Color.FromArgb(76, 175, 80);
        public static Color Warning = Color.FromArgb(255, 152, 0);
        public static Color Error = Color.FromArgb(244, 67, 54);
        public static Color Info = Color.FromArgb(33, 150, 243);
        
        public static Font FontRegular = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font FontMedium = new Font("Segoe UI Semibold", 9f, FontStyle.Regular);
        public static Font FontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font FontLarge = new Font("Segoe UI", 11f, FontStyle.Regular);
        public static Font FontSmall = new Font("Segoe UI", 8f, FontStyle.Regular);
        public static Font FontPath = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static Font FontIcon = new Font("Segoe MDL2 Assets", 12f, FontStyle.Regular);

        public static int BorderRadius = 4;
        public static int Padding = 8;
        public static int ItemHeight = 28;
        public static int IconSize = 16;
        public static int LargeIconSize = 48;

        public static void ApplyTheme(Control control)
        {
            control.BackColor = Background;
            control.ForeColor = TextPrimary;
            control.Font = FontRegular;

            foreach (Control child in control.Controls)
            {
                ApplyTheme(child);
            }

            if (control is Form form)
            {
                form.FormBorderStyle = FormBorderStyle.Sizable;
            }
            else if (control is Button btn)
            {
                StyleButton(btn);
            }
            else if (control is TextBox txt)
            {
                StyleTextBox(txt);
            }
            else if (control is ListView lv)
            {
                StyleListView(lv);
            }
            else if (control is TreeView tv)
            {
                StyleTreeView(tv);
            }
        }

        public static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = AccentPressed;
            button.BackColor = BackgroundLight;
            button.ForeColor = TextPrimary;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleAccentButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentHover;
            button.FlatAppearance.MouseDownBackColor = AccentPressed;
            button.BackColor = Accent;
            button.ForeColor = TextPrimary;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = BackgroundLighter;
            textBox.ForeColor = TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleListView(ListView listView)
        {
            listView.BackColor = Background;
            listView.ForeColor = TextPrimary;
            listView.BorderStyle = BorderStyle.None;
            listView.OwnerDraw = false;
        }

        public static void StyleTreeView(TreeView treeView)
        {
            treeView.BackColor = Background;
            treeView.ForeColor = TextPrimary;
            treeView.BorderStyle = BorderStyle.None;
            treeView.LineColor = Border;
        }

        public static void StylePanel(Panel panel)
        {
            panel.BackColor = Surface;
        }

        public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        public static string FormatDate(DateTime date)
        {
            var now = DateTime.Now;
            var diff = now - date;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";

            return date.ToString("MMM dd, yyyy");
        }
    }

    public static class Icons
    {
        public const string Folder = "[D]";
        public const string FolderOpen = "[D]";
        public const string File = "[F]";
        public const string Image = "[I]";
        public const string Video = "[V]";
        public const string Audio = "[A]";
        public const string Archive = "[Z]";
        public const string Code = "[C]";
        public const string Text = "[T]";
        public const string Pdf = "[P]";
        public const string Excel = "[X]";
        public const string Word = "[W]";
        public const string Exe = "[E]";
        public const string Drive = "[=]";
        public const string DriveFixed = "[=]";
        public const string DriveRemovable = "[=]";
        public const string DriveNetwork = "[N]";
        public const string Search = "[?]";
        public const string Settings = "[S]";
        public const string Refresh = "[R]";
        public const string Back = "<";
        public const string Forward = ">";
        public const string Up = "^";
        public const string Home = "[H]";
        public const string Star = "*";
        public const string Copy = "";
        public const string Cut = "";
        public const string Paste = "";
        public const string Delete = "";
        public const string Rename = "";
        public const string NewFolder = "";
        public const string Info = "i";
        public const string Warning = "!";
        public const string Error = "x";
        public const string Check = "ok";

        public static string GetFileIcon(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return File;

            extension = extension.ToLowerInvariant().TrimStart('.');

            switch (extension)
            {
                case "jpg":
                case "jpeg":
                case "png":
                case "gif":
                case "bmp":
                case "ico":
                case "svg":
                case "webp":
                    return Image;

                case "mp4":
                case "avi":
                case "mkv":
                case "mov":
                case "wmv":
                case "flv":
                case "webm":
                    return Video;

                case "mp3":
                case "wav":
                case "flac":
                case "aac":
                case "ogg":
                case "wma":
                case "m4a":
                    return Audio;

                case "zip":
                case "rar":
                case "7z":
                case "tar":
                case "gz":
                case "bz2":
                    return Archive;

                case "cs":
                case "js":
                case "ts":
                case "py":
                case "java":
                case "cpp":
                case "c":
                case "h":
                case "html":
                case "css":
                case "json":
                case "xml":
                case "yaml":
                case "yml":
                    return Code;

                case "txt":
                case "log":
                case "md":
                case "rtf":
                    return Text;

                case "pdf":
                    return Pdf;

                case "xls":
                case "xlsx":
                case "csv":
                    return Excel;

                case "doc":
                case "docx":
                    return Word;

                case "exe":
                case "msi":
                case "bat":
                case "cmd":
                case "ps1":
                    return Exe;

                default:
                    return File;
            }
        }
    }
}
