using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace convix
{
    public static class FileInputHelper
    {
        /// <summary>
        /// Returns the default allowed file extensions based on the current category.
        /// </summary>
        public static IReadOnlyList<FilePickerFileType> GetAllowedFileTypes(string category)
        {
            switch (category)
            {
                case "Image2PDF":
                    return new[] { new FilePickerFileType("Image Files") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp", "*.avif", "*.ico", "*.bmp", "*.tiff", "*.tif", "*.tga", "*.psd" } } };
                
                case "ImageConverter":
                    return new[] { new FilePickerFileType("Image Files") { Patterns = new[] { "*.jpg", "*.jpeg", "*.ico", "*.png", "*.webp", "*.bmp", "*.tiff", "*.tif" } } };
                
                case "Office2PDF":
                    return new[] { new FilePickerFileType("Office Documents") { Patterns = new[] { "*.docx", "*.pptx" } } };
                
                case "PDF2Image":
                case "PDFMerger":
                    return new[] { new FilePickerFileType("PDF Documents") { Patterns = new[] { "*.pdf" } } };
                
                default:
                    return new[] { new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } } };
            }
        }
    }
}