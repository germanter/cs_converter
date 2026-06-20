
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace convix
{
    public class CtgBox : Border
    {
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
                    // var selectedPath = folders[0].Path.LocalPath;
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

        private SolidColorBrush bgBrush;
        private SolidColorBrush textBrush;
        private FontFamily globalFont;

        public Border ProgressFill { get; private set; } = null!;
        public TextBlock PercentText { get; private set; } = null!;
        public Button BtnOpen { get; private set; } = null!;
        public TextBlock TxtFail { get; private set; } = null!;
        public TextBox TxtName { get; private set; } = null!;

        // The exact max width in pixels the inner block can grow to
        private readonly double MaxFillWidth = 240.0;

        public CtgBox(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
        {
            this.bgBrush = bg;
            this.textBrush = text;
            this.globalFont = font;

            this.Background = bgBrush;
            this.BorderBrush = textBrush;
            this.BorderThickness = new Thickness(0, 0, 0, 4);
            this.Padding = new Thickness(15);
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Top;

            BuildUI(parentWindow);
        }

        // =========================================================================
        // DYNAMIC PROGRESS HANDLER
        // =========================================================================
        public void SetProgress(int? percentage, bool isFail = false)
        {
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

            if (isFail)
            {
                PercentText.IsVisible = false;
                BtnOpen.IsVisible = false;
                TxtFail.IsVisible = true;
            }
            else if (p >= 100)
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

        private void BuildUI(Window parentWindow)
        {
            var mainStack = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            // Version-agnostic column setup
            mainStack.ColumnDefinitions.AddRange(new[]
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
            var btnEnd = CreateStrictButton("END");

            // ==========================================
            // PROGRESS BAR
            // ==========================================
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

            // STATUS CONTAINER
            var statusContainer = new Grid
            {
                Width = 90
            };

            PercentText = new TextBlock
            {
                Text = "",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            BtnOpen = CreateStrictButton("OPEN");
            BtnOpen.HorizontalAlignment = HorizontalAlignment.Center;
            BtnOpen.IsVisible = false;

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

            // ==========================================
            // NAME INPUT PANEL
            // ==========================================
            var lblName = new TextBlock
            {
                Text = "NAME",
                Foreground = textBrush,
                FontFamily = globalFont,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };

            TxtName = CreateStrictTextBox("document");

            // Map layout elements to designated columns
            Grid.SetColumn(btnStart, 0);
            Grid.SetColumn(btnEnd, 1);
            Grid.SetColumn(progressBoxOuter, 2);
            Grid.SetColumn(statusContainer, 3);
            Grid.SetColumn(btnOutput, 4);
            Grid.SetColumn(btnClearAll, 5);
            Grid.SetColumn(lblName, 6);
            Grid.SetColumn(TxtName, 7);

            // Apply 15px right spacing between adjacent columns via Margins
            btnStart.Margin = new Thickness(0, 0, 15, 0);
            btnEnd.Margin = new Thickness(0, 0, 15, 0);
            progressBoxOuter.Margin = new Thickness(0, 0, 15, 0);
            statusContainer.Margin = new Thickness(0, 0, 15, 0);
            btnOutput.Margin = new Thickness(0, 0, 15, 0);
            btnClearAll.Margin = new Thickness(0, 0, 15, 0);
            lblName.Margin = new Thickness(0, 0, 15, 0);

            // Set uniform vertical alignments
            btnStart.VerticalAlignment = VerticalAlignment.Center;
            btnEnd.VerticalAlignment = VerticalAlignment.Center;
            progressBoxOuter.VerticalAlignment = VerticalAlignment.Center;
            statusContainer.VerticalAlignment = VerticalAlignment.Center;
            btnOutput.VerticalAlignment = VerticalAlignment.Center;

            // Assemble children inside Grid layout
            mainStack.Children.Add(btnStart);
            mainStack.Children.Add(btnEnd);
            mainStack.Children.Add(progressBoxOuter);
            mainStack.Children.Add(statusContainer);
            mainStack.Children.Add(btnOutput);
            mainStack.Children.Add(btnClearAll);
            mainStack.Children.Add(lblName);
            mainStack.Children.Add(TxtName);

            this.Child = mainStack;
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

            // Force internal border properties to stay exact on pointer over
            var stylePointerOver = new Style(s => s.OfType<TextBox>().Class(":pointerover").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush)
                }
            };

            // Force internal border properties to stay exact on focus
            var styleFocus = new Style(s => s.OfType<TextBox>().Class(":focus").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush)
                }
            };

            // Force internal border properties to stay exact on focus-within
            var styleFocusWithin = new Style(s => s.OfType<TextBox>().Class(":focus-within").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, bgBrush),
                    new Setter(Border.BorderBrushProperty, textBrush)
                }
            };

            // Baseline border properties fallback
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
}