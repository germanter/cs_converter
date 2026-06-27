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
    public enum OfficeMode
    {
        Docx2PDF,
        Pptx2PDF
    }

    public enum MergeOption
    {
        True,
        False
    }

    public class Office2PdfUI : CtgToolBase
    {
        private TwoColorDropdown<OfficeMode> _modeDropdown = null!;
        private TwoColorDropdown<MergeOption> _mergeDropdown = null!;

        public override string DefaultFileName => "document";
        protected override bool IsRotationEnabled => false;
        protected override Assets.IconData? CustomTileIcon => Assets.DocumentIcon;

        public Office2PdfUI(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
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

            _modeDropdown = new TwoColorDropdown<OfficeMode>("MODE", OfficeMode.Docx2PDF, bgBrush, textBrush, globalFont);
            _mergeDropdown = new TwoColorDropdown<MergeOption>("MERGE", MergeOption.True, bgBrush, textBrush, globalFont);

            leftStack.Children.Add(_modeDropdown);
            leftStack.Children.Add(_mergeDropdown);

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
            var inputPaths = files.Select(x => x.FilePath).ToArray();

            // Maps choices to expectations: "docx-pdf" or "pptx-pdf"
            string modeStr = _modeDropdown.SelectedValue == OfficeMode.Docx2PDF ? "docx-pdf" : "pptx-pdf";

            // Map merge choices to int representation: True = 1, False = 0
            int mergeVal = _mergeDropdown.SelectedValue == MergeOption.True ? 1 : 0;

            string logPath = await CentralController.Office2PdfCallerAsync(
                inputPaths: inputPaths,
                newFileName: filename,
                filePathToSave: saveDirectory,
                mode: modeStr,
                merge: mergeVal,
                progress: progress,
                cancellationToken: cancellationToken
            );

            return logPath;
        }
    }
}