using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Fastest_FileExplorer.UI
{
    public class BatchRenameDialog : Form
    {
        private readonly List<RenameItem> _items = new List<RenameItem>();
        private readonly TextBox _patternBox;
        private readonly TextBox _replaceBox;
        private readonly ComboBox _modeCombo;
        private readonly CheckBox _useRegexCheck;
        private readonly CheckBox _caseSensitiveCheck;
        private readonly NumericUpDown _startNumberBox;
        private readonly ListView _previewList;
        private readonly Button _applyButton;
        private readonly Button _cancelButton;
        private readonly Label _statusLabel;

        public BatchRenameDialog(IEnumerable<string> filePaths)
        {
            Text = "Batch Rename - PradaFit File Explorer";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;

            foreach (var path in filePaths.Where(File.Exists))
            {
                _items.Add(new RenameItem
                {
                    OriginalPath = path,
                    OriginalName = Path.GetFileName(path),
                    NewName = Path.GetFileName(path)
                });
            }

            // Left panel - Options
            var optionsPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                Padding = new Padding(16),
                BackColor = Theme.BackgroundLight
            };

            var optionsTitle = new Label
            {
                Text = "?? Rename Options",
                Font = Theme.FontMedium,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30
            };

            // Mode selection
            var modeLabel = new Label { Text = "Mode:", Dock = DockStyle.Top, Height = 25, ForeColor = Theme.TextSecondary };
            _modeCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextPrimary
            };
            _modeCombo.Items.AddRange(new object[] {
                "Find & Replace",
                "Add Prefix",
                "Add Suffix",
                "Sequential Number",
                "Date Stamp",
                "Remove Characters",
                "Change Case"
            });
            _modeCombo.SelectedIndex = 0;
            _modeCombo.SelectedIndexChanged += (s, e) => UpdatePreview();

            // Pattern input
            var patternLabel = new Label { Text = "Find / Pattern:", Dock = DockStyle.Top, Height = 25, ForeColor = Theme.TextSecondary, Padding = new Padding(0, 8, 0, 0) };
            _patternBox = new TextBox
            {
                Dock = DockStyle.Top,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _patternBox.TextChanged += (s, e) => UpdatePreview();

            // Replace input
            var replaceLabel = new Label { Text = "Replace With:", Dock = DockStyle.Top, Height = 25, ForeColor = Theme.TextSecondary, Padding = new Padding(0, 8, 0, 0) };
            _replaceBox = new TextBox
            {
                Dock = DockStyle.Top,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _replaceBox.TextChanged += (s, e) => UpdatePreview();

            // Start number
            var startLabel = new Label { Text = "Start Number:", Dock = DockStyle.Top, Height = 25, ForeColor = Theme.TextSecondary, Padding = new Padding(0, 8, 0, 0) };
            _startNumberBox = new NumericUpDown
            {
                Dock = DockStyle.Top,
                BackColor = Theme.BackgroundLighter,
                ForeColor = Theme.TextPrimary,
                Minimum = 0,
                Maximum = 99999,
                Value = 1
            };
            _startNumberBox.ValueChanged += (s, e) => UpdatePreview();

            // Checkboxes
            _useRegexCheck = new CheckBox
            {
                Text = "Use Regular Expression",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Theme.TextPrimary,
                Padding = new Padding(0, 8, 0, 0)
            };
            _useRegexCheck.CheckedChanged += (s, e) => UpdatePreview();

            _caseSensitiveCheck = new CheckBox
            {
                Text = "Case Sensitive",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Theme.TextPrimary
            };
            _caseSensitiveCheck.CheckedChanged += (s, e) => UpdatePreview();

            // Help text
            var helpLabel = new Label
            {
                Text = "?? Tip: Use {n} for number, {d} for date,\n{ext} for extension, {name} for filename",
                Dock = DockStyle.Top,
                Height = 50,
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontSmall,
                Padding = new Padding(0, 16, 0, 0)
            };

            optionsPanel.Controls.Add(helpLabel);
            optionsPanel.Controls.Add(_caseSensitiveCheck);
            optionsPanel.Controls.Add(_useRegexCheck);
            optionsPanel.Controls.Add(_startNumberBox);
            optionsPanel.Controls.Add(startLabel);
            optionsPanel.Controls.Add(_replaceBox);
            optionsPanel.Controls.Add(replaceLabel);
            optionsPanel.Controls.Add(_patternBox);
            optionsPanel.Controls.Add(patternLabel);
            optionsPanel.Controls.Add(_modeCombo);
            optionsPanel.Controls.Add(modeLabel);
            optionsPanel.Controls.Add(optionsTitle);

            // Right panel - Preview
            var previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            var previewTitle = new Label
            {
                Text = $"?? Preview ({_items.Count} files)",
                Font = Theme.FontMedium,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30
            };

            _previewList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Theme.Background,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontRegular,
                BorderStyle = BorderStyle.FixedSingle
            };
            _previewList.Columns.Add("Original Name", 250);
            _previewList.Columns.Add("?", 30);
            _previewList.Columns.Add("New Name", 250);

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            previewPanel.Controls.Add(_previewList);
            previewPanel.Controls.Add(_statusLabel);
            previewPanel.Controls.Add(previewTitle);

            // Bottom buttons
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(16, 8, 16, 8)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 34),
                Dock = DockStyle.Right,
                DialogResult = DialogResult.Cancel
            };
            Theme.StyleButton(_cancelButton);

            _applyButton = new Button
            {
                Text = "Apply Rename",
                Size = new Size(120, 34),
                Dock = DockStyle.Right,
                DialogResult = DialogResult.OK
            };
            Theme.StyleAccentButton(_applyButton);
            _applyButton.Click += ApplyButton_Click;

            var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(spacer);
            buttonPanel.Controls.Add(_applyButton);

            Controls.Add(previewPanel);
            Controls.Add(optionsPanel);
            Controls.Add(buttonPanel);

            AcceptButton = _applyButton;
            CancelButton = _cancelButton;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            _previewList.BeginUpdate();
            _previewList.Items.Clear();

            var mode = _modeCombo.SelectedIndex;
            var pattern = _patternBox.Text;
            var replace = _replaceBox.Text;
            var startNum = (int)_startNumberBox.Value;
            var useRegex = _useRegexCheck.Checked;
            var caseSensitive = _caseSensitiveCheck.Checked;

            int changedCount = 0;
            int index = 0;

            foreach (var item in _items)
            {
                var originalName = Path.GetFileNameWithoutExtension(item.OriginalName);
                var extension = Path.GetExtension(item.OriginalName);
                string newName;

                try
                {
                    switch (mode)
                    {
                        case 0: // Find & Replace
                            if (useRegex && !string.IsNullOrEmpty(pattern))
                            {
                                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                                newName = Regex.Replace(originalName, pattern, replace, options);
                            }
                            else if (!string.IsNullOrEmpty(pattern))
                            {
                                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                                newName = originalName.Replace(pattern, replace, comparison);
                            }
                            else
                            {
                                newName = originalName;
                            }
                            break;

                        case 1: // Add Prefix
                            newName = pattern + originalName;
                            break;

                        case 2: // Add Suffix
                            newName = originalName + pattern;
                            break;

                        case 3: // Sequential Number
                            var numFormat = pattern.Length > 0 ? pattern : "{n:D3}";
                            newName = ProcessTemplate(numFormat, originalName, extension, startNum + index);
                            break;

                        case 4: // Date Stamp
                            var dateFormat = pattern.Length > 0 ? pattern : "{d:yyyy-MM-dd}_{name}";
                            newName = ProcessTemplate(dateFormat, originalName, extension, index);
                            break;

                        case 5: // Remove Characters
                            if (!string.IsNullOrEmpty(pattern))
                            {
                                if (useRegex)
                                    newName = Regex.Replace(originalName, pattern, "");
                                else
                                    newName = originalName.Replace(pattern, "");
                            }
                            else
                            {
                                newName = originalName;
                            }
                            break;

                        case 6: // Change Case
                            switch (pattern.ToLower())
                            {
                                case "upper": newName = originalName.ToUpperInvariant(); break;
                                case "lower": newName = originalName.ToLowerInvariant(); break;
                                case "title": newName = ToTitleCase(originalName); break;
                                default: newName = originalName; break;
                            }
                            break;

                        default:
                            newName = originalName;
                            break;
                    }
                }
                catch
                {
                    newName = originalName;
                }

                newName = SanitizeFileName(newName) + extension;
                item.NewName = newName;

                var listItem = new ListViewItem(new[] { item.OriginalName, "?", newName });
                
                if (item.OriginalName != newName)
                {
                    listItem.ForeColor = Theme.Accent;
                    changedCount++;
                }
                else
                {
                    listItem.ForeColor = Theme.TextSecondary;
                }

                _previewList.Items.Add(listItem);
                index++;
            }

            _previewList.EndUpdate();
            _statusLabel.Text = $"{changedCount} of {_items.Count} files will be renamed";
            _applyButton.Enabled = changedCount > 0;
        }

        private string ProcessTemplate(string template, string name, string ext, int number)
        {
            var result = template
                .Replace("{name}", name)
                .Replace("{ext}", ext.TrimStart('.'))
                .Replace("{d}", DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("{t}", DateTime.Now.ToString("HHmmss"));

            // Handle {n} or {n:format}
            result = Regex.Replace(result, @"\{n(?::([^}]+))?\}", m =>
            {
                var format = m.Groups[1].Success ? m.Groups[1].Value : "D1";
                return number.ToString(format);
            });

            // Handle {d:format}
            result = Regex.Replace(result, @"\{d:([^}]+)\}", m =>
            {
                return DateTime.Now.ToString(m.Groups[1].Value);
            });

            return result;
        }

        private string ToTitleCase(string text)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            var errors = new List<string>();
            var renamed = 0;

            foreach (var item in _items.Where(i => i.OriginalName != i.NewName))
            {
                try
                {
                    var dir = Path.GetDirectoryName(item.OriginalPath);
                    var newPath = Path.Combine(dir, item.NewName);

                    if (File.Exists(newPath) && !newPath.Equals(item.OriginalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"File already exists: {item.NewName}");
                        continue;
                    }

                    File.Move(item.OriginalPath, newPath);
                    renamed++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.OriginalName}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    $"Renamed {renamed} files.\n\nErrors:\n" + string.Join("\n", errors.Take(10)),
                    "Batch Rename Results",
                    MessageBoxButtons.OK,
                    errors.Count > renamed ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
        }

        public static void ShowDialog(Form parent, IEnumerable<string> files)
        {
            using (var dialog = new BatchRenameDialog(files))
            {
                dialog.ShowDialog(parent);
            }
        }
    }

    internal class RenameItem
    {
        public string OriginalPath { get; set; }
        public string OriginalName { get; set; }
        public string NewName { get; set; }
    }

    internal static class StringExtensions
    {
        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(oldValue))
                return str;

            var result = new System.Text.StringBuilder();
            int startIndex = 0;
            int foundIndex;

            while ((foundIndex = str.IndexOf(oldValue, startIndex, comparison)) != -1)
            {
                result.Append(str, startIndex, foundIndex - startIndex);
                result.Append(newValue);
                startIndex = foundIndex + oldValue.Length;
            }

            result.Append(str, startIndex, str.Length - startIndex);
            return result.ToString();
        }
    }
}