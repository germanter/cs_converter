
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
//         // UI SETTINGS (Clean, readable parameters)
//         // ==========================================
        
//         // --- SPACING & DISTANCE ---
//         // Adjusts exact distance between the lines and the text in the category menu
//         private readonly double CategorySpacing = 10.0; 
        
//         // Padding around the entire application boundary
//         private readonly double WindowPadding = 25.0; 
        
//         // Standard gap applied between normal blocks
//         private readonly double ElementGap = 15.0; 

//         // --- LINE THICKNESS PROTOCOLS ---
//         private readonly double UIBorderThickness = 4.0;
//         private readonly double UIInnerBorderThickness = 2.0;
//         private readonly double LineThickness = 4.0;
//         private readonly double CatStrokeThickness = 2.0;

//         // --- STRUCTURAL DIMENSIONS ---
//         private readonly double LeftMenuWidth = 220.0;
//         private readonly double TasklistWidth = 280.0;
//         private readonly double HeaderHeight = 60.0;
//         private readonly double DropBoxHeight = 110.0;

//         // --- FONT SIZES ---
//         private readonly double TitleFontSize = 40.0;
//         private readonly double MenuFontSize = 24.0;
//         private readonly double DropFontSize = 30.0;
//         private readonly double TasklistTitleFontSize = 24.0;
//         private readonly double TasklistTextFontSize = 20.0;
        
//         // --- ELEMENT SIZES ---
//         private readonly double TopIconSize = 32.0;
//         private readonly double TaskIconSize = 20.0;
//         private readonly double CatSize = 80.0;
//         private readonly double DropLeftPadding = 30.0;

//         // ==========================================
//         // STATE TRACKERS
//         // ==========================================
//         public string activeCTG = "Image2PDF";
//         private Dictionary<string, Avalonia.Controls.Shapes.Ellipse> categoryPointers = new();
        
//         // Protocol Brushes
//         private SolidColorBrush bgBrush;
//         private SolidColorBrush textBrush;
//         private FontFamily globalFont;

//         // Cat Elements Tracker
//         private Canvas? eyesContainer;
//         private Avalonia.Controls.Shapes.Path? eye1, eye2, eye3, eye4;
//         private DispatcherTimer? blinkTimer;
//         private DispatcherTimer? moodTimer;
//         private string[] moods = { "blip", "rest", "furious" };
//         private int currentMoodIndex = 0;

//         public MainWindow()
//         {
//             // --- STRICT 2-COLOR PROTOCOL ENFORCEMENT ---
//             bgBrush = SolidColorBrush.Parse(Vars.BGcolor)!;
//             textBrush = SolidColorBrush.Parse(Vars.TEXTcolor)!;

//             // --- STRICT NUNITO FONT ENFORCEMENT ---
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
//         }

//         private void BuildUI()
//         {
//             var rootGrid = new Grid
//             {
//                 Margin = new Thickness(WindowPadding),
//                 RowDefinitions = new RowDefinitions($"{HeaderHeight}, *"),
//                 ColumnDefinitions = new ColumnDefinitions($"{LeftMenuWidth}, *")
//             };

//             // ====================
//             // 1. TOP HEADER
//             // ====================
//             var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *") };
//             Grid.SetRow(headerGrid, 0);
//             Grid.SetColumnSpan(headerGrid, 2);
//             rootGrid.Children.Add(headerGrid);

//             var title = new TextBlock
//             {
//                 Text = "convix",
//                 FontSize = TitleFontSize,
//                 Foreground = textBrush,
//                 VerticalAlignment = VerticalAlignment.Top
//             };
//             Grid.SetColumn(title, 0);
//             headerGrid.Children.Add(title);

//             var iconsPanel = new StackPanel
//             {
//                 Orientation = Orientation.Horizontal,
//                 HorizontalAlignment = HorizontalAlignment.Right,
//                 VerticalAlignment = VerticalAlignment.Top,
//                 Spacing = ElementGap
//             };
            
//             iconsPanel.Children.Add(CreateSvgIcon(Assets.SettingsIcon, TopIconSize));
//             iconsPanel.Children.Add(CreateSvgIcon(Assets.HistoryIcon, TopIconSize));
            
//             Grid.SetColumn(iconsPanel, 1);
//             headerGrid.Children.Add(iconsPanel);

//             // ====================
//             // 2. LEFT PANEL (CATEGORIES)
//             // ====================
//             var leftPanel = new StackPanel 
//             { 
//                 Margin = new Thickness(0, ElementGap, ElementGap, 0)
//             };
            
//             string[] categories = { "Image2PDF", "ImageConverter", "Office2PDF", "PDF2Image", "PDFMerger" };
            
//             leftPanel.Children.Add(CreateLine());
            
//             foreach (var ctg in categories)
//             {
//                 // Auto, Auto ensures the text takes exactly the space it needs, and the dot drops perfectly to the right
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

//                 // The pointer dot for the selected active category (moved to the right)
//                 var dotPointer = new Avalonia.Controls.Shapes.Ellipse
//                 {
//                     Width = 10,
//                     Height = 10,
//                     Fill = textBrush,
//                     IsVisible = (activeCTG == ctg),
//                     Margin = new Thickness(10, 0, 0, 0), // 10px spacing purely on the left side of the dot
//                     VerticalAlignment = VerticalAlignment.Center
//                 };
//                 categoryPointers[ctg] = dotPointer;

//                 btn.Click += (s, e) => { 
//                     activeCTG = ctg; 
//                     UpdateCategoryPointers();
//                 };
                
//                 Grid.SetColumn(btn, 0);         // Text first
//                 Grid.SetColumn(dotPointer, 1);  // Dot second
                
//                 categoryContainer.Children.Add(btn);
//                 categoryContainer.Children.Add(dotPointer);

//                 leftPanel.Children.Add(categoryContainer);
//                 leftPanel.Children.Add(CreateLine());
//             }

//             Grid.SetRow(leftPanel, 1);
//             Grid.SetColumn(leftPanel, 0);
//             rootGrid.Children.Add(leftPanel);

//             // ====================
//             // 3. MAIN AREA (DROP + NONO + TASKLIST)
//             // ====================
//             var centerGrid = new Grid
//             {
//                 RowDefinitions = new RowDefinitions($"{DropBoxHeight}, *"),
//                 ColumnDefinitions = new ColumnDefinitions($"*, {TasklistWidth}"),
//                 Margin = new Thickness(ElementGap, ElementGap, 0, 0)
//             };

//             // ---> DROP FILES AREA
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

//             // ---> NONO PLACEHOLDER AREA
//             var nonoBorder = CreateBorder();
//             nonoBorder.Margin = new Thickness(0); 
//             Grid.SetRow(nonoBorder, 1);
//             Grid.SetColumn(nonoBorder, 0);
//             centerGrid.Children.Add(nonoBorder);

//             // ---> TASKLIST AREA 
//             var taskListBorder = CreateBorder();
//             taskListBorder.Margin = new Thickness(-UIBorderThickness, 0, 0, 0); 
//             taskListBorder.ZIndex = 1;

//             var rightStack = new StackPanel { Margin = new Thickness(ElementGap), Spacing = ElementGap };
//             var taskTitle = new TextBlock 
//             { 
//                 Text = "Tasklist", 
//                 FontSize = TasklistTitleFontSize, 
//                 Foreground = textBrush 
//             };
//             rightStack.Children.Add(taskTitle);

//             // Mock Tasks
//             for (int i = 0; i < 3; i++)
//             {
//                 var taskBorder = CreateInnerBorder();
//                 var taskGrid = new Grid 
//                 { 
//                     ColumnDefinitions = new ColumnDefinitions("*, Auto, Auto"),
//                     Margin = new Thickness(8)
//                 };
                
//                 var taskName = new TextBlock 
//                 { 
//                     Text = "c:/docs/1.pdf", 
//                     FontSize = TasklistTextFontSize,
//                     Foreground = textBrush,
//                     VerticalAlignment = VerticalAlignment.Center
//                 };
//                 Grid.SetColumn(taskName, 0);
//                 taskGrid.Children.Add(taskName);

//                 var eyeIcon = CreateSvgIcon(Assets.EyeIcon, TaskIconSize);
//                 eyeIcon.Margin = new Thickness(10, 0);
//                 Grid.SetColumn(eyeIcon, 1);
//                 taskGrid.Children.Add(eyeIcon);

//                 var trashIcon = CreateSvgIcon(Assets.DeleteIcon, TaskIconSize);
//                 Grid.SetColumn(trashIcon, 2);
//                 taskGrid.Children.Add(trashIcon);

//                 taskBorder.Child = taskGrid;
//                 rightStack.Children.Add(taskBorder);
//             }

//             taskListBorder.Child = rightStack;
//             Grid.SetRow(taskListBorder, 1);
//             Grid.SetColumn(taskListBorder, 1);
//             centerGrid.Children.Add(taskListBorder);

//             Grid.SetRow(centerGrid, 1);
//             Grid.SetColumn(centerGrid, 1);
//             rootGrid.Children.Add(centerGrid);

//             this.Content = rootGrid;
//         }

//         // ==========================================
//         // DYNAMIC LOGIC HELPERS
//         // ==========================================
//         private void UpdateCategoryPointers()
//         {
//             foreach (var kvp in categoryPointers)
//             {
//                 kvp.Value.IsVisible = (kvp.Key == activeCTG);
//             }
//         }

//         // ==========================================
//         // CAT AUTOMATION METHODS (Using Assets.cs)
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
//             {
//                 canvas.Children.Add(CreateCatPath(bodyPart.Data, bodyPart.X, bodyPart.Y));
//             }

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

//             eye1 = loadedEyes[0];
//             eye2 = loadedEyes[1];
//             eye3 = loadedEyes[2];
//             eye4 = loadedEyes[3];

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
//             if (eyesContainer?.RenderTransform is ScaleTransform scale)
//             {
//                 scale.ScaleY = 0.1;
//                 await Task.Delay(100);
//                 scale.ScaleY = 1.0;
//             }
//         }

//         private void ResetBlinkScale()
//         {
//             if (eyesContainer?.RenderTransform is ScaleTransform scale) scale.ScaleY = 1.0;
//         }

//         // ==========================================
//         // UI HELPERS (CENTRALIZED STYLING)
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

//         private Border CreateInnerBorder()
//         {
//             return new Border
//             {
//                 BorderBrush = textBrush,
//                 BorderThickness = new Thickness(UIInnerBorderThickness),
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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
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
    public class MainWindow : Window
    {
        // ==========================================
        // UI SETTINGS
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
        private readonly double TasklistTextFontSize = 20.0;
        
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

        private StackPanel? taskListPanel;
        private TextBlock? taskTitleBlock;

        public MainWindow()
        {
            bgBrush = SolidColorBrush.Parse(Vars.BGcolor)!;
            textBrush = SolidColorBrush.Parse(Vars.TEXTcolor)!;

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

            RefreshTaskList();
        }

        protected override void OnClosed(EventArgs e)
        {
            Vars.OnSnapshotChanged -= OnSnapshotChanged;
            base.OnClosed(e);
        }

        private void OnSnapshotChanged(string newSnapshot)
        {
            RefreshTaskList();
        }

        private void BuildUI()
        {
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
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(title, 0);
            headerGrid.Children.Add(title);

            var iconsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Spacing = ElementGap
            };
            
            iconsPanel.Children.Add(CreateSvgIcon(Assets.SettingsIcon, TopIconSize));
            iconsPanel.Children.Add(CreateSvgIcon(Assets.HistoryIcon, TopIconSize));
            
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
                    activeCTG = ctg; 
                    UpdateCategoryPointers();
                    RefreshTaskList(); 
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

            var taskListBorder = CreateBorder();
            taskListBorder.Margin = new Thickness(-UIBorderThickness, 0, 0, 0); 
            taskListBorder.ZIndex = 1;

            taskListPanel = new StackPanel { Margin = new Thickness(ElementGap), Spacing = ElementGap };
            taskTitleBlock = new TextBlock 
            { 
                Text = "Tasklist", 
                FontSize = TasklistTitleFontSize, 
                Foreground = textBrush 
            };
            
            taskListPanel.Children.Add(taskTitleBlock);
            taskListBorder.Child = taskListPanel;

            Grid.SetRow(taskListBorder, 1);
            Grid.SetColumn(taskListBorder, 1);
            centerGrid.Children.Add(taskListBorder);

            Grid.SetRow(centerGrid, 1);
            Grid.SetColumn(centerGrid, 1);
            rootGrid.Children.Add(centerGrid);

            this.Content = rootGrid;
        }

        private void UpdateCategoryPointers()
        {
            foreach (var kvp in categoryPointers)
            {
                kvp.Value.IsVisible = (kvp.Key == activeCTG);
            }
        }

        // --- TASKLIST ENGINE ---
        private void RefreshTaskList()
        {
            if (taskListPanel == null || taskTitleBlock == null) return;

            // Fetch data in background thread
            Task.Run(async () =>
            {
                var result = await TaskListHelper.GetCompletedTasksAsync(activeCTG, Vars.jsonSnapshot);

                // Forces Avalonia UI Thread to redraw safely without crashing
                Dispatcher.UIThread.Post(() =>
                {
                    taskListPanel.Children.Clear();
                    taskListPanel.Children.Add(taskTitleBlock);

                    if (result.Status == "success")
                    {
                        foreach (var task in result.Tasks)
                        {
                            taskListPanel.Children.Add(CreateTaskItem(task.FullPath, task.Uuid));
                        }
                    }
                });
            });
        }

        private Border CreateTaskItem(string fullPath, string uuid)
        {
            bool isFolder = fullPath.EndsWith("*");
            string cleanPath = fullPath.TrimEnd('*', '/', '\\');
            string displayPath = fullPath;
            
            try
            {
                string[] parts = cleanPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    displayPath = $"/{parts[parts.Length - 2]}/{parts[parts.Length - 1]}";
                else if (parts.Length == 1)
                    displayPath = $"/{parts[0]}";
                
                if (isFolder) displayPath += "/";
            }
            catch { /* Keep raw */ }

            var taskBorder = CreateInnerBorder();
            var taskGrid = new Grid 
            { 
                ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto, Auto"),
                Margin = new Thickness(8)
            };

            // 1. [X] CANCEL BUTTON
            var btnCancel = CreateIconButton(Assets.CancelIcon, TaskIconSize);
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += async (s, e) => { 
                await Writer.Mode5_NukeLogsAsync(new List<string> { uuid }); 
            };
            Grid.SetColumn(btnCancel, 0);
            taskGrid.Children.Add(btnCancel);

            // 2. TEXT PATH
            var taskName = new TextBlock 
            { 
                Text = displayPath, 
                FontSize = TasklistTextFontSize,
                Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(taskName, 1);
            taskGrid.Children.Add(taskName);

            // 3. EYE BUTTON
            var btnEye = CreateIconButton(Assets.EyeIcon, TaskIconSize);
            btnEye.Margin = new Thickness(10, 0);
            btnEye.Click += async (s, e) => {
                try {
                    string target = isFolder ? cleanPath : fullPath;
                    if(File.Exists(target) || Directory.Exists(target)) {
                        Process.Start(new ProcessStartInfo {
                            FileName = "explorer",
                            Arguments = isFolder ? $"\"{target}\"" : $"/select,\"{target}\"",
                            UseShellExecute = true
                        });
                    } else {
                        await Writer.Mode5_NukeLogsAsync(new List<string> { uuid });
                    }
                } catch {
                    await Writer.Mode5_NukeLogsAsync(new List<string> { uuid });
                }
            };
            Grid.SetColumn(btnEye, 2);
            taskGrid.Children.Add(btnEye);

            // 4. TRASH BUTTON (Hidden if output is a Folder)
            if (!isFolder)
            {
                var btnTrash = CreateIconButton(Assets.DeleteIcon, TaskIconSize);
                btnTrash.Margin = new Thickness(10, 0, 0, 0);
                btnTrash.Click += async (s, e) => {
                    await TaskListHelper.DeleteFileAsync(fullPath);
                    await Writer.Mode5_NukeLogsAsync(new List<string> { uuid });
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
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

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
            if (eyesContainer?.RenderTransform is ScaleTransform scale)
            {
                scale.ScaleY = 0.1;
                await Task.Delay(100);
                scale.ScaleY = 1.0;
            }
        }

        private void ResetBlinkScale()
        {
            if (eyesContainer?.RenderTransform is ScaleTransform scale) scale.ScaleY = 1.0;
        }

        // ==========================================
        // UI HELPERS (CENTRALIZED STYLING)
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

        private Border CreateInnerBorder()
        {
            return new Border
            {
                BorderBrush = textBrush,
                BorderThickness = new Thickness(UIInnerBorderThickness),
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