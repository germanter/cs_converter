using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using Glo;
using WriterHead;

namespace convix
{
    public class HistoryPanel : Border
    {
        private Grid _mainModalView;
        private StackPanel _logsContainer;
        
        private SolidColorBrush _bgBrush;
        private SolidColorBrush _textBrush;
        private FontFamily _font;

        private bool _isCooldown = false;

        public HistoryPanel(SolidColorBrush bgBrush, SolidColorBrush textBrush, FontFamily font)
        {
            _bgBrush = bgBrush;
            _textBrush = textBrush;
            _font = font;
            
            this.Background = Brushes.Transparent;
            this.IsVisible = false;
            this.ZIndex = 100;
            
            // Auto-refresh the UI if the JSON data changes globally
            Vars.OnSnapshotChanged += (newSnap) => {
                Dispatcher.UIThread.Post(() => {
                    if (this.IsVisible) RefreshUI();
                });
            };

            var rootGrid = new Grid();

            // ==========================================
            // INVISIBLE CLICK-BLOCKER BACKGROUND
            // ==========================================
            var clickBlocker = new Border { Background = Brushes.Transparent };
            clickBlocker.PointerPressed += (s, e) => e.Handled = true; 
            rootGrid.Children.Add(clickBlocker);

            // ==========================================
            // MAIN FLOATING MODAL
            // ==========================================
            _mainModalView = new Grid { 
                RowDefinitions = new RowDefinitions("Auto, *"),
                Margin = new Thickness(40),
                MaxWidth = 900,
                MaxHeight = 600,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var modalBorder = new Border {
                Background = _bgBrush,
                BorderBrush = _textBrush,
                BorderThickness = new Thickness(4),
                Padding = new Thickness(25),
                Child = _mainModalView
            };

            // HEADER
            var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto, Auto") };
            
            var title = new TextBlock { Text = "history", FontSize = 40, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(title, 0);
            
            var btnClearAll = CreateSimpleButton("[ clear all ]", async () => await ClearAllLogs(), 24);
            btnClearAll.Margin = new Thickness(0, 0, 20, 0);
            Grid.SetColumn(btnClearAll, 2);

            var btnClose = new Button { Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
            btnClose.Click += (s, e) => this.IsVisible = false;
            btnClose.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((c, s) => 
                new TextBlock { Text = "X", FontSize = 40, FontFamily = font, Foreground = _textBrush, Background = Brushes.Transparent });
            Grid.SetColumn(btnClose, 3);

            headerGrid.Children.Add(title);
            headerGrid.Children.Add(btnClearAll);
            headerGrid.Children.Add(btnClose);
            Grid.SetRow(headerGrid, 0);
            _mainModalView.Children.Add(headerGrid);

            // SCROLLABLE LOGS LIST
            _logsContainer = new StackPanel { Spacing = 10, Margin = new Thickness(0, 20, 0, 0) };
            
            var scrollViewer = new ScrollViewer {
                Content = _logsContainer,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };
            
            Grid.SetRow(scrollViewer, 1);
            _mainModalView.Children.Add(scrollViewer);

            rootGrid.Children.Add(modalBorder);
            this.Child = rootGrid;
        }

        // ==========================================
        // UI REFRESH & JSON PARSING
        // ==========================================
        public void RefreshUI()
        {
            _logsContainer.Children.Clear();

            try
            {
                var doc = JsonDocument.Parse(Vars.jsonSnapshot);
                if (doc.RootElement.TryGetProperty("logs", out var logsArray) && logsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var log in logsArray.EnumerateArray())
                    {
                        string uuid = log.TryGetProperty("uuid", out var u) ? u.GetString() ?? "" : "";
                        string type = log.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                        string path = log.TryGetProperty("fullpath", out var p) ? p.GetString() ?? "" : "";
                        string status = log.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                        string time = log.TryGetProperty("timestamp", out var ts) ? ts.GetString() ?? "" : "";

                        _logsContainer.Children.Add(CreateLogRow(uuid, type, path, status, time));
                    }
                }
                
                if (_logsContainer.Children.Count == 0)
                {
                    var emptyText = new TextBlock { 
                        Text = "No history found.", FontSize = 20, FontFamily = _font, Foreground = _textBrush, 
                        HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(20) 
                    };
                    _logsContainer.Children.Add(emptyText);
                }
            }
            catch { /* Silent kill to prevent UI thread crashing on invalid JSON */ }
        }

        private Border CreateLogRow(string uuid, string type, string fullpath, string status, string time)
        {
            var rowBorder = new Border {
                BorderBrush = _textBrush,
                BorderThickness = new Thickness(2),
                Background = Brushes.Transparent,
                Padding = new Thickness(15)
            };

            var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto") };

            // 1. Delete Button (Left)
            var btnDelete = CreateSimpleButton("X", async () => await DeleteLog(uuid), 24);
            btnDelete.Margin = new Thickness(0, 0, 15, 0);
            Grid.SetColumn(btnDelete, 0);

            // 2. Info Block (Middle)
            var infoStack = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
            
            var headerText = new TextBlock { Text = $"[{type.ToUpper()}] - {status.ToUpper()} - {time}", FontSize = 16, FontFamily = _font, Foreground = _textBrush, FontWeight = FontWeight.Bold };
            var pathText = new TextBlock { Text = $"Path: {fullpath}", FontSize = 14, FontFamily = _font, Foreground = _textBrush, TextTrimming = TextTrimming.CharacterEllipsis };
            var uuidText = new TextBlock { Text = $"UUID: {uuid}", FontSize = 12, FontFamily = _font, Foreground = _textBrush, Opacity = 0.7 };

            infoStack.Children.Add(headerText);
            infoStack.Children.Add(pathText);
            infoStack.Children.Add(uuidText);
            Grid.SetColumn(infoStack, 1);

            // 3. Copy Button (Right - ONLY if path exists)
            if (!string.IsNullOrWhiteSpace(fullpath))
            {
                var btnCopy = CreateSimpleButton("Copy", async () => await CopyToClipboard(fullpath), 18);
                btnCopy.Margin = new Thickness(15, 0, 0, 0);
                Grid.SetColumn(btnCopy, 2);
                rowGrid.Children.Add(btnCopy);
            }

            rowGrid.Children.Add(btnDelete);
            rowGrid.Children.Add(infoStack);
            
            rowBorder.Child = rowGrid;
            return rowBorder;
        }

        // ==========================================
        // STRICT 2-COLOR UI HELPERS
        // ==========================================
        private Button CreateSimpleButton(string text, Action onClick, double fontSize)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
            btn.Click += (s, e) => onClick();
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border {
                    BorderBrush = _textBrush, BorderThickness = new Thickness(2), Background = Brushes.Transparent, Padding = new Thickness(10, 5),
                    Child = new TextBlock { Text = text, FontSize = fontSize, FontFamily = _font, Foreground = _textBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                };
            });
            return btn;
        }

        // ==========================================
        // DB LOGIC & ACTIONS
        // ==========================================
        private async Task DeleteLog(string uuid)
        {
            if (_isCooldown || string.IsNullOrWhiteSpace(uuid)) return;
            _isCooldown = true;
            try {
                await Writer.Mode5_NukeLogsAsync(new List<string> { uuid });
            } catch { } finally { await Task.Delay(300); _isCooldown = false; }
        }

        private async Task ClearAllLogs()
        {
            if (_isCooldown) return;
            _isCooldown = true;
            try {
                await Writer.Mode5_NukeLogsAsync(null); // Passing null truncates all logs
            } catch { } finally { await Task.Delay(300); _isCooldown = false; }
        }

        private async Task CopyToClipboard(string path)
        {
            try {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(path);
                }
            } catch { /* Silent kill */ }
        }
    }
}