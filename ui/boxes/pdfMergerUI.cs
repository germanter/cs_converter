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
    public class PdfMergerUI : CtgToolBase
    {
        public override string DefaultFileName => "merged";
        protected override bool IsRotationEnabled => false;
        protected override Assets.IconData? CustomTileIcon => Assets.DocumentIcon;

        public PdfMergerUI(Window parentWindow, SolidColorBrush bg, SolidColorBrush text, FontFamily font)
            : base(parentWindow, bg, text, font)
        {
            InitializeBase();
        }

        protected override Control CreateSettingsControl()
        {
            // PDF merging has no additional parameters, so we return an empty Grid container
            return new Grid();
        }

        protected override async Task<string> OnExecuteAsync(
            IReadOnlyList<FileCollectionPanel.FileItem> files,
            string saveDirectory,
            string filename,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var pdfPaths = files.Select(x => x.FilePath).ToArray();

            string logPath = await CentralController.PdfMergerCallerAsync(
                pdfPaths: pdfPaths,
                filePathToSave: saveDirectory,
                newFileName: filename,
                progress: progress,
                cancellationToken: cancellationToken
            );

            return logPath;
        }
    }
}