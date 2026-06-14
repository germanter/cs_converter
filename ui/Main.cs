using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ConvixPrototype;

public class MainWindow : Window
{
    // Ekranda sürekli değişecek olan o sihirli "boş kutu / div"
    private ContentControl _centerDisplay;

    public MainWindow()
    {
        Title = "Convix Pure C# Architecture Test";
        Width = 700;
        Height = 450;

        // --- BUILT-IN NUNITO FONT ATAMASI ---
        // "avares://" protokolü ile s_assets içindeki ttf dosyasını doğrudan içeriye çekiyoruz.
        // #Nunito kısmı, ttf dosyasının içindeki font ailesinin gerçek adıdır.
        this.FontFamily = new FontFamily("avares://cs_convx/s_assets#Nunito");

        // Ekranı 2 sütuna bölüyoruz: [Sol Menü: 200 piksel] ve [Sağ İçerik: Kalan Tüm Alan]
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        // --- SOL MENÜ (SKELETON FIXED UI) ---
        var sidebar = new StackPanel 
        { 
            Background = Brushes.Gray, 
            Spacing = 15,
            Margin = new Avalonia.Thickness(0)
        };

        var titleText = new TextBlock 
        { 
            Text = "CONVIX v1.0", 
            FontSize = 22, 
            Foreground = Brushes.White, 
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 20),
            FontWeight = FontWeight.Normal // Nunito ile temiz ve düz durması için
        };
        sidebar.Children.Add(titleText);

        // Kategorilerimiz (Butonlar)
        var btnOffice = new Button { Content = "Office to PDF", HorizontalAlignment = HorizontalAlignment.Stretch };
        var btnImage = new Button { Content = "Image Compressor", HorizontalAlignment = HorizontalAlignment.Stretch };
        var btnVideo = new Button { Content = "Video Converter", HorizontalAlignment = HorizontalAlignment.Stretch };

        // Butonlara tıklandığında ilgili ekranları yükle (Mock sınıflar)
        // btnOffice.Click += (s, e) => SwitchDynamicScreen(new Office2PdfTool());
        // btnImage.Click += (s, e) => SwitchDynamicScreen(new ImageCompressorTool());
        // btnVideo.Click += (s, e) => SwitchDynamicScreen(new VideoConverterTool());

        sidebar.Children.Add(btnOffice);
        sidebar.Children.Add(btnImage);
        sidebar.Children.Add(btnVideo);

        // Sol menüyü Grid'in 0. sütununa koyduk
        Grid.SetColumn(sidebar, 0);
        mainGrid.Children.Add(sidebar);

        // --- SAĞ İÇERİK ALANI (DİNAMİK DEĞİŞEN BOX) ---
        _centerDisplay = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Avalonia.Thickness(20)
        };

        // Varsayılan boş veya hoş geldiniz metni (Test edebilmeniz için örnek semboller ekledim)
        _centerDisplay.Content = new TextBlock 
        { 
            Text = "Nunito Test Alanı:\n✨ 🚀 👑 🎯 💥 ⚡ 🔮\n½ ¼ ¾ ‰ ↑ → ↓ ← ≠ ≤ ≥ ±", 
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Dinamik kutuyu Grid'in 1. sütununa koyduk
        Grid.SetColumn(_centerDisplay, 1);
        mainGrid.Children.Add(_centerDisplay);

        // Pencerenin ana içeriğini bu hazırladığımız grid yapıyoruz
        Content = mainGrid;
    }

    private void SwitchDynamicScreen(UserControl newToolScreen)
    {
        _centerDisplay.Content = newToolScreen;
    }
}