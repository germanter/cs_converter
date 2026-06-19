

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
            // MAIN FULLSCREEN OVERLAY
            // ==========================================
            _mainModalView = new Grid { 
                RowDefinitions = new RowDefinitions("Auto, *")
                // MaxWidth/Height removed so the header spreads to absolute screen corners
            };

            var modalBorder = new Border {
                Background = _bgBrush,
                Padding = new Thickness(40), // 40px uniform padding from screen edges
                Child = _mainModalView
            };

            // HEADER (Spans full width pushing items to corners)
            var headerGrid = new Grid { 
                ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto, Auto"),
                Margin = new Thickness(0, 0, 0, 40) // Breathing room below header
            };
            
            // TOP LEFT: Title
            var title = new TextBlock { 
                Text = "history", 
                FontSize = 40, 
                FontFamily = font, 
                Foreground = _textBrush, 
                VerticalAlignment = VerticalAlignment.Center // Centered vertically with buttons
            };
            Grid.SetColumn(title, 0);
            
            // TOP RIGHT: Clear All Button (0px border as requested)
            var btnClearAll = CreateSimpleButton("[ clear all ]", async () => await ClearAllLogs(), 18, 0);
            btnClearAll.Margin = new Thickness(0, 0, 25, 0);
            Grid.SetColumn(btnClearAll, 2);

            // TOP RIGHT: Close X Button
            var btnClose = new Button { 
                Cursor = new Cursor(StandardCursorType.Hand), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            btnClose.Click += (s, e) => this.IsVisible = false;
            btnClose.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((c, s) => 
                new TextBlock { Text = "X", FontSize = 36, FontFamily = font, Foreground = _textBrush, Background = Brushes.Transparent });
            Grid.SetColumn(btnClose, 3);

            headerGrid.Children.Add(title);
            headerGrid.Children.Add(btnClearAll);
            headerGrid.Children.Add(btnClose);
            Grid.SetRow(headerGrid, 0);
            _mainModalView.Children.Add(headerGrid);

            // SCROLLABLE LOGS LIST (Restricted to center of screen)
            _logsContainer = new StackPanel { 
                Spacing = 20 
            };
            
            var scrollViewer = new ScrollViewer {
                Content = _logsContainer,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxWidth = 950, // Keeps the table nice and centered instead of spanning the whole wide monitor
                HorizontalAlignment = HorizontalAlignment.Center
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
                BorderThickness = new Thickness(0, 0, 0, 1), // Clean bottom divider line
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 10, 10, 20) 
            };

            var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto") };

            // 1. Delete Button (Left)
            var btnDelete = CreateSimpleButton("X", async () => await DeleteLog(uuid), 16);
            btnDelete.Margin = new Thickness(0, 0, 25, 0); 
            Grid.SetColumn(btnDelete, 0);

            // 2. Info Block (Middle)
            var infoStack = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            
            var headerText = new TextBlock { Text = $"[{type.ToUpper()}] - {status.ToUpper()} - {time}", FontSize = 16, FontFamily = _font, Foreground = _textBrush, FontWeight = FontWeight.Bold };
            var pathText = new TextBlock { Text = $"Path: {fullpath}", FontSize = 14, FontFamily = _font, Foreground = _textBrush, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = 0.9 };
            var uuidText = new TextBlock { Text = $"UUID: {uuid}", FontSize = 12, FontFamily = _font, Foreground = _textBrush, Opacity = 0.5 };

            infoStack.Children.Add(headerText);
            infoStack.Children.Add(pathText);
            infoStack.Children.Add(uuidText);
            Grid.SetColumn(infoStack, 1);

            // 3. Copy Button (Right - ONLY if path exists)
            if (!string.IsNullOrWhiteSpace(fullpath))
            {
                var btnCopy = CreateSimpleButton("Copy", async () => await CopyToClipboard(fullpath), 14);
                btnCopy.Margin = new Thickness(25, 0, 0, 0);
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
        // Added borderThickness parameter default to 1 so you can bypass it for text-only buttons
        private Button CreateSimpleButton(string text, Action onClick, double fontSize, double borderThickness = 1)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
            btn.Click += (s, e) => onClick();
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border {
                    BorderBrush = _textBrush, 
                    BorderThickness = new Thickness(borderThickness), 
                    Background = Brushes.Transparent, 
                    Padding = new Thickness(15, 8), 
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