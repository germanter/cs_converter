
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Layout;
// using Avalonia.Media;
// using Avalonia.Input;
// using Avalonia.Threading;
// using Glo;

// namespace convix
// {
//     public class MainWindow : Window
//     {
//         // ==========================================
//         // UI SETTINGS
//         // ==========================================
//         private readonly double CategorySpacing = 10.0; 
//         private readonly double WindowPadding = 25.0; 
//         private readonly double ElementGap = 15.0; 

//         private readonly double UIBorderThickness = 4.0;
//         private readonly double UIInnerBorderThickness = 2.0;
//         private readonly double LineThickness = 4.0;
//         private readonly double CatStrokeThickness = 2.0;

//         private readonly double LeftMenuWidth = 220.0;
//         private readonly double TasklistWidth = 280.0;
//         private readonly double HeaderHeight = 60.0;
//         private readonly double DropBoxHeight = 110.0;

//         private readonly double TitleFontSize = 40.0;
//         private readonly double MenuFontSize = 24.0;
//         private readonly double DropFontSize = 30.0;
//         private readonly double TasklistTitleFontSize = 24.0;
//         private readonly double TasklistTextFontSize = 14.0;
        
//         private readonly double TopIconSize = 32.0;
//         private readonly double TaskIconSize = 20.0;
//         private readonly double CatSize = 80.0;
//         private readonly double DropLeftPadding = 30.0;

//         // ==========================================
//         // STATE TRACKERS
//         // ==========================================
//         public string activeCTG = "Image2PDF";
//         private Dictionary<string, Avalonia.Controls.Shapes.Ellipse> categoryPointers = new();
        
//         private SolidColorBrush bgBrush;
//         private SolidColorBrush textBrush;
//         private FontFamily globalFont;

//         private Canvas? eyesContainer;
//         private Avalonia.Controls.Shapes.Path? eye1, eye2, eye3, eye4;
//         private DispatcherTimer? blinkTimer;
//         private DispatcherTimer? moodTimer;
//         private string[] moods = { "blip", "rest", "furious" };
//         private int currentMoodIndex = 0;

//         private TaskListUI? _taskListEngine;
//         private SettingsPanel? _settingsPanel; 
//         private HistoryPanel? _historyPanel; // NEW: History panel hook

//         public MainWindow()
//         {
//             bgBrush = new SolidColorBrush(Color.Parse(Vars.BGcolor));
//             textBrush = new SolidColorBrush(Color.Parse(Vars.TEXTcolor));

//             var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "convix";
//             globalFont = new FontFamily($"avares://{assemblyName}/s_assets#Nunito");

//             this.Background = bgBrush;
//             this.Foreground = textBrush;
//             this.FontFamily = globalFont;
            
//             this.Title = "convix";
//             this.Width = 1100;
//             this.Height = 650;
//             this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

//             BuildUI();

//             Vars.OnSnapshotChanged += OnSnapshotChanged;
            
//             Vars.OnThemeChanged += () => {
//                 Dispatcher.UIThread.Post(() => {
//                     try {
//                         bgBrush.Color = Color.Parse(Vars.BGcolor);
//                         textBrush.Color = Color.Parse(Vars.TEXTcolor);
//                     } catch { }
//                 });
//             };

//             _taskListEngine?.Refresh(activeCTG, Vars.jsonSnapshot);
//         }

//         protected override void OnClosed(EventArgs e)
//         {
//             Vars.OnSnapshotChanged -= OnSnapshotChanged;
//             base.OnClosed(e);
//         }

//         private void OnSnapshotChanged(string newSnapshot)
//         {
//             _taskListEngine?.Refresh(activeCTG, newSnapshot);
//         }

//         private void BuildUI()
//         {
//             var mainContainer = new Grid(); 

//             var rootGrid = new Grid
//             {
//                 Margin = new Thickness(WindowPadding),
//                 RowDefinitions = new RowDefinitions($"{HeaderHeight}, *"),
//                 ColumnDefinitions = new ColumnDefinitions($"{LeftMenuWidth}, *")
//             };

//             var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *") };
//             Grid.SetRow(headerGrid, 0);
//             Grid.SetColumnSpan(headerGrid, 2);
//             rootGrid.Children.Add(headerGrid);

//             var title = new TextBlock
//             {
//                 Text = "convix",
//                 FontSize = TitleFontSize,
//                 Foreground = textBrush,
//                 VerticalAlignment = VerticalAlignment.Top,
//                 Margin = new Thickness(0, -20, 0, 0)
//             };
//             Grid.SetColumn(title, 0);
//             headerGrid.Children.Add(title);

//             var iconsPanel = new StackPanel
//             {
//                 Orientation = Orientation.Horizontal,
//                 HorizontalAlignment = HorizontalAlignment.Right,
//                 VerticalAlignment = VerticalAlignment.Top,
//                 Spacing = 30.0
//             };
            
//             var btnHistory = CreateTopIconButton(Assets.HistoryIcon, TopIconSize);
//             var btnSettings = CreateTopIconButton(Assets.SettingsIcon, TopIconSize);
            
//             btnSettings.Click += (s, e) => {
//                 if (_settingsPanel != null) {
//                     _settingsPanel.RefreshUI();
//                     _settingsPanel.IsVisible = true;
//                 }
//             };

//             // NEW: History button trigger
//             btnHistory.Click += (s, e) => {
//                 if (_historyPanel != null) {
//                     _historyPanel.RefreshUI();
//                     _historyPanel.IsVisible = true;
//                 }
//             };

//             iconsPanel.Children.Add(btnHistory);
//             iconsPanel.Children.Add(btnSettings);
            
//             Grid.SetColumn(iconsPanel, 1);
//             headerGrid.Children.Add(iconsPanel);

//             var leftPanel = new StackPanel { Margin = new Thickness(0, ElementGap, ElementGap, 0) };
//             string[] categories = { "Image2PDF", "ImageConverter", "Office2PDF", "PDF2Image", "PDFMerger" };
            
//             leftPanel.Children.Add(CreateLine());
            
//             foreach (var ctg in categories)
//             {
//                 var categoryContainer = new Grid
//                 {
//                     ColumnDefinitions = new ColumnDefinitions("Auto, Auto"),
//                     Margin = new Thickness(0, CategorySpacing) 
//                 };

//                 var btn = new Button
//                 {
//                     Content = "|_" + ctg, 
//                     Cursor = new Cursor(StandardCursorType.Hand),
//                     HorizontalAlignment = HorizontalAlignment.Left,
//                     HorizontalContentAlignment = HorizontalAlignment.Left
//                 };

//                 btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
//                 {
//                     return new TextBlock
//                     {
//                         Text = control.Content?.ToString() ?? "", 
//                         FontSize = MenuFontSize,
//                         Foreground = textBrush,
//                         Background = Brushes.Transparent,
//                         VerticalAlignment = VerticalAlignment.Center
//                     };
//                 });

//                 var dotPointer = new Avalonia.Controls.Shapes.Ellipse
//                 {
//                     Width = 10,
//                     Height = 10,
//                     Fill = textBrush,
//                     IsVisible = (activeCTG == ctg),
//                     Margin = new Thickness(10, 0, 0, 0), 
//                     VerticalAlignment = VerticalAlignment.Center
//                 };
//                 categoryPointers[ctg] = dotPointer;

//                 btn.Click += (s, e) => { 
//                     activeCTG = ctg; 
//                     UpdateCategoryPointers();
//                     _taskListEngine?.Refresh(activeCTG, Vars.jsonSnapshot); 
//                 };
                
//                 Grid.SetColumn(btn, 0);         
//                 Grid.SetColumn(dotPointer, 1);  
                
//                 categoryContainer.Children.Add(btn);
//                 categoryContainer.Children.Add(dotPointer);

//                 leftPanel.Children.Add(categoryContainer);
//                 leftPanel.Children.Add(CreateLine());
//             }

//             Grid.SetRow(leftPanel, 1);
//             Grid.SetColumn(leftPanel, 0);
//             rootGrid.Children.Add(leftPanel);

//             var centerGrid = new Grid
//             {
//                 RowDefinitions = new RowDefinitions($"{DropBoxHeight}, *"),
//                 ColumnDefinitions = new ColumnDefinitions($"*, {TasklistWidth}"),
//                 Margin = new Thickness(ElementGap, ElementGap, 0, 0)
//             };

//             var dropBorder = CreateBorder();
//             dropBorder.Margin = new Thickness(0, 0, 0, ElementGap);
//             var dropContent = new StackPanel 
//             { 
//                 Orientation = Orientation.Horizontal, 
//                 HorizontalAlignment = HorizontalAlignment.Left, 
//                 VerticalAlignment = VerticalAlignment.Center,
//                 Spacing = ElementGap * 1.5,
//                 Margin = new Thickness(DropLeftPadding, 0, 0, 0) 
//             };

//             var dropText = new TextBlock 
//             { 
//                 Text = "DROP FILES", 
//                 FontSize = DropFontSize, 
//                 Foreground = textBrush,
//                 VerticalAlignment = VerticalAlignment.Center
//             };
//             var dropBar = CreateBorder();
//             dropBar.Width = 200;
//             dropBar.Height = 25;

//             dropContent.Children.Add(CreateAnimatedCat());
//             dropContent.Children.Add(dropText);
//             dropContent.Children.Add(dropBar);
//             dropBorder.Child = dropContent;

//             Grid.SetRow(dropBorder, 0);
//             Grid.SetColumnSpan(dropBorder, 2); 
//             centerGrid.Children.Add(dropBorder);

//             var nonoBorder = CreateBorder();
//             nonoBorder.Margin = new Thickness(0); 
//             Grid.SetRow(nonoBorder, 1);
//             Grid.SetColumn(nonoBorder, 0);
//             centerGrid.Children.Add(nonoBorder);

//             _taskListEngine = new TaskListUI(
//                 textBrush, bgBrush, 
//                 UIBorderThickness, UIInnerBorderThickness, 
//                 ElementGap, TasklistTitleFontSize, TasklistTextFontSize, TaskIconSize
//             );

//             Grid.SetRow(_taskListEngine, 1);
//             Grid.SetColumn(_taskListEngine, 1);
//             centerGrid.Children.Add(_taskListEngine);

//             Grid.SetRow(centerGrid, 1);
//             Grid.SetColumn(centerGrid, 1);
//             rootGrid.Children.Add(centerGrid);

//             // Initialize both floating modals
//             _settingsPanel = new SettingsPanel(bgBrush, textBrush, globalFont);
//             _historyPanel = new HistoryPanel(bgBrush, textBrush, globalFont);

//             mainContainer.Children.Add(rootGrid);
//             mainContainer.Children.Add(_settingsPanel);
//             mainContainer.Children.Add(_historyPanel); // NEW
            
//             this.Content = mainContainer;
//         }

//         private void UpdateCategoryPointers()
//         {
//             foreach (var kvp in categoryPointers)
//             {
//                 kvp.Value.IsVisible = (kvp.Key == activeCTG);
//             }
//         }

//         // ==========================================
//         // CAT AUTOMATION METHODS
//         // ==========================================
//         private Viewbox CreateAnimatedCat()
//         {
//             var canvas = new Canvas
//             {
//                 Width = 75.52,
//                 Height = 44.32,
//                 Background = Brushes.Transparent 
//             };

//             foreach (var bodyPart in Assets.CatBody)
//                 canvas.Children.Add(CreateCatPath(bodyPart.Data, bodyPart.X, bodyPart.Y));

//             eyesContainer = new Canvas
//             {
//                 Width = 75.52,
//                 Height = 44.32,
//                 RenderTransformOrigin = new RelativePoint(38, 27, RelativeUnit.Absolute),
//                 RenderTransform = new ScaleTransform()
//             };

//             var loadedEyes = new List<Avalonia.Controls.Shapes.Path>();
//             foreach (var eyePart in Assets.CatEyes)
//             {
//                 var p = CreateCatPath(eyePart.Data, eyePart.X, eyePart.Y);
//                 eyesContainer.Children.Add(p);
//                 loadedEyes.Add(p);
//             }

//             eye1 = loadedEyes[0]; eye2 = loadedEyes[1];
//             eye3 = loadedEyes[2]; eye4 = loadedEyes[3];

//             canvas.Children.Add(eyesContainer);

//             blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
//             blinkTimer.Tick += BlinkTimer_Tick;

//             moodTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
//             moodTimer.Tick += MoodTimer_Tick;

//             SetEyeState("blip");
//             moodTimer.Start();

//             return new Viewbox
//             {
//                 Height = CatSize, 
//                 Stretch = Stretch.Uniform,
//                 Child = canvas
//             };
//         }

//         private Avalonia.Controls.Shapes.Path CreateCatPath(string data, double x, double y)
//         {
//             var path = new Avalonia.Controls.Shapes.Path
//             {
//                 Data = Geometry.Parse(data),
//                 Stroke = textBrush, 
//                 StrokeThickness = CatStrokeThickness,
//                 StrokeLineCap = PenLineCap.Round,
//                 Fill = Brushes.Transparent 
//             };
//             Canvas.SetLeft(path, x);
//             Canvas.SetTop(path, y);
//             return path;
//         }

//         private void MoodTimer_Tick(object? sender, EventArgs e)
//         {
//             currentMoodIndex = (currentMoodIndex + 1) % moods.Length;
//             SetEyeState(moods[currentMoodIndex]);
//         }

//         private void SetEyeState(string state)
//         {
//             if (eye1 == null || eye2 == null || eye3 == null || eye4 == null || blinkTimer == null) return;

//             eye1.IsVisible = (state == "blip" || state == "furious");
//             eye2.IsVisible = (state == "blip" || state == "rest");
//             eye3.IsVisible = (state == "blip" || state == "furious");
//             eye4.IsVisible = (state == "blip" || state == "rest");

//             if (state == "blip") blinkTimer.Start();
//             else { blinkTimer.Stop(); ResetBlinkScale(); }
//         }

//         private async void BlinkTimer_Tick(object? sender, EventArgs e)
//         {
//             try
//             {
//                 if (eyesContainer?.RenderTransform is ScaleTransform scale)
//                 {
//                     scale.ScaleY = 0.1;
//                     await Task.Delay(100);
//                     scale.ScaleY = 1.0;
//                 }
//             }
//             catch { }
//         }

//         private void ResetBlinkScale()
//         {
//             if (eyesContainer?.RenderTransform is ScaleTransform scale) scale.ScaleY = 1.0;
//         }

//         // ==========================================
//         // UI HELPERS
//         // ==========================================
//         private Border CreateBorder()
//         {
//             return new Border
//             {
//                 BorderBrush = textBrush,
//                 BorderThickness = new Thickness(UIBorderThickness),
//                 Background = bgBrush
//             };
//         }

//         private Control CreateLine()
//         {
//             return new Border
//             {
//                 Height = LineThickness,
//                 Background = textBrush, 
//                 HorizontalAlignment = HorizontalAlignment.Stretch
//             };
//         }
        
//         private Button CreateTopIconButton(Assets.IconData icon, double size)
//         {
//             var btn = new Button { Cursor = new Cursor(StandardCursorType.Hand) };
//             btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
//             {
//                 return new Border
//                 {
//                     Background = Brushes.Transparent, 
//                     Child = CreateSvgIcon(icon, size)
//                 };
//             });
//             return btn;
//         }

//         private Avalonia.Controls.Shapes.Path CreateSvgIcon(Assets.IconData icon, double size)
//         {
//             var path = new Avalonia.Controls.Shapes.Path
//             {
//                 Data = Geometry.Parse(icon.PathData),
//                 Width = size,
//                 Height = size,
//                 Stretch = Stretch.Uniform
//             };

//             if (icon.IsStroked)
//             {
//                 path.Stroke = textBrush;
//                 path.StrokeThickness = 2.0;
//                 path.StrokeLineCap = PenLineCap.Round;
//                 path.StrokeJoin = PenLineJoin.Round;
//                 path.Fill = Brushes.Transparent;
//             }
//             else
//             {
//                 path.Fill = textBrush;
//                 path.Stroke = Brushes.Transparent;
//             }

//             return path;
//         }
//     }
// }




using System; 
using System.Collections.Generic; 
using System.Threading.Tasks;
using Avalonia; 
using Avalonia.Controls; 
using Avalonia.Layout; 
using Avalonia.Media; 
using Avalonia.Input; 
using Avalonia.Threading; 
using Glo;
using CentralGateway; // Added dependency

namespace convix { 
public class MainWindow : Window { 
    // ========================================== 
    // UI SETTINGS //
    // ========================================== 
    private readonly double CategorySpacing = 10.0; 
    private readonly double WindowPadding = 25.0; 
    private readonly double ElementGap = 15.0;

    private readonly double UIBorderThickness = 4.0;
    private readonly double UIInnerBorderThickness = 2.0;
    private readonly double LineThickness = 4.0;
    private readonly double CatStrokeThickness = 2.0;

    private readonly double LeftMenuWidth = 220.0;
    private readonly double TasklistWidth = 280.0;
    private readonly double HeaderHeight = 60.0;
    private readonly double DropBoxHeight = 110.0;

    private readonly double TitleFontSize = 40.0;
    private readonly double MenuFontSize = 24.0;
    private readonly double DropFontSize = 30.0;
    private readonly double TasklistTitleFontSize = 24.0;
    private readonly double TasklistTextFontSize = 14.0;
    
    private readonly double TopIconSize = 32.0;
    private readonly double TaskIconSize = 20.0;
    private readonly double CatSize = 80.0;
    private readonly double DropLeftPadding = 30.0;

    // ==========================================
    // STATE TRACKERS
    // ==========================================
    public string activeCTG = "Image2PDF";
    private Dictionary<string, Avalonia.Controls.Shapes.Ellipse> categoryPointers = new();
    
    private SolidColorBrush bgBrush;
    private SolidColorBrush textBrush;
    private FontFamily globalFont;

    private Canvas? eyesContainer;
    private Avalonia.Controls.Shapes.Path? eye1, eye2, eye3, eye4;
    private DispatcherTimer? blinkTimer;
    private DispatcherTimer? moodTimer;
    private string[] moods = { "blip", "rest", "furious" };
    private int currentMoodIndex = 0;

    private TaskListUI? _taskListEngine;
    private SettingsPanel? _settingsPanel; 
    private HistoryPanel? _historyPanel;

    public MainWindow()
    {
        bgBrush = new SolidColorBrush(Color.Parse(Vars.BGcolor));
        textBrush = new SolidColorBrush(Color.Parse(Vars.TEXTcolor));

        var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "convix";
        globalFont = new FontFamily($"avares://{assemblyName}/s_assets#Nunito");

        this.Background = bgBrush;
        this.Foreground = textBrush;
        this.FontFamily = globalFont;
        
        this.Title = "convix";
        this.Width = 1100;
        this.Height = 650;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildUI();

        Vars.OnSnapshotChanged += OnSnapshotChanged;
        
        Vars.OnThemeChanged += () => {
            Dispatcher.UIThread.Post(() => {
                try {
                    bgBrush.Color = Color.Parse(Vars.BGcolor);
                    textBrush.Color = Color.Parse(Vars.TEXTcolor);
                } catch { }
            });
        };

        _taskListEngine?.Refresh(activeCTG, Vars.jsonSnapshot);
    }

    protected override void OnClosed(EventArgs e)
    {
        Vars.OnSnapshotChanged -= OnSnapshotChanged;
        base.OnClosed(e);
    }

    private void OnSnapshotChanged(string newSnapshot)
    {
        _taskListEngine?.Refresh(activeCTG, newSnapshot);
    }

    private void BuildUI()
    {
        var mainContainer = new Grid(); 

        var rootGrid = new Grid
        {
            Margin = new Thickness(WindowPadding),
            RowDefinitions = new RowDefinitions($"{HeaderHeight}, *"),
            ColumnDefinitions = new ColumnDefinitions($"{LeftMenuWidth}, *")
        };

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *") };
        Grid.SetRow(headerGrid, 0);
        Grid.SetColumnSpan(headerGrid, 2);
        rootGrid.Children.Add(headerGrid);

        var title = new TextBlock
        {
            Text = "convix",
            FontSize = TitleFontSize,
            Foreground = textBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -20, 0, 0)
        };
        Grid.SetColumn(title, 0);
        headerGrid.Children.Add(title);

        var iconsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Spacing = 30.0
        };
        
        var btnHistory = CreateTopIconButton(Assets.HistoryIcon, TopIconSize);
        var btnSettings = CreateTopIconButton(Assets.SettingsIcon, TopIconSize);
        
        btnSettings.Click += (s, e) => {
            if (_settingsPanel != null) {
                _settingsPanel.RefreshUI();
                _settingsPanel.IsVisible = true;
            }
        };

        btnHistory.Click += (s, e) => {
            if (_historyPanel != null) {
                _historyPanel.RefreshUI();
                _historyPanel.IsVisible = true;
            }
        };

        iconsPanel.Children.Add(btnHistory);
        iconsPanel.Children.Add(btnSettings);
        
        Grid.SetColumn(iconsPanel, 1);
        headerGrid.Children.Add(iconsPanel);

        var leftPanel = new StackPanel { Margin = new Thickness(0, ElementGap, ElementGap, 0) };
        string[] categories = { "Image2PDF", "ImageConverter", "Office2PDF", "PDF2Image", "PDFMerger" };
        
        leftPanel.Children.Add(CreateLine());
        
        foreach (var ctg in categories)
        {
            var categoryContainer = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto, Auto"),
                Margin = new Thickness(0, CategorySpacing) 
            };

            var btn = new Button
            {
                Content = "|_" + ctg, 
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            btn.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, scope) =>
            {
                return new TextBlock
                {
                    Text = control.Content?.ToString() ?? "", 
                    FontSize = MenuFontSize,
                    Foreground = textBrush,
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center
                };
            });

            var dotPointer = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = textBrush,
                IsVisible = (activeCTG == ctg),
                Margin = new Thickness(10, 0, 0, 0), 
                VerticalAlignment = VerticalAlignment.Center
            };
            categoryPointers[ctg] = dotPointer;

            btn.Click += (s, e) => { 
                if (CentralController.isRunning) return; // <--- The lock: directly stops change without affecting UI visually
                
                activeCTG = ctg; 
                UpdateCategoryPointers();
                _taskListEngine?.Refresh(activeCTG, Vars.jsonSnapshot); 
            };
            
            Grid.SetColumn(btn, 0);         
            Grid.SetColumn(dotPointer, 1);  
            
            categoryContainer.Children.Add(btn);
            categoryContainer.Children.Add(dotPointer);

            leftPanel.Children.Add(categoryContainer);
            leftPanel.Children.Add(CreateLine());
        }

        Grid.SetRow(leftPanel, 1);
        Grid.SetColumn(leftPanel, 0);
        rootGrid.Children.Add(leftPanel);

        var centerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions($"{DropBoxHeight}, *"),
            ColumnDefinitions = new ColumnDefinitions($"*, {TasklistWidth}"),
            Margin = new Thickness(ElementGap, ElementGap, 0, 0)
        };

        var dropBorder = CreateBorder();
        dropBorder.Margin = new Thickness(0, 0, 0, ElementGap);
        var dropContent = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Left, 
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = ElementGap * 1.5,
            Margin = new Thickness(DropLeftPadding, 0, 0, 0) 
        };

        var dropText = new TextBlock 
        { 
            Text = "DROP FILES", 
            FontSize = DropFontSize, 
            Foreground = textBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        var dropBar = CreateBorder();
        dropBar.Width = 200;
        dropBar.Height = 25;

        dropContent.Children.Add(CreateAnimatedCat());
        dropContent.Children.Add(dropText);
        dropContent.Children.Add(dropBar);
        dropBorder.Child = dropContent;

        Grid.SetRow(dropBorder, 0);
        Grid.SetColumnSpan(dropBorder, 2); 
        centerGrid.Children.Add(dropBorder);

        var nonoBorder = CreateBorder();
        nonoBorder.Margin = new Thickness(0); 
        Grid.SetRow(nonoBorder, 1);
        Grid.SetColumn(nonoBorder, 0);
        centerGrid.Children.Add(nonoBorder);

        _taskListEngine = new TaskListUI(
            textBrush, bgBrush, 
            UIBorderThickness, UIInnerBorderThickness, 
            ElementGap, TasklistTitleFontSize, TasklistTextFontSize, TaskIconSize
        );

        Grid.SetRow(_taskListEngine, 1);
        Grid.SetColumn(_taskListEngine, 1);
        centerGrid.Children.Add(_taskListEngine);

        Grid.SetRow(centerGrid, 1);
        Grid.SetColumn(centerGrid, 1);
        rootGrid.Children.Add(centerGrid);

        _settingsPanel = new SettingsPanel(bgBrush, textBrush, globalFont);
        _historyPanel = new HistoryPanel(bgBrush, textBrush, globalFont);

        mainContainer.Children.Add(rootGrid);
        mainContainer.Children.Add(_settingsPanel);
        mainContainer.Children.Add(_historyPanel); 
        
        this.Content = mainContainer;
    }

    private void UpdateCategoryPointers()
    {
        foreach (var kvp in categoryPointers)
        {
            kvp.Value.IsVisible = (kvp.Key == activeCTG);
        }
    }

    // ==========================================
    // CAT AUTOMATION METHODS
    // ==========================================
    private Viewbox CreateAnimatedCat()
    {
        var canvas = new Canvas
        {
            Width = 75.52,
            Height = 44.32,
            Background = Brushes.Transparent 
        };

        foreach (var bodyPart in Assets.CatBody)
            canvas.Children.Add(CreateCatPath(bodyPart.Data, bodyPart.X, bodyPart.Y));

        eyesContainer = new Canvas
        {
            Width = 75.52,
            Height = 44.32,
            RenderTransformOrigin = new RelativePoint(38, 27, RelativeUnit.Absolute),
            RenderTransform = new ScaleTransform()
        };

        var loadedEyes = new List<Avalonia.Controls.Shapes.Path>();
        foreach (var eyePart in Assets.CatEyes)
        {
            var p = CreateCatPath(eyePart.Data, eyePart.X, eyePart.Y);
            eyesContainer.Children.Add(p);
            loadedEyes.Add(p);
        }

        eye1 = loadedEyes[0]; eye2 = loadedEyes[1];
        eye3 = loadedEyes[2]; eye4 = loadedEyes[3];

        canvas.Children.Add(eyesContainer);

        blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        blinkTimer.Tick += BlinkTimer_Tick;

        moodTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        moodTimer.Tick += MoodTimer_Tick;

        SetEyeState("blip");
        moodTimer.Start();

        return new Viewbox
        {
            Height = CatSize, 
            Stretch = Stretch.Uniform,
            Child = canvas
        };
    }

    private Avalonia.Controls.Shapes.Path CreateCatPath(string data, double x, double y)
    {
        var path = new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(data),
            Stroke = textBrush, 
            StrokeThickness = CatStrokeThickness,
            StrokeLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent 
        };
        Canvas.SetLeft(path, x);
        Canvas.SetTop(path, y);
        return path;
    }

    private void MoodTimer_Tick(object? sender, EventArgs e)
    {
        currentMoodIndex = (currentMoodIndex + 1) % moods.Length;
        SetEyeState(moods[currentMoodIndex]);
    }

    private void SetEyeState(string state)
    {
        if (eye1 == null || eye2 == null || eye3 == null || eye4 == null || blinkTimer == null) return;

        eye1.IsVisible = (state == "blip" || state == "furious");
        eye2.IsVisible = (state == "blip" || state == "rest");
        eye3.IsVisible = (state == "blip" || state == "furious");
        eye4.IsVisible = (state == "blip" || state == "rest");

        if (state == "blip") blinkTimer.Start();
        else { blinkTimer.Stop(); ResetBlinkScale(); }
    }

    private async void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (eyesContainer?.RenderTransform is ScaleTransform scale)
            {
                scale.ScaleY = 0.1;
                await Task.Delay(100);
                scale.ScaleY = 1.0;
            }
        }
        catch { }
    }

    private void ResetBlinkScale()
    {
        if (eyesContainer?.RenderTransform is ScaleTransform scale) scale.ScaleY = 1.0;
    }

    // ==========================================
    // UI HELPERS
    // ==========================================
    private Border CreateBorder()
    {
        return new Border
        {
            BorderBrush = textBrush,
            BorderThickness = new Thickness(UIBorderThickness),
            Background = bgBrush
        };
    }

    private Control CreateLine()
    {
        return new Border
        {
            Height = LineThickness,
            Background = textBrush, 
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }
    
    private Button CreateTopIconButton(Assets.IconData icon, double size)
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
}
}