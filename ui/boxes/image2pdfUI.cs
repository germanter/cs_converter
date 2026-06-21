using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Platform.Storage;

using ImageToPdfApp;
using PdfEngine;
using PdfUtilities;
using Orchestration;
using Glo;
using CentralGateway;

namespace convix
{
    public class Image2PdfUI : Grid, ICtgToolUI
    {
        private readonly SolidColorBrush bgBrush;
        private readonly SolidColorBrush textBrush;
        private readonly FontFamily globalFont;
        private readonly Window _parentWindow;

        private readonly FileCollectionPanel _fileCollectionPanel;
        private string? _saveDirectory;
        private string? _outputPdfPath;
        private CancellationTokenSource? _cts;
        private readonly double MaxFillWidth = 240.0;

        public Border ProgressFill { get; private set; } = null!;
        public TextBlock PercentText { get; private set; } = null!;
        public Button BtnOpen { get; private set; } = null!;
        public TextBlock TxtFail { get; private set; } = null!;
        public TextBox TxtName { get; private set; } = null!;

        private TwoColorDropdown<PageSizeOption> _pageSizeDropdown = null!;
        private TwoColorDropdown<OrientationOption> _orientationDropdown = null!;
        private TwoColorDropdown<MarginOption> _marginDropdown = null!;
        private TwoColorDropdown<ImageFitOption> _imageFitDropdown = null!;
        private CustomSlider _qualitySlider = null!;

        public Image2PdfUI(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
        {
            this.bgBrush = bg;
            this.textBrush = text;
            this.globalFont = font;
            this._parentWindow = parentWindow;

            // Instantiates reusable panel and allows rotation specifically for Image to PDF converting
            _fileCollectionPanel = new FileCollectionPanel(bg, text, font, isRotationEnabled: true);

            BuildUI();
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
            _fileCollectionPanel.AddFiles(paths, path =>
            {
                if (string.IsNullOrEmpty(_saveDirectory))
                {
                    _saveDirectory = Path.GetDirectoryName(path) ?? "";
                }
            });
        }

        private async void OnStartClicked()
        {
            if (CentralController.isRunning) return;
            if (_fileCollectionPanel.FileItems.Count == 0) return;

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
            foreach (var item in _fileCollectionPanel.FileItems)
            {
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

        private void BuildUI()
        {
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 0: Top bar
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 1: Upper Divider
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 2: Settings bar
            this.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 3: Lower Divider
            this.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Row 4: Draggable Collection panel

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

            var btnOutput = CtgBox.CreateStrictButtonStatic("OUTPUT", bgBrush, textBrush, globalFont);
            btnOutput.Click += async (s, e) => await OpenFolderPickerAsync(_parentWindow);

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
                _fileCollectionPanel.Clear();
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

            TxtName = CtgBox.CreateStrictTextBoxStatic("document", bgBrush, textBrush, globalFont);

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
            this.Children.Add(settingsGrid);

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
            Grid.SetRow(_fileCollectionPanel, 4);
            this.Children.Add(_fileCollectionPanel);
        }
    }
}