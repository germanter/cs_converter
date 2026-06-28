
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Glo;
using WriterHead;

namespace convix
{
    public class SettingsPanel : Border
    {
        private Grid _mainView;
        private Grid _colorPickerModal;
        
        private TextBlock _txtBgColor;
        private TextBlock _txtTextColor;
        private TextBlock _txtLogs;
        
        private Border _previewColorBox;
        
        // True 2D HSV State
        private double _currentHue = 0;        // 0 to 360
        private double _currentSaturation = 1; // 0 to 1
        private double _currentValue = 1;      // 0 to 1
        
        private Border _vBaseLayer; // Grader base color layer
        
        private CustomColorSlider _sliderR;
        private CustomColorSlider _sliderG;
        private CustomColorSlider _sliderB;
        private bool _isUpdatingColor = false;
        
        private SolidColorBrush _bgBrush;
        private SolidColorBrush _textBrush;
        
        private string _targetColorKey = "";
        private string _selectedHex = "";
        private bool _isCooldown = false;

        public SettingsPanel(SolidColorBrush bgBrush, SolidColorBrush textBrush, FontFamily font)
        {
            _bgBrush = bgBrush;
            _textBrush = textBrush;
            
            this.Background = _bgBrush;
            this.IsVisible = false;
            this.ZIndex = 100;
            
            var rootGrid = new Grid { Margin = new Thickness(25) };

            // ==========================================
            // MAIN SETTINGS VIEW
            // ==========================================
            _mainView = new Grid { RowDefinitions = new RowDefinitions("60, *") }; // Configured for 60px header height
            
            var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *") }; // Columns configured like MainWindow
            var title = new TextBlock { 
                Text = "settings", 
                FontSize = 40.0, 
                FontFamily = font, 
                Foreground = _textBrush,
                VerticalAlignment = VerticalAlignment.Top, // Vertically aligned to match the top
                Margin = new Thickness(0, -20, 0, 0) // Matching title top offset
            };
            
            // Container for top-right aligned actions
            var rightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Spacing = 30.0
            };

            var btnClose = new Button { 
                Cursor = new Cursor(StandardCursorType.Hand),
                VerticalAlignment = VerticalAlignment.Top
            };
            btnClose.Click += (s, e) => this.IsVisible = false;
            btnClose.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((c, s) => 
                new TextBlock { 
                    Text = "X", 
                    FontSize = 32.0, // Standardized font size for top close buttons
                    FontFamily = font, 
                    Foreground = _textBrush, 
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Top
                });
            
            rightPanel.Children.Add(btnClose);

            Grid.SetColumn(title, 0);
            Grid.SetColumn(rightPanel, 1);
            headerGrid.Children.Add(title);
            headerGrid.Children.Add(rightPanel);
            
            var optionsStack = new StackPanel { Spacing = 10, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            _txtTextColor = new TextBlock { FontSize = 30, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };
            _txtBgColor = new TextBlock { FontSize = 30, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };
            _txtLogs = new TextBlock { FontSize = 30, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };
            var txtRestore = new TextBlock { Text = "restore defaults", FontSize = 30, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };

            optionsStack.Children.Add(CreateOptionButton(_txtTextColor, () => OpenColorPicker("text")));
            optionsStack.Children.Add(CreateOptionButton(_txtBgColor, () => OpenColorPicker("bg")));
            optionsStack.Children.Add(CreateOptionButton(_txtLogs, async () => await ToggleLogs()));
            optionsStack.Children.Add(CreateOptionButton(txtRestore, async () => await RestoreDefaults()));

            // ==========================================
            // NEW: DESTROY CORES OPTION
            // ==========================================
            var txtDestroy = new TextBlock { Text = "destroy convix files", FontSize = 30, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };
            var descDestroy = new TextBlock { Text = "this will destroy all app files created by convix", FontSize = 16, FontFamily = font, Foreground = _textBrush, Margin = new Thickness(0, 5, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            
            var destroyStack = new StackPanel { Spacing = 5, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var btnDestroy = CreateOptionButton(txtDestroy, async () => await ExecuteDestruction(txtDestroy));
            destroyStack.Children.Add(btnDestroy);
            destroyStack.Children.Add(descDestroy);
            optionsStack.Children.Add(destroyStack);

            Grid.SetRow(headerGrid, 0);
            Grid.SetRow(optionsStack, 1);
            _mainView.Children.Add(headerGrid);
            _mainView.Children.Add(optionsStack);

            // ==========================================
            // TRUE 2D HSV COLOR PICKER MODAL
            // ==========================================
            _colorPickerModal = new Grid { IsVisible = false };
            
            var clickBlocker = new Border { Background = Brushes.Transparent };
            clickBlocker.PointerPressed += (s, e) => e.Handled = true; 
            _colorPickerModal.Children.Add(clickBlocker);

            var modalPanel = new Border {
                Background = _bgBrush,
                BorderBrush = _textBrush,
                BorderThickness = new Thickness(4),
                Padding = new Thickness(30),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var pickerStack = new StackPanel { Spacing = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var pickerTitle = new TextBlock { Text = "select color", FontSize = 30, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center };
            _previewColorBox = new Border { Width = 80, Height = 40, BorderThickness = new Thickness(2), BorderBrush = _textBrush, CornerRadius = new CornerRadius(4) };
            headerStack.Children.Add(pickerTitle);
            headerStack.Children.Add(_previewColorBox);
            pickerStack.Children.Add(headerStack);

            // --- THE SWAPPED TRUE 2D PALETTE AND GRADER ---
            var paletteAndHueStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 15, HorizontalAlignment = HorizontalAlignment.Center };

            // 1. The Large Main Color Picker (Hue / Saturation)
            var hsGrid = new Grid { Height = 180, Width = 300, Cursor = new Cursor(StandardCursorType.Cross) };
            var hsBorder = new Border { BorderBrush = _textBrush, BorderThickness = new Thickness(2), Child = hsGrid };

            var rainbowHorizontalBrush = new LinearGradientBrush {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops {
                    new GradientStop(Color.Parse("#FF0000"), 0.000), new GradientStop(Color.Parse("#FFFF00"), 0.167),
                    new GradientStop(Color.Parse("#00FF00"), 0.333), new GradientStop(Color.Parse("#00FFFF"), 0.500),
                    new GradientStop(Color.Parse("#0000FF"), 0.667), new GradientStop(Color.Parse("#FF00FF"), 0.833),
                    new GradientStop(Color.Parse("#FF0000"), 1.000)
                }
            };
            var hsBaseLayer = new Border { Background = rainbowHorizontalBrush };

            // Fades to White at the bottom (Saturation)
            var satLayer = new Border { 
                Background = new LinearGradientBrush {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops { new GradientStop(Colors.Transparent, 0.0), new GradientStop(Colors.White, 1.0) }
                }
            };

            hsGrid.Children.Add(hsBaseLayer);
            hsGrid.Children.Add(satLayer);
            hsGrid.PointerPressed += HandleHSInteraction;
            hsGrid.PointerMoved += (s, e) => { if (e.GetCurrentPoint(hsGrid).Properties.IsLeftButtonPressed) HandleHSInteraction(s, e); };
            
            paletteAndHueStack.Children.Add(hsBorder);

            // 2. The Small Value Grader (Light to Dark)
            var vGrid = new Grid { Height = 180, Width = 25, Cursor = new Cursor(StandardCursorType.Cross) };
            var vBorder = new Border { BorderBrush = _textBrush, BorderThickness = new Thickness(2), Child = vGrid };
            
            _vBaseLayer = new Border { Background = new SolidColorBrush(Colors.Red) };
            
            // Fades to Black at the bottom (Value)
            var valLayer = new Border { 
                Background = new LinearGradientBrush {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops { new GradientStop(Colors.Transparent, 0.0), new GradientStop(Colors.Black, 1.0) }
                }
            };

            vGrid.Children.Add(_vBaseLayer);
            vGrid.Children.Add(valLayer);
            vGrid.PointerPressed += HandleVInteraction;
            vGrid.PointerMoved += (s, e) => { if (e.GetCurrentPoint(vGrid).Properties.IsLeftButtonPressed) HandleVInteraction(s, e); };
            
            paletteAndHueStack.Children.Add(vBorder);
            pickerStack.Children.Add(paletteAndHueStack);

            rootGrid.Children.Add(_mainView);        
            rootGrid.Children.Add(_colorPickerModal); 

            // --- RGB SLIDERS (For exact math control) ---
            var slidersGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), ColumnDefinitions = new ColumnDefinitions("Auto,*"), Width = 340 };

            var lblR = new TextBlock { Text = "R", FontSize = 20, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,15,0) };
            _sliderR = new CustomColorSlider(_textBrush, 280); _sliderR.ValueChanged += OnRgbChanged;
            Grid.SetRow(lblR, 0); Grid.SetColumn(lblR, 0); Grid.SetRow(_sliderR, 0); Grid.SetColumn(_sliderR, 1);

            var lblG = new TextBlock { Text = "G", FontSize = 20, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,15,0) };
            _sliderG = new CustomColorSlider(_textBrush, 280); _sliderG.ValueChanged += OnRgbChanged;
            Grid.SetRow(lblG, 1); Grid.SetColumn(lblG, 0); Grid.SetRow(_sliderG, 1); Grid.SetColumn(_sliderG, 1);

            var lblB = new TextBlock { Text = "B", FontSize = 20, FontFamily = font, Foreground = _textBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,15,0) };
            _sliderB = new CustomColorSlider(_textBrush, 280); _sliderB.ValueChanged += OnRgbChanged;
            Grid.SetRow(lblB, 2); Grid.SetColumn(lblB, 0); Grid.SetRow(_sliderB, 2); Grid.SetColumn(_sliderB, 1);

            slidersGrid.Children.Add(lblR); slidersGrid.Children.Add(_sliderR);
            slidersGrid.Children.Add(lblG); slidersGrid.Children.Add(_sliderG);
            slidersGrid.Children.Add(lblB); slidersGrid.Children.Add(_sliderB);

            pickerStack.Children.Add(slidersGrid);

            // --- ACTIONS ---
            var actionStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 20, Margin = new Thickness(0,10,0,0) };
            var btnApply = CreateSimpleButton("Apply", async () => await ApplyColor(), font);
            var btnCancelPicker = CreateSimpleButton("Cancel", () => { _colorPickerModal.IsVisible = false; }, font);
            
            actionStack.Children.Add(btnApply);
            actionStack.Children.Add(btnCancelPicker);
            pickerStack.Children.Add(actionStack);
            
            modalPanel.Child = pickerStack;
            _colorPickerModal.Children.Add(modalPanel);

            this.Child = rootGrid;
        }

        // ==========================================
        // UI HELPERS
        // ==========================================
        public void RefreshUI()
        {
            _txtBgColor.Text = $"background color : {Vars.BGcolor}";
            _txtTextColor.Text = $"text color : {Vars.TEXTcolor}";
            _txtLogs.Text = $"save logs  : {Vars.openLog.ToString().ToLower()}";
        }

        private Button CreateOptionButton(TextBlock contentBlock, Action onClick)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand) };
            btn.Click += (s, e) => onClick();
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border { BorderBrush = _textBrush, BorderThickness = new Thickness(4), Background = Brushes.Transparent, Padding = new Thickness(15, 5), MinWidth = 450, Child = contentBlock };
            });
            return btn;
        }

        private Button CreateSimpleButton(string text, Action onClick, FontFamily font)
        {
            var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand) };
            btn.Click += (s, e) => onClick();
            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new Border {
                    BorderBrush = _textBrush, BorderThickness = new Thickness(4), Background = Brushes.Transparent, Padding = new Thickness(15, 5), MinWidth = 120,
                    Child = new TextBlock { Text = text, FontSize = 24, FontFamily = font, Foreground = _textBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                };
            });
            return btn;
        }

        private async Task ToggleLogs()
        {
            if (_isCooldown) return;
            _isCooldown = true;
            try {
                Vars.openLog = !Vars.openLog;
                await Writer.Mode3_UpdateSettingAsync("openLog", Vars.openLog);
                RefreshUI();
            } catch { } finally { await Task.Delay(500); _isCooldown = false; }
        }

        private async Task RestoreDefaults()
        {
            if (_isCooldown) return;
            _isCooldown = true;
            try {
                await Writer.Mode4_NukeSysAsync();
                Vars.BGcolor = Vars.baseBGcolor;
                Vars.TEXTcolor = Vars.baseTEXTcolor;
                Vars.openLog = true;
                Vars.NotifyThemeChanged();
                RefreshUI();
            } catch { } finally { await Task.Delay(500); _isCooldown = false; }
        }

        // ==========================================
        // DESTRUCTION EXECUTION ENGINE
        // ==========================================
        private async Task ExecuteDestruction(TextBlock txtDestroy)
        {
            if (_isCooldown) return;
            _isCooldown = true;

            // Globally disable interaction for this entire settings control hierarchy
            // UI thread remains fluid/unfrozen, but ignore pointer inputs on close and other buttons
            this.IsHitTestVisible = false;
            txtDestroy.Text = "destroying...";

            try
            {
                bool success = await Destruction.Destructor.Destroy();
                if (!success)
                {
                    txtDestroy.Text = "fail";
                }
            }
            catch
            {
                txtDestroy.Text = "fail";
            }
            finally
            {
                // Restore interaction state if destruction is aborted or unsuccessful
                this.IsHitTestVisible = true;
                _isCooldown = false;
            }
        }

        // ==========================================
        // TRUE 2D COLOR ENGINE LOGIC
        // ==========================================
        private void OpenColorPicker(string targetKey)
        {
            _targetColorKey = targetKey;
            _selectedHex = targetKey == "bg" ? Vars.BGcolor : Vars.TEXTcolor;

            if (Color.TryParse(_selectedHex, out Color c))
            {
                _isUpdatingColor = true;
                _sliderR.Value = c.R; _sliderG.Value = c.G; _sliderB.Value = c.B;
                ColorToHSV(c, out _currentHue, out _currentSaturation, out _currentValue);
                UpdateBaseVColor();
                _previewColorBox.Background = new SolidColorBrush(c);
                _isUpdatingColor = false;
            }
            
            _colorPickerModal.IsVisible = true;
        }

        private async Task ApplyColor()
        {
            if (_isCooldown) return;
            _isCooldown = true;
            try {
                if (_targetColorKey == "bg") {
                    Vars.BGcolor = _selectedHex;
                    await Writer.Mode3_UpdateSettingAsync("bg", _selectedHex);
                } else {
                    Vars.TEXTcolor = _selectedHex;
                    await Writer.Mode3_UpdateSettingAsync("text", _selectedHex);
                }
                
                Vars.NotifyThemeChanged();
                RefreshUI();
                _colorPickerModal.IsVisible = false;
            } catch { } finally { await Task.Delay(500); _isCooldown = false; }
        }

        // Handles dragging inside the Large Box (Hue & Saturation)
        private void HandleHSInteraction(object? sender, PointerEventArgs e)
        {
            var element = sender as Grid;
            if (element == null) return;
            var pos = e.GetPosition(element);
            
            if (element.Bounds.Width == 0 || element.Bounds.Height == 0) return;

            _currentHue = Math.Clamp(pos.X / element.Bounds.Width, 0, 1) * 360.0;
            _currentSaturation = 1.0 - Math.Clamp(pos.Y / element.Bounds.Height, 0, 1);
            
            UpdateBaseVColor();
            UpdateFromHSV();
        }

        // Handles dragging the Small Grader Bar (Value)
        private void HandleVInteraction(object? sender, PointerEventArgs e)
        {
            var element = sender as Grid;
            if (element == null) return;
            var pos = e.GetPosition(element);
            
            if (element.Bounds.Height == 0) return;

            _currentValue = 1.0 - Math.Clamp(pos.Y / element.Bounds.Height, 0, 1);
            UpdateFromHSV();
        }

        private void UpdateBaseVColor()
        {
            var baseColor = ColorFromHSV(_currentHue, _currentSaturation, 1.0);
            _vBaseLayer.Background = new SolidColorBrush(baseColor);
        }

        private void UpdateFromHSV()
        {
            _isUpdatingColor = true;
            var color = ColorFromHSV(_currentHue, _currentSaturation, _currentValue);
            
            _sliderR.Value = color.R;
            _sliderG.Value = color.G;
            _sliderB.Value = color.B;
            
            _selectedHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            _previewColorBox.Background = new SolidColorBrush(color);
            _isUpdatingColor = false;
        }

        private void OnRgbChanged(double value)
        {
            if (_isUpdatingColor) return;
            
            _isUpdatingColor = true;
            byte r = (byte)_sliderR.Value; byte g = (byte)_sliderG.Value; byte b = (byte)_sliderB.Value;
            var color = Color.FromRgb(r, g, b);
            
            ColorToHSV(color, out _currentHue, out _currentSaturation, out _currentValue);
            UpdateBaseVColor();
            
            _selectedHex = $"#{r:X2}{g:X2}{b:X2}";
            _previewColorBox.Background = new SolidColorBrush(color);
            _isUpdatingColor = false;
        }

        // --- MATH: HSV to RGB ---
        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = value - c;

            double r = 0, g = 0, b = 0;
            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        // --- MATH: RGB to HSV ---
        private void ColorToHSV(Color color, out double h, out double s, out double v)
        {
            double r = color.R / 255.0; double g = color.G / 255.0; double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;

            if (delta == 0) h = 0;
            else if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * (((b - r) / delta) + 2);
            else h = 60 * (((r - g) / delta) + 4);

            if (h < 0) h += 360;
        }
    }

    public class CustomColorSlider : Border
    {
        private Border _fill;
        public event Action<double>? ValueChanged;
        private double _val = 0;

        public CustomColorSlider(SolidColorBrush textBrush, double width)
        {
            this.Height = 25;
            this.Width = width;
            this.BorderBrush = textBrush;
            this.BorderThickness = new Thickness(2);
            this.Background = Brushes.Transparent;
            this.Cursor = new Cursor(StandardCursorType.Hand);
            this.Margin = new Thickness(0, 5);

            _fill = new Border { HorizontalAlignment = HorizontalAlignment.Left, Background = textBrush, Width = 0 };
            this.Child = _fill;

            this.PointerPressed += OnPointerEvent;
            this.PointerMoved += (s, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) OnPointerEvent(s, e); };
        }

        public double Value
        {
            get => _val;
            set { _val = Math.Clamp(value, 0, 255); UpdateFill(); }
        }

        private void UpdateFill()
        {
            if (this.Bounds.Width > 0)
                _fill.Width = (_val / 255.0) * this.Bounds.Width;
            else
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { _fill.Width = (_val / 255.0) * this.Bounds.Width; });
        }

        private void OnPointerEvent(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(this);
            double w = this.Bounds.Width;
            if (w == 0) return;
            
            double ratio = Math.Clamp(pos.X / w, 0, 1);
            this.Value = ratio * 255.0;
            ValueChanged?.Invoke(this.Value);
        }
    }
}