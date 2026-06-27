
// new guy
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

using ImageToPdfApp;
using PdfEngine;
using PdfUtilities;
using Orchestration;
using Glo;
using CentralGateway;

namespace convix
{
    public class Image2PdfUI : CtgToolBase
    {
        private TwoColorDropdown<PageSizeOption> _pageSizeDropdown = null!;
        private TwoColorDropdown<OrientationOption> _orientationDropdown = null!;
        private TwoColorDropdown<MarginOption> _marginDropdown = null!;
        private TwoColorDropdown<ImageFitOption> _imageFitDropdown = null!;
        private CustomSlider _qualitySlider = null!;

        public override string DefaultFileName => "document";
        protected override bool IsRotationEnabled => true;

        public Image2PdfUI(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
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

            return settingsGrid;
        }

        protected override async Task<string> OnExecuteAsync(
            IReadOnlyList<FileCollectionPanel.FileItem> files,
            string saveDirectory,
            string filename,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var pageSel = _pageSizeDropdown.SelectedValue;
            var orientSel = _orientationDropdown.SelectedValue;
            var marginSel = _marginDropdown.SelectedValue;
            var imageFitSel = _imageFitDropdown.SelectedValue;
            int qualSel = (int)_qualitySlider.Value;

            var imagesToConvert = new List<ImageInput>();
            foreach (var item in files)
            {
                imagesToConvert.Add(new ImageInput(item.FilePath, item.Rotation));
            }

            string outputPdf = await CentralController.Image2PdfCallerAsync(
                images: imagesToConvert,
                saveDirectory: saveDirectory,
                filename: filename,
                pageSize: pageSel,
                orientation: orientSel,
                margin: marginSel,
                imageFit: imageFitSel,
                quality: qualSel,
                progress: progress,
                cancellationToken: cancellationToken
            );

            return outputPdf;
        }
    }
}