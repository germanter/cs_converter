
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

// Namespace imports to locate all operational parameters and enums
using ImageToPdfApp;
using PdfEngine;
using PdfUtilities;
using Orchestration;
using Glo;
using CentralGateway;

namespace convix
{
    public class CtgBox : Border
    {
        // Internal state logic tracker
        private class ImageItemUI
        {
            public string FilePath { get; set; } = "";
            public RotationSteps Rotation { get; set; } = RotationSteps.None;
            public Control Visual { get; set; } = null!;
        }

        private readonly List<ImageItemUI> _fileItems = new();
        private string? _saveDirectory;
        private string? _outputPdfPath;
        private CancellationTokenSource? _cts;
        private ImageItemUI? _draggedItem;

        // Visual layout tracking states for hardware-accelerated drag-drop
        private Point _dragStartPoint;
        private Point _dragStartOffset;

        private SolidColorBrush bgBrush;
        private SolidColorBrush textBrush;
        private FontFamily globalFont;

        public Border ProgressFill { get; private set; } = null!;
        public TextBlock PercentText { get; private set; } = null!;
        public Button BtnOpen { get; private set; } = null!;
        public TextBlock TxtFail { get; private set; } = null!;
        public TextBox TxtName { get; private set; } = null!;

        // Dynamic settings selectors
        private TwoColorDropdown<PageSizeOption> _pageSizeDropdown = null!;
        private TwoColorDropdown<OrientationOption> _orientationDropdown = null!;
        private TwoColorDropdown<MarginOption> _marginDropdown = null!;
        private TwoColorDropdown<ImageFitOption> _imageFitDropdown = null!;
        private CustomSlider _qualitySlider = null!;

        private WrapPanel _fileWrapPanel = null!;
        private ScrollViewer _fileScrollViewer = null!;

        private readonly double MaxFillWidth = 240.0;

        public CtgBox(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
        {
            this.bgBrush = bg;
            this.textBrush = text;
            this.globalFont = font;

            this.Background = bgBrush;
            this.BorderBrush = textBrush;
            this.BorderThickness = new Thickness(0); // Clean border thickness of 0 to prevent double-thick outline overlap
            this.Padding = new Thickness(15);
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;

            BuildUI(parentWindow);
        }

        // =========================================================================
        // NATIVE FOLDER PICKER LOGIC (OUTPUT BUTTON)
        // =========================================================================
        private DateTime _lastOutputClickTime = DateTime.MinValue;
        private bool _isFolderPickerOpen = false;

        private async Task OpenFolderPickerAsync(Window parentWindow)
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
            catch (Exception)
            {
                // Safely catch
            }
            finally
            {
                _isFolderPickerOpen = false;
            }
        }

        // =========================================================================
        // DYNAMIC PROGRESS HANDLER
        // =========================================================================
        public void SetProgress(int? percentage, bool isFail = false)
        {
            if (isFail)
            {
                ProgressFill.Width = 0;
                PercentText.IsVisible = false;
                BtnOpen.IsVisible = false;
                TxtFail.IsVisible = true; // Display fail state immediately
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
                BtnOpen.IsVisible = true; // Bring back the open button cleanly
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
            foreach (var path in paths)
            {
                // Automatically set the save directory from the first valid path found
                if (string.IsNullOrEmpty(_saveDirectory))
                {
                    _saveDirectory = Path.GetDirectoryName(path) ?? "";
                }

                // Just take the path and shove it right into the list
                _fileItems.Add(new ImageItemUI
                {
                    FilePath = path,
                    Rotation = RotationSteps.None
                });
            }
            
            RebuildFileListUI();
        }

        private void RebuildFileListUI()
        {
            _fileWrapPanel.Children.Clear();
            foreach (var item in _fileItems)
            {
                var tile = CreateFileTile(item);
                _fileWrapPanel.Children.Add(tile);
            }
        }

        private Control CreateFileTile(ImageItemUI item)
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
                BorderBrush = textBrush,
                BorderThickness = new Thickness(2),
                Background = bgBrush,
                Child = grid,
                Margin = new Thickness(0, 0, 15, 15), // Spacing handled here for backward-compatible WrapPanel layout
                Cursor = new Cursor(StandardCursorType.SizeAll)
            };

            // Fluid drag-and-drop mechanics using native pointer moves and swaps without rebuilding trees
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

                    border.ZIndex = 100; // Float safely above other adjacent elements during active dragging
                    e.Pointer.Capture(border); // Capture pointer cleanly to track swap bounds
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

                    // Set GPU-friendly rendering translations without modifying parent children collection live
                    border.RenderTransform = new TranslateTransform(_dragStartOffset.X + deltaX, _dragStartOffset.Y + deltaY);
                    e.Handled = true;
                }
            };

            border.PointerReleased += (s, e) =>
            {
                if (_draggedItem == item)
                {
                    e.Pointer.Capture(null);

                    // Determine where the dragged tile is relative to other tiles
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

                    ImageItemUI? targetItem = null;
                    double minDistance = double.MaxValue;

                    foreach (var otherItem in _fileItems)
                    {
                        if (otherItem == _draggedItem) continue;

                        var otherBounds = otherItem.Visual.Bounds;
                        double otherX = otherBounds.X + otherBounds.Width / 2;
                        double otherY = otherBounds.Y + otherBounds.Height / 2;
                        var otherCenter = new Point(otherX, otherY);

                        double dist = Math.Sqrt(Math.Pow(currentCenter.X - otherCenter.X, 2) + Math.Pow(currentCenter.Y - otherCenter.Y, 2));
                        if (dist < minDistance && dist < 120) // Drag swap detection radius threshold
                        {
                            minDistance = dist;
                            targetItem = otherItem;
                        }
                    }

                    if (targetItem != null)
                    {
                        int idx1 = _fileItems.IndexOf(_draggedItem);
                        int idx2 = _fileItems.IndexOf(targetItem);
                        if (idx1 >= 0 && idx2 >= 0 && idx1 != idx2)
                        {
                            // Pull from old index and insert into target index, shifting others beautifully
                            _fileItems.RemoveAt(idx1);
                            _fileItems.Insert(idx2, _draggedItem);
                        }
                    }

                    border.RenderTransform = null;
                    _draggedItem = null;

                    // Safely rebuild visual tree only once drag session is finished
                    RebuildFileListUI();
                    e.Handled = true;
                }
            };

            // Tile Delete Cross (Top Right)
            var btnClose = CreateIconButton(Assets.CancelIcon, 12, () =>
            {
                _fileItems.Remove(item);
                RebuildFileListUI();
            });
            btnClose.HorizontalAlignment = HorizontalAlignment.Right;
            btnClose.VerticalAlignment = VerticalAlignment.Center;
            btnClose.Margin = new Thickness(0, 2, 5, 0);
            Grid.SetRow(btnClose, 0);
            grid.Children.Add(btnClose);

            // Center image icon (rotatable)
            var imageIcon = CreateSvgIcon(Assets.ImageIcon, 40);
            imageIcon.HorizontalAlignment = HorizontalAlignment.Center;
            imageIcon.VerticalAlignment = VerticalAlignment.Center;
            imageIcon.RenderTransformOrigin = RelativePoint.Center;
            imageIcon.RenderTransform = new RotateTransform((int)item.Rotation * 90);
            Grid.SetRow(imageIcon, 1);
            grid.Children.Add(imageIcon);

            // Bottom bar holding metadata text & rotating click triggers
            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            string nameOnly = Path.GetFileName(item.FilePath) ?? "";
            if (nameOnly.Length > 8) nameOnly = nameOnly.Substring(0, 6) + "..";

            var txtName = new TextBlock
            {
                Text = nameOnly,
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            var btnRotate = CreateIconButton(Assets.RotateIcon, 15, () =>
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

            Grid.SetColumn(txtName, 0);
            Grid.SetColumn(btnRotate, 1);
            bottomGrid.Children.Add(txtName);
            bottomGrid.Children.Add(btnRotate);

            Grid.SetRow(bottomGrid, 2);
            grid.Children.Add(bottomGrid);

            item.Visual = border;
            return border;
        }

        private async void OnStartClicked()
        {
            if (CentralController.isRunning) return;
            if (_fileItems.Count == 0) return;

            _cts = new CancellationTokenSource();
            SetProgress(0);

            string outDir = _saveDirectory ?? Directory.GetCurrentDirectory();
            string filename = TxtName.Text ?? "document";
            if (string.IsNullOrWhiteSpace(filename)) filename = "document";

            var pageSel = _pageSizeDropdown.SelectedValue;
            var orientSel = _orientationDropdown.SelectedValue;
            var marginSel = _marginDropdown.SelectedValue;
            var imageFitSel = _imageFitDropdown.SelectedValue;
            int qualSel = (int)_qualitySlider.Value;

            var imagesToConvert = new List<ImageInput>();
            foreach (var item in _fileItems)
            {
                // Corrected instantiation to use the parameterized constructor of ImageInput
                imagesToConvert.Add(new ImageInput(item.FilePath, item.Rotation));
            }

            try
            {
                var progressReporter = new Progress<double>(val =>
                {
                    int pct = (int)(val <= 1.0 ? val * 100 : val);
                    SetProgress(pct);
                });

                string outputPdf = await CentralController.Image2PdfCallerAsync(
                    images: imagesToConvert,
                    saveDirectory: outDir,
                    filename: filename,
                    pageSize: pageSel,
                    orientation: orientSel,
                    margin: marginSel,
                    imageFit: imageFitSel,
                    quality: qualSel,
                    progress: progressReporter,
                    cancellationToken: _cts.Token
                );

                if (!string.IsNullOrEmpty(outputPdf))
                {
                    _outputPdfPath = outputPdf;
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

        private void BuildUI(Window parentWindow)
        {
            var rootLayout = new Grid();
            rootLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 0: Top bar
            rootLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 1: Line break
            rootLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 2: Settings bar
            rootLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 3: Line break
            rootLayout.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Row 4: Draggable Wrap list

            // ==========================================
            // ROW 0: TOP BAR PANEL
            // ==========================================
            var topBarGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            topBarGrid.ColumnDefinitions.AddRange(new[]
            {
                new ColumnDefinition { Width = GridLength.Auto }, // 0: START
                new ColumnDefinition { Width = GridLength.Auto }, // 1: END
                new ColumnDefinition { Width = GridLength.Auto }, // 2: Progress Outer
                new ColumnDefinition { Width = GridLength.Auto }, // 3: Status Container
                new ColumnDefinition { Width = GridLength.Auto }, // 4: OUTPUT
                new ColumnDefinition { Width = GridLength.Auto }, // 5: clear all
                new ColumnDefinition { Width = GridLength.Auto }, // 6: NAME
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // 7: TxtName
            });

            var btnStart = CreateStrictButton("START");
            btnStart.Click += (s, e) => OnStartClicked();

            var btnEnd = CreateStrictButton("END");
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

            BtnOpen = CreateStrictButton("OPEN");
            BtnOpen.HorizontalAlignment = HorizontalAlignment.Center;
            BtnOpen.IsVisible = false;
            BtnOpen.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_outputPdfPath) && File.Exists(_outputPdfPath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = _outputPdfPath,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            };

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

            var btnOutput = CreateStrictButton("OUTPUT");
            btnOutput.Click += async (s, e) => await OpenFolderPickerAsync(parentWindow);

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
                _fileItems.Clear();
                RebuildFileListUI();
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

            TxtName = CreateStrictTextBox("document");

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
            rootLayout.Children.Add(topBarGrid);

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
            rootLayout.Children.Add(divider1);

            // ==========================================
            // ROW 2: PARAMETERS SETTINGS PANEL
            // ==========================================
            var settingsGrid = new Grid { Margin = new Thickness(0, 5, 0, 10) };
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 25 };

            _pageSizeDropdown = new TwoColorDropdown<PageSizeOption>("PAGE SIZE", PageSizeOption.FitToImage, bgBrush, textBrush, globalFont);
            _orientationDropdown = new TwoColorDropdown<OrientationOption>("ORIENTATION", OrientationOption.Auto, bgBrush, textBrush, globalFont);
            _marginDropdown = new TwoColorDropdown<MarginOption>("MARGIN", MarginOption.None, bgBrush, textBrush, globalFont);
            _imageFitDropdown = new TwoColorDropdown<ImageFitOption>("IMAGE FIT", ImageFitOption.FitKeepRatio, bgBrush, textBrush, globalFont);

            leftStack.Children.Add(_pageSizeDropdown);
            leftStack.Children.Add(_orientationDropdown);
            leftStack.Children.Add(_marginDropdown);
            leftStack.Children.Add(_imageFitDropdown);

            Grid.SetColumn(leftStack, 0);
            settingsGrid.Children.Add(leftStack);

            // Quality Slider Panel (Right-aligned)
            var qualityStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var lblQuality = new TextBlock
            {
                Text = "QUALITY",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            _qualitySlider = new CustomSlider(bgBrush, textBrush);
            _qualitySlider.Value = 80;

            var txtQualityPercent = new TextBlock
            {
                Text = "80%",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 16,
                Width = 45,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            _qualitySlider.ValueChanged += (val) => { txtQualityPercent.Text = $"{(int)val}%"; };

            qualityStack.Children.Add(lblQuality);
            qualityStack.Children.Add(_qualitySlider);
            qualityStack.Children.Add(txtQualityPercent);

            Grid.SetColumn(qualityStack, 2);
            settingsGrid.Children.Add(qualityStack);

            Grid.SetRow(settingsGrid, 2);
            rootLayout.Children.Add(settingsGrid);

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
            rootLayout.Children.Add(divider2);

            // ==========================================
            // ROW 4: FILE COLLECTION PANEL (SCROLLABLE WRAP)
            // ==========================================
            _fileWrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Scrollbars visually hidden to preserve strict two-color visual rules
            _fileScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _fileWrapPanel
            };

            Grid.SetRow(_fileScrollViewer, 4);
            rootLayout.Children.Add(_fileScrollViewer);

            this.Child = rootLayout;
        }

        // ==========================================
        // UI GRAPHICS HELPERS
        // ==========================================
        private Button CreateIconButton(Assets.IconData icon, double size, Action onClick)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand) };
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border
                {
                    Background = Brushes.Transparent,
                    Child = CreateSvgIcon(icon, size)
                };
            });
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Avalonia.Controls.Shapes.Path CreateSvgIcon(Assets.IconData icon, double size)
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

        private Button CreateStrictButton(string text)
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

        private TextBox CreateStrictTextBox(string defaultText)
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

    // ==========================================
    // REUSABLE CONTROL: DYNAMIC TWO-COLOR DROPDOWN (FLAWLESS THEME SAFETY)
    // ==========================================
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
                return new Border
                {
                    BorderBrush = _text,
                    BorderThickness = new Thickness(2),
                    Background = _bg,
                    Padding = new Thickness(15, 5),
                    Child = new TextBlock
                    {
                        Text = control.Content?.ToString() ?? "",
                        Foreground = _text,
                        FontFamily = _font,
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
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

                    // Hover inversion logic keeping exactly within the 2-color rule
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
                    e.Handled = true; // Stop bubbling to prevent lingering parameters
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
            return name;
        }
    }

    // ==========================================
    // REUSABLE CONTROL: STYLISH TWO-COLOR QUALITY SLIDER (NO GRAY GRADIENTS)
    // ==========================================
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