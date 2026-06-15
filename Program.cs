// using System;
// using Avalonia;
// using Avalonia.Controls.ApplicationLifetimes;
// using Avalonia.Themes.Fluent;

// namespace ConvixPrototype;

// class Program
// {
//     [STAThread]
//     public static void Main(string[] args) => BuildAvaloniaApp()
//         .StartWithClassicDesktopLifetime(args);

//     public static AppBuilder BuildAvaloniaApp()
//         => AppBuilder.Configure<App>()
//             .UsePlatformDetect()
//             .LogToTrace();
// }

// public class App : Application
// {
//     public override void Initialize()
//     {
//         Styles.Add(new FluentTheme());
//     }

//     public override void OnFrameworkInitializationCompleted()
//     {
//         if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//         {
//             desktop.MainWindow = new MainWindow();
//         }
//         base.OnFrameworkInitializationCompleted();
//     }
// }



/// latest cat + work
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using CentralGateway; // Needed to reach your CentralController

// Resolve the name collision between System.IO.Path and Avalonia.Controls.Shapes.Path
using Path = Avalonia.Controls.Shapes.Path;

namespace InteractiveCatApp;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public class MainWindow : Window
{
    // Cat variables
    private Path eye1, eye2, eye3, eye4;
    private Button btnBlip, btnRest, btnFurious;
    private Canvas eyesContainer;
    private DispatcherTimer blinkTimer;

    // Task Integration variables
    private Button btnRunTask, btnCancelTask;
    private TextBlock txtResult;
    private ProgressBar pbTaskProgress; // <-- NEW PROGRESS BAR
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        Title = "Interactive Cat & Conversion Engine";
        Width = 700;
        Height = 650;
        Background = SolidColorBrush.Parse("#1a1a1a");
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // Container Stack Setup
        var mainStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20
        };

        // --- 1. CAT SVG VISUALS ---
        var viewbox = new Viewbox
        {
            MaxWidth = 500,
            Stretch = Stretch.Uniform
        };

        var canvas = new Canvas
        {
            Width = 75.52,
            Height = 44.32,
            Background = Brushes.Black
        };

        Path CreateCatPath(string data, double x, double y)
        {
            var path = new Path
            {
                Data = Geometry.Parse(data),
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                StrokeLineCap = PenLineCap.Round,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(path, x);
            Canvas.SetTop(path, y);
            return path;
        }

        canvas.Children.Add(CreateCatPath("M0 0 C2.66 -7.87, 5.31 -15.74, 7.59 -22.49 M0 0 C1.96 -5.82, 3.93 -11.64, 7.59 -22.49", 10, 32.49));
        canvas.Children.Add(CreateCatPath("M0 0 C3.01 2.99, 6.02 5.97, 9.89 9.81 M0 0 C2.91 2.89, 5.82 5.78, 9.89 9.81", 17.86, 10.01));
        canvas.Children.Add(CreateCatPath("M0 0 C4.14 0.03, 8.29 0.06, 18.55 0.13 M0 0 C6.74 0.05, 13.47 0.09, 18.55 0.13", 27.91, 19.88));
        canvas.Children.Add(CreateCatPath("M0 0 C3.51 -3.15, 7.03 -6.29, 10.87 -9.73 M0 0 C4.09 -3.66, 8.18 -7.32, 10.87 -9.73", 46.43, 20.01));
        canvas.Children.Add(CreateCatPath("M0 0 C-2.62 -7.16, -5.23 -14.31, -8.18 -22.38 M0 0 C-2.3 -6.3, -4.61 -12.6, -8.18 -22.38", 65.52, 32.57));
        canvas.Children.Add(CreateCatPath("M0 0 C2.92 0.01, 5.84 0.02, 10.73 0.04 M0 0 C4.01 0.01, 8.03 0.03, 10.73 0.04", 32.26, 34.29));

        eyesContainer = new Canvas
        {
            Width = 75.52,
            Height = 44.32,
            RenderTransformOrigin = new RelativePoint(38, 27, RelativeUnit.Absolute),
            RenderTransform = new ScaleTransform()
        };

        eye1 = CreateCatPath("M0 0 C3.35 1.69, 6.71 3.39, 9.04 4.56 M0 0 C2.1 1.06, 4.21 2.12, 9.04 4.56", 19.44, 24.26);
        eye2 = CreateCatPath("M0 0 C-3.39 0.02, -6.78 0.05, -10.11 0.07 M0 0 C-2.48 0.02, -4.96 0.04, -10.11 0.07", 28.42, 29.51);
        eye3 = CreateCatPath("M0 0 C3.45 -1.34, 6.9 -2.68, 9.31 -3.62 M0 0 C3.35 -1.3, 6.7 -2.6, 9.31 -3.62", 46.62, 28.47);
        eye4 = CreateCatPath("M0 0 C-2.74 -0.01, -5.47 -0.02, -10.51 -0.03 M0 0 C-3.81 -0.01, -7.62 -0.03, -10.51 -0.03", 57.15, 29.32);

        eyesContainer.Children.Add(eye1);
        eyesContainer.Children.Add(eye2);
        eyesContainer.Children.Add(eye3);
        eyesContainer.Children.Add(eye4);

        canvas.Children.Add(eyesContainer);
        viewbox.Child = canvas;
        mainStack.Children.Add(viewbox);

        // --- 2. CAT CONTROLS ---
        var controlsStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Button CreateButton(string text)
        {
            return new Button
            {
                Content = text,
                Padding = new Thickness(24, 12),
                FontSize = 16,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(2)
            };
        }

        btnBlip = CreateButton("Blip");
        btnRest = CreateButton("Rest");
        btnFurious = CreateButton("Furious");

        btnBlip.Click += (s, e) => SetEyeState("blip");
        btnRest.Click += (s, e) => SetEyeState("rest");
        btnFurious.Click += (s, e) => SetEyeState("furious");

        controlsStack.Children.Add(btnBlip);
        controlsStack.Children.Add(btnRest);
        controlsStack.Children.Add(btnFurious);
        mainStack.Children.Add(controlsStack);

        // --- 3. BACKGROUND TASK EXECUTION CONTROLS ---
        var separator = new Border { Height = 2, Background = SolidColorBrush.Parse("#333"), Margin = new Thickness(0, 20) };
        mainStack.Children.Add(separator);

        var taskControlsStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        btnRunTask = CreateButton("Run Office2Pdf");
        btnRunTask.Background = SolidColorBrush.Parse("#0078D7"); // Blue Run Button
        
        btnCancelTask = CreateButton("X");
        btnCancelTask.Background = SolidColorBrush.Parse("#D13438"); // Red Cancel Button
        btnCancelTask.Foreground = Brushes.White;
        btnCancelTask.IsEnabled = false; // Disabled by default until a task is running
        btnCancelTask.FontWeight = FontWeight.Bold;

        btnRunTask.Click += BtnRunTask_Click;
        btnCancelTask.Click += BtnCancelTask_Click;

        taskControlsStack.Children.Add(btnRunTask);
        taskControlsStack.Children.Add(btnCancelTask);
        mainStack.Children.Add(taskControlsStack);

        // --- NEW: PROGRESS BAR ---
        pbTaskProgress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 10,
            MaxWidth = 400,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Foreground = SolidColorBrush.Parse("#0078D7"), // Matches the blue run button
            Margin = new Thickness(0, 5)
        };
        mainStack.Children.Add(pbTaskProgress);

        // Result Display TextBlock
        txtResult = new TextBlock
        {
            Foreground = Brushes.LightGreen,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 600,
            MinHeight = 80,
            Text = "Task results will appear here."
        };
        mainStack.Children.Add(txtResult);

        Content = mainStack;

        // Start Cat blink timer
        blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        blinkTimer.Tick += BlinkTimer_Tick;
        SetEyeState("blip");
    }

    // ==============================================
    // TASK EXECUTION LOGIC
    // ==============================================
    private async void BtnRunTask_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Don't run multiple at once
        if (_cts != null) return; 

        _cts = new CancellationTokenSource();
        btnRunTask.IsEnabled = false;
        btnCancelTask.IsEnabled = true;
        txtResult.Foreground = Brushes.Yellow;
        txtResult.Text = "Running Office to PDF Conversion...\n(This happens entirely on a background thread)";
        
        // Reset the progress bar
        pbTaskProgress.Value = 0;

        // THE MAGIC: This object automatically handles safely jumping back to the UI thread
        var progressReporter = new Progress<double>(percentage => 
        {
            pbTaskProgress.Value = percentage;
        });

        try
        {
            // Explicit parameters requested
            string[] inputPaths = new string[]
            {
                @"C:\Users\GERMANTATE\Downloads\mainj.docx",
                @"C:\Users\GERMANTATE\Downloads\Lecture 3_IT PM (1).docx"
            };
            string newFileName = "ConvertedOfficeDoc";
            string filePathToSave = @"C:\Users\GERMANTATE\Downloads";
            string mode = "docx-pdf"; 
            int merge = 1;

            // Run the controller using the cancellation token AND the progress reporter
            string[] result = await CentralController.Office2PdfCallerAsync(
                inputPaths: inputPaths,
                newFileName: newFileName,
                filePathToSave: filePathToSave,
                mode: mode,
                merge: merge,
                progress: progressReporter, // <--- WIRED UP HERE
                cancellationToken: _cts.Token
            );

            // Print success output to screen
            pbTaskProgress.Value = 100; // Force it to full on success
            txtResult.Foreground = Brushes.LightGreen;
            txtResult.Text = "Execution completed successfully.\nOutput paths:\n" + string.Join("\n", result);
        }
        catch (OperationCanceledException)
        {
            pbTaskProgress.Value = 0; // Reset bar on cancel
            txtResult.Foreground = Brushes.Orange;
            txtResult.Text = "Task was intentionally canceled by the user.";
        }
        catch (Exception ex)
        {
            pbTaskProgress.Value = 0; // Reset bar on fail
            txtResult.Foreground = Brushes.Red;
            txtResult.Text = $"Execution failed: {ex.Message}";
        }
        finally
        {
            // Reset task UI elements after completion, crash, or cancel
            _cts.Dispose();
            _cts = null;
            btnRunTask.IsEnabled = true;
            btnCancelTask.IsEnabled = false;
        }
    }

    private void BtnCancelTask_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Tell the background task to kill itself immediately
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            txtResult.Text = "Canceling operation... please wait.";
            _cts.Cancel();
        }
    }

    // ==============================================
    // CAT UI LOGIC
    // ==============================================
    private void SetEyeState(string state)
    {
        eye1.IsVisible = (state == "blip" || state == "furious");
        eye2.IsVisible = (state == "blip" || state == "rest");
        eye3.IsVisible = (state == "blip" || state == "furious");
        eye4.IsVisible = (state == "blip" || state == "rest");

        if (state == "blip") blinkTimer.Start();
        else
        {
            blinkTimer.Stop();
            ResetBlinkScale();
        }

        StyleButton(btnBlip, state == "blip");
        StyleButton(btnRest, state == "rest");
        StyleButton(btnFurious, state == "furious");
    }

    private void StyleButton(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.Background = Brushes.White;
            btn.Foreground = SolidColorBrush.Parse("#1a1a1a");
            btn.BorderBrush = Brushes.White;
            btn.FontWeight = FontWeight.Bold;
        }
        else
        {
            btn.Background = SolidColorBrush.Parse("#333");
            btn.Foreground = Brushes.White;
            btn.BorderBrush = SolidColorBrush.Parse("#ffffff33");
            btn.FontWeight = FontWeight.Normal;
        }
    }

    private async void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        if (eyesContainer.RenderTransform is ScaleTransform scale)
        {
            scale.ScaleY = 0.1;
            await Task.Delay(100);
            scale.ScaleY = 1.0;
        }
    }

    private void ResetBlinkScale()
    {
        if (eyesContainer.RenderTransform is ScaleTransform scale)
        {
            scale.ScaleY = 1.0;
        }
    }
}