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
    public class Pdf2ImageUI : CtgToolBase
    {
        private CustomSlider _qualitySlider = null!;

        public override string DefaultFileName => "page";
        protected override bool IsRotationEnabled => false; // strictly disabled rotation panel on pdf2image categories
        protected override Assets.IconData? CustomTileIcon => Assets.DocumentIcon; // Uses Document icon instead of Image icon
        protected override int MaxFilesAllowed => 1; // strictly restricted to exactly 1 file

        public Pdf2ImageUI(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
            : base(parentWindow, bg, text, font)
        {
            InitializeBase();
        }

        protected override Control CreateSettingsControl()
        {
            var settingsGrid = new Grid { Margin = new Thickness(0, 5, 0, 10) };
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            // Left stack serves as an empty aligner to preserve uniform settings row metrics
            var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 25 };
            Grid.SetColumn(leftStack, 0);
            settingsGrid.Children.Add(leftStack);

            // Quality Slider Panel (Right-aligned, matching the structure of Image2PDF)
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

            return settingsGrid;
        }

        protected override async Task<string> OnExecuteAsync(
            IReadOnlyList<FileCollectionPanel.FileItem> files,
            string saveDirectory,
            string filename,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var targetFile = files.FirstOrDefault();
            if (targetFile == null) return string.Empty;

            int qualSel = (int)_qualitySlider.Value;

            // Invoke CentralController Pdf2ImageCallerAsync
            string logPath = await CentralController.Pdf2ImageCallerAsync(
                pdfPath: targetFile.FilePath,
                outputPath: saveDirectory,
                filename: filename,
                quality: qualSel,
                progress: progress,
                cancellationToken: cancellationToken
            );

            return logPath;
        }
    }
}