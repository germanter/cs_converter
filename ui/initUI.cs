using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Glo;

namespace convix
{
    public class InitWindow : Window
    {
        private readonly double UIBorderThickness = 4.0;
        private readonly double ElementGap = 15.0;

        public InitWindow()
        {
            // Resolve base configuration colors with fallback safe guards
            Color bgColor;
            Color textColor;

            try
            {
                bgColor = Color.Parse(Vars.baseBGcolor);
            }
            catch
            {
                bgColor = Color.Parse("#1E1E1E");
            }

            try
            {
                textColor = Color.Parse(Vars.baseTEXTcolor);
            }
            catch
            {
                textColor = Color.Parse("#FFFFFF");
            }

            var bgBrush = new SolidColorBrush(bgColor);
            var textBrush = new SolidColorBrush(textColor);

            this.Background = bgBrush;
            this.Foreground = textBrush;
            this.FontFamily = FontFamily.Default; // Safe system-default sans-serif

            this.Title = "convix - Initializing";
            this.Width = 400;
            this.Height = 220;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.SystemDecorations = SystemDecorations.None; // Minimalist borderless scene

            // Core border setup mimicking MainWindow borders
            var border = new Border
            {
                BorderBrush = textBrush,
                BorderThickness = new Thickness(UIBorderThickness),
                Background = bgBrush,
                Padding = new Thickness(25)
            };

            var contentPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = ElementGap
            };

            var titleBlock = new TextBlock
            {
                Text = "convix",
                FontSize = 32.0,
                FontWeight = FontWeight.Bold,
                Foreground = textBrush,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var divider = new Border
            {
                Height = 2.0,
                Background = textBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var statusBlock = new TextBlock
            {
                Text = "Initializing...",
                FontSize = 18.0,
                Foreground = textBrush,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var substatusBlock = new TextBlock
            {
                Text = "Please wait",
                FontSize = 14.0,
                Foreground = textBrush,
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            contentPanel.Children.Add(titleBlock);
            contentPanel.Children.Add(divider);
            contentPanel.Children.Add(statusBlock);
            contentPanel.Children.Add(substatusBlock);

            border.Child = contentPanel;
            this.Content = border;
        }
    }
}