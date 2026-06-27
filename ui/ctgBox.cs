
// new guy
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Data;
using Avalonia.Platform.Storage;

using ImageToPdfApp;
using PdfEngine;
using PdfUtilities;
using Orchestration;
using Glo;
using CentralGateway;

namespace convix
{
    // Common interface for the top bar and state interaction
    public interface ICtgToolUI
    {
        Border ProgressFill { get; }
        TextBlock PercentText { get; }
        Button BtnOpen { get; }
        TextBlock TxtFail { get; }
        TextBox TxtName { get; }
        void SetProgress(int? percentage, bool isFail = false);
        void AddFiles(IEnumerable<string> paths);
    }

    // Lightweight container that hosts the active tool view control
    public class CtgBox : Border
    {
        private readonly SolidColorBrush bgBrush;
        private readonly SolidColorBrush textBrush;
        private readonly FontFamily globalFont;
        private ICtgToolUI? _currentUI;

        public Border ProgressFill => _currentUI?.ProgressFill!;
        public TextBlock PercentText => _currentUI?.PercentText!;
        public Button BtnOpen => _currentUI?.BtnOpen!;
        public TextBlock TxtFail => _currentUI?.TxtFail!;
        public TextBox TxtName => _currentUI?.TxtName!;

        public CtgBox(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
        {
            this.bgBrush = bg;
            this.textBrush = text;
            this.globalFont = font;

            this.Background = bgBrush;
            this.BorderBrush = textBrush;
            this.BorderThickness = new Thickness(0);
            this.Padding = new Thickness(15);
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
        }

        public void MountUI(ICtgToolUI toolUI)
        {
            _currentUI = toolUI;
            this.Child = toolUI as Control;
        }

        public void SetProgress(int? percentage, bool isFail = false)
        {
            _currentUI?.SetProgress(percentage, isFail);
        }

        public void AddFiles(IEnumerable<string> paths)
        {
            _currentUI?.AddFiles(paths);
        }

        // ==========================================
        // UI GRAPHICS HELPERS
        // ==========================================
        public static Button CreateIconButtonStatic(Assets.IconData icon, double size, IBrush bgBrush, IBrush textBrush, Action onClick)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand) };
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border
                {
                    Background = Brushes.Transparent,
                    Child = CreateSvgIconStatic(icon, size, bgBrush, textBrush)
                };
            });
            btn.Click += (s, e) => onClick();
            return btn;
        }

        public static Avalonia.Controls.Shapes.Path CreateSvgIconStatic(Assets.IconData icon, double size, IBrush bgBrush, IBrush textBrush)
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
                path.Stroke = textBrush;
                path.StrokeThickness = 2.0;
                path.StrokeLineCap = PenLineCap.Round;
                path.StrokeJoin = PenLineJoin.Round;
                path.Fill = Brushes.Transparent;
            }
            else
            {
                path.Fill = textBrush;
                path.Stroke = Brushes.Transparent;
            }

            return path;
        }

        public static Button CreateStrictButtonStatic(string text, IBrush bgBrush, IBrush textBrush, FontFamily globalFont)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand) };
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border
                {
                    BorderBrush = textBrush,
                    BorderThickness = new Thickness(2),
                    Background = bgBrush,
                    Padding = new Thickness(10, 5),
                    Child = new TextBlock
                    {
                        Text = text,
                        Foreground = textBrush,
                        FontFamily = globalFont,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
            });
            return btn;
        }

        public static TextBox CreateStrictTextBoxStatic(string defaultText, IBrush bgBrush, IBrush textBrush, FontFamily globalFont)
        {
            var tb = new TextBox
            {
                Text = defaultText,
                Foreground = textBrush,
                Background = bgBrush,
                BorderBrush = textBrush,
                BorderThickness = new Thickness(2),
                FontFamily = globalFont,
                FontSize = 16,
                Height = 35, 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch, 
                VerticalContentAlignment = VerticalAlignment.Center,
                CaretBrush = textBrush,
                SelectionBrush = textBrush,
                SelectionForegroundBrush = bgBrush,
                Padding = new Thickness(10, 5)
            };

            var stylePointerOver = new Style(s => s.OfType<TextBox>().Class(":pointerover").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush)
                }
            };

            var styleFocus = new Style(s => s.OfType<TextBox>().Class(":focus").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush)
                }
            };

            var styleFocusWithin = new Style(s => s.OfType<TextBox>().Class(":focus-within").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush)
                }
            };

            var styleBaseBorder = new Style(s => s.OfType<TextBox>().Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush),
                    new Setter(Border.BorderThicknessProperty, new Thickness(2)),
                    new Setter(Border.PaddingProperty, new Thickness(10, 5))
                }
            };

            tb.Styles.Add(stylePointerOver);
            tb.Styles.Add(styleFocus);
            tb.Styles.Add(styleFocusWithin);
            tb.Styles.Add(styleBaseBorder);

            return tb;
        }
    }

    // =========================================================================
    // BASE OOP CLASS: ENCAPSULATES TOP BAR, DIVIDERS, DROP PANEL AND FILE OPENER
    // =========================================================================
    public abstract class CtgToolBase : Grid, ICtgToolUI
    {
        protected readonly SolidColorBrush bgBrush;
        protected readonly SolidColorBrush textBrush;
        protected readonly FontFamily globalFont;
        protected readonly Window parentWindow;

        public Border ProgressFill { get; private set; } = null!;
        public TextBlock PercentText { get; private set; } = null!;
        public Button BtnOpen { get; private set; } = null!;
        public TextBlock TxtFail { get; private set; } = null!;
        public TextBox TxtName { get; private set; } = null!;

        protected FileCollectionPanel FileCollection { get; private set; } = null!;
        private string? _saveDirectory;
        private string? _outputPath;
        private CancellationTokenSource? _cts;
        private readonly double MaxFillWidth = 240.0;
        private DateTime _lastOpenClickTime = DateTime.MinValue;
        private DateTime _lastOutputClickTime = DateTime.MinValue;
        private bool _isFolderPickerOpen = false;

        protected abstract Control CreateSettingsControl();
        protected abstract bool IsRotationEnabled { get; }
        public abstract string DefaultFileName { get; }
        protected virtual Assets.IconData? CustomTileIcon => null;
        protected virtual int MaxFilesAllowed => int.MaxValue;

        protected abstract Task<string> OnExecuteAsync(
            IReadOnlyList<FileCollectionPanel.FileItem> files,
            string saveDirectory,
            string filename,
            IProgress<double> progress,
            CancellationToken cancellationToken);

        protected CtgToolBase(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
        {
            this.parentWindow = parentWindow;
            this.bgBrush = bg;
            this.textBrush = text;
            this.globalFont = font;
        }

        protected void InitializeBase()
        {
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 0: Top bar
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 1: Line break
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 2: Settings control
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 3: Line break
            this.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Row 4: File Collection list

            // ==========================================
            // ROW 0: TOP BAR PANEL
            // ==========================================
            var topBarGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            topBarGrid.ColumnDefinitions.AddRange(new[]
            {
                new ColumnDefinition { Width = GridLength.Auto }, // START
                new ColumnDefinition { Width = GridLength.Auto }, // END
                new ColumnDefinition { Width = GridLength.Auto }, // Progress Outer
                new ColumnDefinition { Width = GridLength.Auto }, // Status Container
                new ColumnDefinition { Width = GridLength.Auto }, // OUTPUT
                new ColumnDefinition { Width = GridLength.Auto }, // clear all
                new ColumnDefinition { Width = GridLength.Auto }, // NAME
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // TxtName
            });

            var btnStart = CtgBox.CreateStrictButtonStatic("START", bgBrush, textBrush, globalFont);
            btnStart.Click += (s, e) => OnStartClicked();

            var btnEnd = CtgBox.CreateStrictButtonStatic("END", bgBrush, textBrush, globalFont);
            btnEnd.Click += (s, e) => _cts?.Cancel();

            var progressBoxOuter = new Border
            {
                BorderBrush = textBrush,
                BorderThickness = new Thickness(2),
                Background = bgBrush,
                Width = 250,
                Height = 35,
                Padding = new Thickness(3)
            };

            ProgressFill = new Border
            {
                Background = textBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            progressBoxOuter.Child = ProgressFill;

            var statusContainer = new Grid { Width = 90 };
            PercentText = new TextBlock
            {
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            BtnOpen = CtgBox.CreateStrictButtonStatic("OPEN", bgBrush, textBrush, globalFont);
            BtnOpen.HorizontalAlignment = HorizontalAlignment.Center;
            BtnOpen.IsVisible = false;
            BtnOpen.Click += (s, e) => HandleOpenClicked();

            TxtFail = new TextBlock
            {
                Text = "FAIL",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = false
            };

            statusContainer.Children.Add(PercentText);
            statusContainer.Children.Add(BtnOpen);
            statusContainer.Children.Add(TxtFail);

            var btnOutput = CtgBox.CreateStrictButtonStatic("OUTPUT", bgBrush, textBrush, globalFont);
            btnOutput.Click += async (s, e) => await OpenFolderPickerAsync();

            var btnClearAll = new TextBlock
            {
                Text = "[ clear all ]",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            btnClearAll.PointerPressed += (s, e) =>
            {
                FileCollection.Clear();
                SetProgress(null);
            };

            var lblName = new TextBlock
            {
                Text = "NAME",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };

            TxtName = CtgBox.CreateStrictTextBoxStatic(DefaultFileName, bgBrush, textBrush, globalFont);

            Grid.SetColumn(btnStart, 0);
            Grid.SetColumn(btnEnd, 1);
            Grid.SetColumn(progressBoxOuter, 2);
            Grid.SetColumn(statusContainer, 3);
            Grid.SetColumn(btnOutput, 4);
            Grid.SetColumn(btnClearAll, 5);
            Grid.SetColumn(lblName, 6);
            Grid.SetColumn(TxtName, 7);

            btnStart.Margin = new Thickness(0, 0, 15, 0);
            btnEnd.Margin = new Thickness(0, 0, 15, 0);
            progressBoxOuter.Margin = new Thickness(0, 0, 15, 0);
            statusContainer.Margin = new Thickness(0, 0, 15, 0);
            btnOutput.Margin = new Thickness(0, 0, 15, 0);
            btnClearAll.Margin = new Thickness(0, 0, 15, 0);
            lblName.Margin = new Thickness(0, 0, 15, 0);

            btnStart.VerticalAlignment = VerticalAlignment.Center;
            btnEnd.VerticalAlignment = VerticalAlignment.Center;
            progressBoxOuter.VerticalAlignment = VerticalAlignment.Center;
            statusContainer.VerticalAlignment = VerticalAlignment.Center;
            btnOutput.VerticalAlignment = VerticalAlignment.Center;

            topBarGrid.Children.Add(btnStart);
            topBarGrid.Children.Add(btnEnd);
            topBarGrid.Children.Add(progressBoxOuter);
            topBarGrid.Children.Add(statusContainer);
            topBarGrid.Children.Add(btnOutput);
            topBarGrid.Children.Add(btnClearAll);
            topBarGrid.Children.Add(lblName);
            topBarGrid.Children.Add(TxtName);

            Grid.SetRow(topBarGrid, 0);
            this.Children.Add(topBarGrid);

            // ==========================================
            // ROW 1: UPPER DIVIDER LINE
            // ==========================================
            var divider1 = new Border
            {
                Height = 4,
                Background = textBrush,
                Margin = new Thickness(-15, 10, -15, 10)
            };
            Grid.SetRow(divider1, 1);
            this.Children.Add(divider1);

            // ==========================================
            // ROW 2: PARAMETERS SETTINGS PANEL INJECTION
            // ==========================================
            var settingsControl = CreateSettingsControl();
            Grid.SetRow(settingsControl, 2);
            this.Children.Add(settingsControl);

            // ==========================================
            // ROW 3: LOWER DIVIDER LINE
            // ==========================================
            var divider2 = new Border
            {
                Height = 4,
                Background = textBrush,
                Margin = new Thickness(-15, 10, -15, 15)
            };
            Grid.SetRow(divider2, 3);
            this.Children.Add(divider2);

            // ==========================================
            // ROW 4: FILE COLLECTION PANEL (SCROLLABLE WRAP)
            // ==========================================
            FileCollection = new FileCollectionPanel(bgBrush, textBrush, globalFont, IsRotationEnabled, CustomTileIcon, MaxFilesAllowed);
            Grid.SetRow(FileCollection, 4);
            this.Children.Add(FileCollection);
        }

        public void SetProgress(int? percentage, bool isFail = false)
        {
            if (isFail)
            {
                ProgressFill.Width = 0;
                PercentText.IsVisible = false;
                BtnOpen.IsVisible = false;
                TxtFail.IsVisible = true;
                return;
            }

            if (percentage == null)
            {
                ProgressFill.Width = 0;
                PercentText.Text = "";
                PercentText.IsVisible = true;
                BtnOpen.IsVisible = false;
                TxtFail.IsVisible = false;
                return;
            }

            int p = Math.Max(0, Math.Min(100, percentage.Value));
            ProgressFill.Width = (p / 100.0) * MaxFillWidth;

            if (p >= 100 && !CentralController.isRunning)
            {
                PercentText.IsVisible = false;
                TxtFail.IsVisible = false;
                BtnOpen.IsVisible = true;
            }
            else
            {
                PercentText.Text = $"{p}%";
                PercentText.IsVisible = true;
                BtnOpen.IsVisible = false;
                TxtFail.IsVisible = false;
            }
        }

        public void AddFiles(IEnumerable<string> paths)
        {
            FileCollection.AddFiles(paths, path =>
            {
                if (string.IsNullOrEmpty(_saveDirectory))
                {
                    _saveDirectory = Path.GetDirectoryName(path) ?? "";
                }
            });
        }

        private void HandleOpenClicked()
        {
            if ((DateTime.Now - _lastOpenClickTime).TotalMilliseconds < 500) return;
            _lastOpenClickTime = DateTime.Now;

            try
            {
                if (string.IsNullOrEmpty(_outputPath)) return;

                bool isDirectory = Directory.Exists(_outputPath);
                bool isFile = File.Exists(_outputPath);

                if (!isDirectory && !isFile) return;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (isDirectory)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{_outputPath}\"");
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_outputPath}\"");
                    }
                }
                else
                {
                    string targetDir = isDirectory ? _outputPath : Path.GetDirectoryName(_outputPath)!;
                    if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = targetDir,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch {}
        }

        private async Task OpenFolderPickerAsync()
        {
            if ((DateTime.Now - _lastOutputClickTime).TotalMilliseconds < 500) return;
            if (_isFolderPickerOpen) return;

            _lastOutputClickTime = DateTime.Now;
            _isFolderPickerOpen = true;

            try
            {
                var folders = await parentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Output Directory",
                    AllowMultiple = false
                });

                if (folders != null && folders.Count > 0)
                {
                    _saveDirectory = folders[0].Path.LocalPath;
                }
            }
            catch {}
            finally
            {
                _isFolderPickerOpen = false;
            }
        }

        private async void OnStartClicked()
        {
            if (CentralController.isRunning) return;
            if (FileCollection.FileItems.Count == 0) return;

            _cts = new CancellationTokenSource();
            SetProgress(0);

            string outDir = _saveDirectory ?? Directory.GetCurrentDirectory();
            string filename = TxtName.Text ?? DefaultFileName;
            if (string.IsNullOrWhiteSpace(filename)) filename = DefaultFileName;

            try
            {
                var progressReporter = new Progress<double>(val =>
                {
                    int pct = (int)(val <= 1.0 ? val * 100 : val);
                    SetProgress(pct);
                });

                _outputPath = await OnExecuteAsync(
                    FileCollection.FileItems,
                    outDir,
                    filename,
                    progressReporter,
                    _cts.Token
                );

                if (!string.IsNullOrEmpty(_outputPath))
                {
                    SetProgress(100);
                }
                else
                {
                    SetProgress(null, isFail: true);
                }
            }
            catch (OperationCanceledException)
            {
                SetProgress(null);
            }
            catch (Exception)
            {
                SetProgress(null, isFail: true);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
    }

    // =========================================================================
    // REUSABLE PANEL: DRAGGABLE / DELETABLE WRAP PANEL WITH ROTATION TOGGLE
    // =========================================================================
    public class FileCollectionPanel : Border
    {
        public class FileItem
        {
            public string FilePath { get; set; } = "";
            public RotationSteps Rotation { get; set; } = RotationSteps.None;
            public Control Visual { get; set; } = null!;
        }

        private readonly List<FileItem> _fileItems = new();
        private readonly WrapPanel _fileWrapPanel;
        private readonly ScrollViewer _fileScrollViewer;
        private readonly SolidColorBrush _bgBrush;
        private readonly SolidColorBrush _textBrush;
        private readonly FontFamily _globalFont;
        private readonly bool _isRotationEnabled;
        private readonly Assets.IconData _tileIcon;
        private readonly int _maxFiles;

        private FileItem? _draggedItem;
        private Point _dragStartPoint;
        private Point _dragStartOffset;

        public IReadOnlyList<FileItem> FileItems => _fileItems;

        public FileCollectionPanel(SolidColorBrush bg, SolidColorBrush text, FontFamily font, bool isRotationEnabled, Assets.IconData? tileIcon = null, int maxFiles = int.MaxValue)
        {
            _bgBrush = bg;
            _textBrush = text;
            _globalFont = font;
            _isRotationEnabled = isRotationEnabled;
            _tileIcon = tileIcon ?? Assets.ImageIcon;
            _maxFiles = maxFiles;

            _fileWrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _fileScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _fileWrapPanel
            };

            this.Child = _fileScrollViewer;
        }

        public void AddFiles(IEnumerable<string> paths, Action<string>? onFirstFileAdded = null)
        {
            if (_maxFiles == 1)
            {
                var path = paths.FirstOrDefault();
                if (path != null)
                {
                    _fileItems.Clear(); // PDF2Image limit: replace existing file cleanly
                    if (onFirstFileAdded != null)
                    {
                        onFirstFileAdded(path);
                    }
                    _fileItems.Add(new FileItem
                    {
                        FilePath = path,
                        Rotation = RotationSteps.None
                    });
                }
            }
            else
            {
                foreach (var path in paths)
                {
                    if (_fileItems.Count == 0 && onFirstFileAdded != null)
                    {
                        onFirstFileAdded(path);
                    }

                    _fileItems.Add(new FileItem
                    {
                        FilePath = path,
                        Rotation = RotationSteps.None
                    });
                }
            }
            RebuildFileListUI();
        }

        public void Clear()
        {
            _fileItems.Clear();
            RebuildFileListUI();
        }

        public void RebuildFileListUI()
        {
            _fileWrapPanel.Children.Clear();
            foreach (var item in _fileItems)
            {
                var tile = CreateFileTile(item);
                _fileWrapPanel.Children.Add(tile);
            }
        }

        private bool IsFlowBefore(Point pt1, Point pt2, double rowHeight)
        {
            double yDiff = pt1.Y - pt2.Y;
            if (Math.Abs(yDiff) > rowHeight * 0.5)
            {
                return yDiff < 0;
            }
            return pt1.X < pt2.X;
        }

        private Control CreateFileTile(FileItem item)
        {
            var grid = new Grid
            {
                Width = 120,
                Height = 120,
                Margin = new Thickness(5)
            };
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(25)));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(25)));

            var border = new Border
            {
                BorderBrush = _textBrush,
                BorderThickness = new Thickness(2),
                Background = _bgBrush,
                Child = grid,
                Margin = new Thickness(0, 0, 15, 15),
                Cursor = new Cursor(StandardCursorType.SizeAll)
            };

            border.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
                {
                    _draggedItem = item;
                    _dragStartPoint = e.GetPosition(_fileWrapPanel);

                    double initialX = 0;
                    double initialY = 0;
                    if (border.RenderTransform is TranslateTransform tt)
                    {
                        initialX = tt.X;
                        initialY = tt.Y;
                    }
                    _dragStartOffset = new Point(initialX, initialY);

                    border.ZIndex = 100;
                    e.Pointer.Capture(border);
                    e.Handled = true;
                }
            };

            border.PointerMoved += (s, e) =>
            {
                if (_draggedItem != null && _draggedItem == item)
                {
                    var currentPt = e.GetPosition(_fileWrapPanel);
                    var deltaX = currentPt.X - _dragStartPoint.X;
                    var deltaY = currentPt.Y - _dragStartPoint.Y;

                    border.RenderTransform = new TranslateTransform(_dragStartOffset.X + deltaX, _dragStartOffset.Y + deltaY);
                    e.Handled = true;
                }
            };

            border.PointerReleased += (s, e) =>
            {
                if (_draggedItem == item)
                {
                    e.Pointer.Capture(null);

                    var bounds = border.Bounds;
                    double offsetX = 0;
                    double offsetY = 0;
                    if (border.RenderTransform is TranslateTransform releaseTt)
                    {
                        offsetX = releaseTt.X;
                        offsetY = releaseTt.Y;
                    }
                    double currentX = bounds.X + bounds.Width / 2 + offsetX;
                    double currentY = bounds.Y + bounds.Height / 2 + offsetY;
                    var currentCenter = new Point(currentX, currentY);

                    var otherItems = _fileItems.Where(x => x != _draggedItem).ToList();
                    int targetIndex = -1;

                    for (int i = 0; i < otherItems.Count; i++)
                    {
                        var otherItem = otherItems[i];
                        var otherBounds = otherItem.Visual.Bounds;
                        double otherX = otherBounds.X + otherBounds.Width / 2;
                        double otherY = otherBounds.Y + otherBounds.Height / 2;
                        var otherCenter = new Point(otherX, otherY);

                        if (IsFlowBefore(currentCenter, otherCenter, 135.0))
                        {
                            targetIndex = i;
                            break;
                        }
                    }

                    _fileItems.Remove(_draggedItem);
                    if (targetIndex != -1)
                    {
                        _fileItems.Insert(targetIndex, _draggedItem);
                    }
                    else
                    {
                        _fileItems.Add(_draggedItem);
                    }

                    border.RenderTransform = null;
                    _draggedItem = null;

                    RebuildFileListUI();
                    e.Handled = true;
                }
            };

            var btnClose = CtgBox.CreateIconButtonStatic(Assets.CancelIcon, 12, _bgBrush, _textBrush, () =>
            {
                _fileItems.Remove(item);
                RebuildFileListUI();
            });
            btnClose.HorizontalAlignment = HorizontalAlignment.Right;
            btnClose.VerticalAlignment = VerticalAlignment.Center;
            btnClose.Margin = new Thickness(0, 2, 5, 0);
            Grid.SetRow(btnClose, 0);
            grid.Children.Add(btnClose);

            var imageIcon = CtgBox.CreateSvgIconStatic(_tileIcon, 40, _bgBrush, _textBrush);
            imageIcon.HorizontalAlignment = HorizontalAlignment.Center;
            imageIcon.VerticalAlignment = VerticalAlignment.Center;
            imageIcon.RenderTransformOrigin = RelativePoint.Center;
            if (_isRotationEnabled)
            {
                imageIcon.RenderTransform = new RotateTransform((int)item.Rotation * 90);
            }
            Grid.SetRow(imageIcon, 1);
            grid.Children.Add(imageIcon);

            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            string nameOnly = Path.GetFileName(item.FilePath) ?? "";
            if (nameOnly.Length > 8) nameOnly = nameOnly.Substring(0, 6) + "..";

            var txtName = new TextBlock
            {
                Text = nameOnly,
                Foreground = _textBrush,
                FontFamily = _globalFont,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(txtName, 0);
            bottomGrid.Children.Add(txtName);

            if (_isRotationEnabled)
            {
                var btnRotate = CtgBox.CreateIconButtonStatic(Assets.RotateIcon, 15, _bgBrush, _textBrush, () =>
                {
                    item.Rotation = (RotationSteps)(((int)item.Rotation + 1) % 4);
                    if (imageIcon.RenderTransform is RotateTransform rt)
                    {
                        rt.Angle = (int)item.Rotation * 90;
                    }
                    else
                    {
                        imageIcon.RenderTransform = new RotateTransform((int)item.Rotation * 90);
                    }
                });
                btnRotate.Margin = new Thickness(0, 0, 5, 0);
                Grid.SetColumn(btnRotate, 1);
                bottomGrid.Children.Add(btnRotate);
            }

            Grid.SetRow(bottomGrid, 2);
            grid.Children.Add(bottomGrid);

            item.Visual = border;
            return border;
        }
    }

    // =========================================================================
    // REUSABLE CONTROL: DYNAMIC TWO-COLOR DROPDOWN (WITH RESOLVED BINDING BUG)
    // =========================================================================
    public class TwoColorDropdown<T> : StackPanel where T : struct, Enum
    {
        private readonly SolidColorBrush _bg;
        private readonly SolidColorBrush _text;
        private readonly FontFamily _font;
        private readonly Button _btn;
        private readonly Popup _popup;
        private T _selectedValue;

        public T SelectedValue
        {
            get => _selectedValue;
            set
            {
                _selectedValue = value;
                _btn.Content = FormatEnum(value);
                ValueChanged?.Invoke(value);
            }
        }

        public event Action<T>? ValueChanged;

        public TwoColorDropdown(string labelText, T defaultValue, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
        {
            _bg = bg;
            _text = text;
            _font = font;
            _selectedValue = defaultValue;

            this.Spacing = 5;
            this.HorizontalAlignment = HorizontalAlignment.Left;

            var lbl = new TextBlock
            {
                Text = labelText,
                Foreground = _text,
                FontFamily = _font,
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _btn = new Button
            {
                Cursor = new Cursor(StandardCursorType.Hand),
                Height = 35,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                var textBlock = new TextBlock
                {
                    Foreground = _text,
                    FontFamily = _font,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                textBlock.Bind(TextBlock.TextProperty, new Binding
                {
                    Source = control,
                    Path = nameof(Button.Content)
                });

                return new Border
                {
                    BorderBrush = _text,
                    BorderThickness = new Thickness(2),
                    Background = _bg,
                    Padding = new Thickness(15, 5),
                    Child = textBlock
                };
            });

            _btn.Content = FormatEnum(defaultValue);

            _popup = new Popup
            {
                PlacementTarget = _btn,
                Placement = PlacementMode.Bottom,
                IsLightDismissEnabled = true
            };

            var popupContent = new Border
            {
                BorderBrush = _text,
                BorderThickness = new Thickness(2),
                Background = _bg,
                Padding = new Thickness(2)
            };

            var optionsStack = new StackPanel { Spacing = 2 };

            foreach (T val in Enum.GetValues(typeof(T)).Cast<T>())
            {
                var optBtn = new Button
                {
                    Content = FormatEnum(val),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                optBtn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((ctrl, scope) =>
                {
                    var border = new Border
                    {
                        Background = _bg,
                        Padding = new Thickness(15, 8)
                    };

                    var textBlock = new TextBlock
                    {
                        Text = ctrl.Content?.ToString() ?? "",
                        Foreground = _text,
                        FontFamily = _font,
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    border.Child = textBlock;

                    border.PointerEntered += (s, e) =>
                    {
                        border.Background = _text;
                        textBlock.Foreground = _bg;
                    };

                    border.PointerExited += (s, e) =>
                    {
                        border.Background = _bg;
                        textBlock.Foreground = _text;
                    };

                    return border;
                });

                T tempVal = val;
                optBtn.Click += (s, e) =>
                {
                    SelectedValue = tempVal;
                    _popup.IsOpen = false;
                    e.Handled = true;
                };

                optionsStack.Children.Add(optBtn);
            }

            popupContent.Child = optionsStack;
            _popup.Child = popupContent;

            _btn.Click += (s, e) => _popup.IsOpen = !_popup.IsOpen;

            this.Children.Add(lbl);
            this.Children.Add(_btn);
            this.Children.Add(_popup);
        }

        private string FormatEnum(T val)
        {
            string name = val.ToString();
            if (name == "None") return "None";
            if (name == "FitToImage") return "Fit to Image";
            if (name == "Mm10") return "10 mm";
            if (name == "Mm20") return "20 mm";
            if (name == "Mm30") return "30 mm";
            if (name == "FitKeepRatio") return "Fit (Keep Ratio)";
            if (name == "StretchToFill") return "Stretch to Fill";
            if (name == "ActualSize") return "Actual Size";

            if (name.Equals("Png", StringComparison.OrdinalIgnoreCase)) return "PNG";
            if (name.Equals("Jpeg", StringComparison.OrdinalIgnoreCase)) return "JPEG";
            if (name.Equals("Bmp", StringComparison.OrdinalIgnoreCase)) return "BMP";
            if (name.Equals("Tiff", StringComparison.OrdinalIgnoreCase)) return "TIFF";
            if (name.Equals("Tif", StringComparison.OrdinalIgnoreCase)) return "TIF";
            if (name.Equals("Webp", StringComparison.OrdinalIgnoreCase)) return "WEBP";
            if (name.Equals("Ico", StringComparison.OrdinalIgnoreCase)) return "ICO";

            return name;
        }
    }

    // =========================================================================
    // REUSABLE CONTROL: STYLISH TWO-COLOR QUALITY SLIDER
    // =========================================================================
    public class CustomSlider : Grid
    {
        private readonly SolidColorBrush _bg;
        private readonly SolidColorBrush _text;
        private readonly Border _line;
        private readonly Border _thumb;
        private readonly Canvas _canvas;
        private double _value = 80;
        public event Action<double>? ValueChanged;

        public double Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(100, value));
                UpdateThumbPosition();
                ValueChanged?.Invoke(_value);
            }
        }

        public CustomSlider(SolidColorBrush bg, SolidColorBrush text)
        {
            _bg = bg;
            _text = text;
            this.Height = 30;
            this.Width = 150;
            this.Background = Brushes.Transparent;

            _line = new Border
            {
                Height = 3,
                Background = _text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _canvas = new Canvas
            {
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            _thumb = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = _text,
                BorderBrush = _text,
                BorderThickness = new Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            _canvas.Children.Add(_thumb);

            this.Children.Add(_line);
            this.Children.Add(_canvas);

            bool isDragging = false;

            PointerPressed += (s, e) =>
            {
                isDragging = true;
                UpdateValueFromPointer(e.GetPosition(_canvas).X);
                e.Handled = true;
            };

            PointerMoved += (s, e) =>
            {
                if (isDragging)
                {
                    UpdateValueFromPointer(e.GetPosition(_canvas).X);
                    e.Handled = true;
                }
            };

            PointerReleased += (s, e) => { isDragging = false; };
            SizeChanged += (s, e) => UpdateThumbPosition();
        }

        private void UpdateValueFromPointer(double x)
        {
            double width = _canvas.Bounds.Width;
            if (width <= 0) return;
            double pct = x / width;
            Value = pct * 100;
        }

        private void UpdateThumbPosition()
        {
            double width = _canvas.Bounds.Width;
            if (width <= 0) return;
            double x = (Value / 100.0) * width - (_thumb.Width / 2);
            Canvas.SetLeft(_thumb, Math.Max(-_thumb.Width / 2, Math.Min(width - _thumb.Width / 2, x)));
            Canvas.SetTop(_thumb, (_canvas.Bounds.Height - _thumb.Height) / 2);
        }
    }
}