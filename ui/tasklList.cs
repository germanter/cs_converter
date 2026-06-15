using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using WriterHead;

namespace convix
{
    public class TaskListUI : Border
    {
        private StackPanel _panel;
        private Grid _titleGrid;
        private Button _clearAllBtn;

        private SolidColorBrush _textBrush;
        private SolidColorBrush _bgBrush;
        private double _iconSize;
        private double _textSize;
        private double _innerThickness;
        
        private string _currentCategory = "";
        
        private readonly object _ctsLock = new(); 
        private CancellationTokenSource? _cts;
        private List<string> _currentUuids = new(); 

        public TaskListUI(SolidColorBrush textBrush, SolidColorBrush bgBrush, double borderThick, double innerThick, double gap, double titleSize, double textSize, double iconSize)
        {
            _textBrush = textBrush;
            _bgBrush = bgBrush;
            _iconSize = iconSize;
            _textSize = textSize; 
            _innerThickness = innerThick;

            this.BorderBrush = textBrush;
            this.BorderThickness = new Thickness(borderThick);
            this.Background = bgBrush;
            this.Margin = new Thickness(-borderThick, 0, 0, 0);
            this.ZIndex = 1;

            _panel = new StackPanel { Margin = new Thickness(gap), Spacing = gap };

            _titleGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };

            var titleBlock = new TextBlock { Text = "Tasklist", FontSize = titleSize, Foreground = textBrush, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(titleBlock, 0);

            _clearAllBtn = new Button
            {
                Content = "[ Clear all ]",
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_clearAllBtn, 1);

            _clearAllBtn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new TextBlock
                {
                    Text = control.Content?.ToString() ?? "", 
                    FontSize = _textSize,
                    Foreground = _textBrush,
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center
                };
            });

            _clearAllBtn.Click += async (s, e) => 
            {
                try 
                {
                    if (_currentUuids.Count == 0 || !_clearAllBtn.IsEnabled) return;
                    _clearAllBtn.IsEnabled = false; 
                    
                    // FIX: Cancel any ongoing UI rendering so we don't accidentally repopulate ghost data
                    lock (_ctsLock)
                    {
                        _cts?.Cancel();
                    }

                    var uuidsToNuke = new List<string>(_currentUuids);
                    _currentUuids.Clear();

                    _panel.Children.Clear();
                    _panel.Children.Add(_titleGrid);

                    await Writer.Mode5_NukeLogsAsync(uuidsToNuke);
                }
                catch { /* FIX: Silent kill global app crash on DB failure */ }
                finally 
                {
                    _clearAllBtn.IsEnabled = true; 
                }
            };

            _titleGrid.Children.Add(titleBlock);
            _titleGrid.Children.Add(_clearAllBtn);

            _panel.Children.Add(_titleGrid);
            this.Child = _panel;
        }

        public async void Refresh(string category, string snapshot)
        {
            try 
            {
                _currentCategory = category;
                
                CancellationToken token;
                lock (_ctsLock)
                {
                    if (_cts != null)
                    {
                        _cts.Cancel();
                        _cts.Dispose(); // FIX: Killed persistent CancellationToken memory leak
                    }
                    _cts = new CancellationTokenSource();
                    token = _cts.Token;
                }

                var result = await TaskListHelper.GetCompletedTasksAsync(category, snapshot, token);
                
                if (token.IsCancellationRequested || _currentCategory != category) return;

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (token.IsCancellationRequested || _currentCategory != category) return;

                        _panel.Children.Clear();
                        _panel.Children.Add(_titleGrid);
                        _currentUuids.Clear();

                        if (result.Status == "success")
                        {
                            // OPTIMIZATION: Maximize capacity instantly to prevent heavy List resizing under the hood
                            _currentUuids.Capacity = Math.Max(_currentUuids.Capacity, result.Tasks.Count);
                            
                            foreach (var task in result.Tasks)
                            {
                                if (token.IsCancellationRequested) break; // FIX: Prevent redundant rendering loops
                                _currentUuids.Add(task.Uuid);
                                _panel.Children.Add(CreateTaskItem(task.FullPath, task.Uuid));
                            }
                        }
                    }
                    catch { /* FIX: Silent kill Disposed UI exceptions */ }
                }, DispatcherPriority.Normal);
            }
            catch { /* FIX: Silent kill Refresh thread failure */ }
        }

        private Border CreateTaskItem(string fullPath, string uuid)
        {
            fullPath ??= string.Empty;
            uuid ??= string.Empty;

            bool isFolder = false;
            try 
            {
                // FIX: Prevents application lockup if mapped network drive disconnects
                isFolder = fullPath.EndsWith("/") || fullPath.EndsWith("\\") || Directory.Exists(fullPath);
            }
            catch { /* Silent kill */ }

            string cleanPath = fullPath.TrimEnd('/', '\\');
            string displayPath = fullPath;
            
            try
            {
                char sep = Path.DirectorySeparatorChar;
                string fileName = Path.GetFileName(cleanPath) ?? "";
                string? dirName = Path.GetDirectoryName(cleanPath);

                if (string.IsNullOrEmpty(dirName))
                {
                    displayPath = sep.ToString() + fileName;
                }
                else
                {
                    string parentDir = Path.GetFileName(dirName) ?? "";
                    if (string.IsNullOrEmpty(parentDir))
                    {
                        displayPath = Path.Combine(dirName, fileName);
                    }
                    else
                    {
                        displayPath = sep.ToString() + Path.Combine(parentDir, fileName);
                    }
                }
                
                if (isFolder && !displayPath.EndsWith(sep.ToString())) 
                    displayPath += sep;
            }
            catch { /* Keep raw full path safely */ }

            var taskBorder = new Border
            {
                BorderBrush = _textBrush,
                BorderThickness = new Thickness(_innerThickness),
                Background = _bgBrush
            };

            var taskGrid = new Grid 
            { 
                ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto, Auto"),
                Margin = new Thickness(8)
            };

            var btnCancel = CreateIconButton(Assets.CancelIcon, _iconSize);
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += async (s, e) => {
                try 
                {
                    if (!btnCancel.IsEnabled) return;
                    btnCancel.IsEnabled = false;

                    _panel.Children.Remove(taskBorder);
                    _currentUuids.Remove(uuid);

                    await Writer.Mode5_NukeLogsAsync(new List<string> { uuid }); 
                }
                catch { /* FIX: Async void throws will no longer explode the app */ }
            };
            Grid.SetColumn(btnCancel, 0);
            taskGrid.Children.Add(btnCancel);

            var taskName = new TextBlock 
            { 
                Text = displayPath, 
                FontSize = _textSize, 
                Foreground = _textBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis // FIX: Avoids blowing up UI layout for mega-long paths
            };
            ToolTip.SetTip(taskName, fullPath); // FIX: Interactivity ensures you can still see the whole path
            Grid.SetColumn(taskName, 1);
            taskGrid.Children.Add(taskName);

            var btnEye = CreateIconButton(Assets.EyeIcon, _iconSize);
            btnEye.Margin = new Thickness(10, 3, 10, 0);
            btnEye.Click += async (s, e) => {
                try 
                {
                    if (!btnEye.IsEnabled) return;
                    btnEye.IsEnabled = false;

                    string target = isFolder ? cleanPath : fullPath;
                    bool targetExists = false;
                    
                    try {
                        targetExists = File.Exists(target) || Directory.Exists(target);
                    } catch { targetExists = false; }

                    if (targetExists) 
                    {
                        try 
                        {
                            Process.Start(new ProcessStartInfo {
                                FileName = "explorer",
                                Arguments = isFolder ? $"\"{target}\"" : $"/select,\"{target}\"",
                                UseShellExecute = true
                            });
                        } 
                        catch { /* FIX: Process ACL failures will no longer permanently delete valid DB logs */ }
                    } 
                    else 
                    {
                        _panel.Children.Remove(taskBorder);
                        _currentUuids.Remove(uuid);
                        await Writer.Mode5_NukeLogsAsync(new List<string> { uuid });
                    }
                } 
                catch { /* FIX: Silent global kill */ }
                finally 
                {
                    try {
                        await Task.Delay(1500);
                        if (btnEye != null) btnEye.IsEnabled = true;
                    } catch {}
                }
            };
            Grid.SetColumn(btnEye, 2);
            taskGrid.Children.Add(btnEye);

            if (!isFolder)
            {
                var btnTrash = CreateIconButton(Assets.DeleteIcon, _iconSize);
                btnTrash.Margin = new Thickness(10, 0, 0, 0);
                btnTrash.Click += async (s, e) => {
                    try 
                    {
                        if (!btnTrash.IsEnabled) return;
                        btnTrash.IsEnabled = false;

                        _panel.Children.Remove(taskBorder);
                        _currentUuids.Remove(uuid);

                        await TaskListHelper.DeleteFileAsync(fullPath);
                        await Writer.Mode5_NukeLogsAsync(new List<string> { uuid });
                    }
                    catch { /* FIX: Protect async void */ }
                };
                Grid.SetColumn(btnTrash, 3);
                taskGrid.Children.Add(btnTrash);
            }

            taskBorder.Child = taskGrid;
            return taskBorder;
        }

        private Button CreateIconButton(Assets.IconData icon, double size)
        {
            var btn = new Button
            {
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                var path = new Avalonia.Controls.Shapes.Path
                {
                    Data = Geometry.Parse(icon.PathData),
                    Width = size,
                    Height = size,
                    Stretch = Stretch.Uniform
                };

                if (icon.IsStroked)
                {
                    path.Stroke = _textBrush;
                    path.StrokeThickness = 2.0;
                    path.StrokeLineCap = PenLineCap.Round;
                    path.StrokeJoin = PenLineJoin.Round;
                    path.Fill = Brushes.Transparent;
                }
                else
                {
                    path.Fill = _textBrush;
                    path.Stroke = Brushes.Transparent;
                }

                return new Border
                {
                    Background = Brushes.Transparent,
                    Child = path
                };
            });

            return btn;
        }
    }
}