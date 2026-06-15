using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ConvixPrototype;

// 1. PARÇA: Office to PDF Ekranı
public class Office2PdfTool : UserControl
{
    public Office2PdfTool()
    {
        var layout = new StackPanel { Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        
        layout.Children.Add(new TextBlock { Text = "📄 [Office to PDF Motoru Active]", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center });
        layout.Children.Add(new Button { Content = "Dosyaları Seç ve Çevir", HorizontalAlignment = HorizontalAlignment.Center });
        
        Content = layout;
    }
}

// 2. PARÇA: Image Compressor Ekranı
public class ImageCompressorTool : UserControl
{
    public ImageCompressorTool()
    {
        var layout = new StackPanel { Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        
        layout.Children.Add(new TextBlock { Text = "🖼️ [Image Compressor Active]", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center });
        layout.Children.Add(new Button { Content = "Görselleri Optimize Et", HorizontalAlignment = HorizontalAlignment.Center });
        
        Content = layout;
    }
}

// 3. PARÇA: Video Converter Ekranı
public class VideoConverterTool : UserControl
{
    public VideoConverterTool()
    {
        var layout = new StackPanel { Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        
        layout.Children.Add(new TextBlock { Text = "🎬 [Video Converter Active]", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center });
        layout.Children.Add(new Button { Content = "Videoyu MP4 Yap", HorizontalAlignment = HorizontalAlignment.Center });
        
        Content = layout;
    }
}