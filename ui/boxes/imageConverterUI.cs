using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;

using ImageToPdfApp;
using PdfEngine;
using PdfUtilities;
using Orchestration;
using Glo;
using CentralGateway;

namespace convix
{
    public enum ImageFormatOption
    {
        Png,
        Jpeg,
        Bmp,
        Tiff,
        Tif,
        Webp,
        Ico
    }

    public class ImageConverterUI : CtgToolBase
    {
        private TwoColorDropdown<ImageFormatOption> _formatDropdown = null!;

        public override string DefaultFileName => "image";
        protected override bool IsRotationEnabled => false; // strictly disabled rotation panel on converter categories

        public ImageConverterUI(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
            : base(parentWindow, bg, text, font)
        {
            InitializeBase();
        }

        protected override Control CreateSettingsControl()
        {
            var grid = new Grid { Margin = new Thickness(0, 5, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 25 };

            _formatDropdown = new TwoColorDropdown<ImageFormatOption>("CONVERT TO", ImageFormatOption.Png, bgBrush, textBrush, globalFont);
            leftStack.Children.Add(_formatDropdown);

            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            return grid;
        }

        protected override async Task<string> OnExecuteAsync(
            IReadOnlyList<FileCollectionPanel.FileItem> files,
            string saveDirectory,
            string filename,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var targetFormat = _formatDropdown.SelectedValue.ToString().ToLower();
            var sourceImages = files.Select(x => x.FilePath).ToArray();

            // Invoke CentralController ImageConverterCallerAsync with the required filename parameter
            string logPath = await CentralController.ImageConverterCallerAsync(
                sourceImages: sourceImages,
                targetFormat: targetFormat,
                outputPath: saveDirectory,
                filename: filename, // Added parameter to match the active method overload signature
                progress: progress,
                cancellationToken: cancellationToken
            );

            return logPath;
        }
    }
}