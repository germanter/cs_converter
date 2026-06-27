using Avalonia;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using Glo;

namespace convix
{
    class Program
    {
        // Static tracker to prevent single-instance Mutex from garbage collection cleanup
        private static Mutex? _mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            const string appName = "convix-unique-application-mutex-id";

            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                // Force exit if application context is already running elsewhere on the system
                return; 
            }

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                _mutex.Dispose();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace(); 
    }

    public class App : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set explicit shutdown mode to prevent early exit when transitioning windows
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

                var initWin = new InitWindow();
                desktop.MainWindow = initWin;
                initWin.Show();

                // Dispatch synchronous initialization routines to a background worker thread
                Task.Run(() =>
                {
                    try
                    {
                        // Invoke synchronous workspace installation and verification setup
                        Initialization.AppInitializer.Initialize();

                        // Transition UI screens safely on the main thread once locks are resolved
                        if (Vars.initLock)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                var mainWin = new MainWindow();
                                desktop.MainWindow = mainWin;
                                mainWin.Show();
                                initWin.Close();

                                // Re-enable standard close bindings to shut down the application lifetime
                                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                            });
                        }
                        else
                        {
                            Environment.Exit(1);
                        }
                    }
                    catch
                    {
                        Environment.Exit(1);
                    }
                });
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}