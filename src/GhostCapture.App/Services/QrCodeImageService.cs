using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace GhostCapture.App.Services;

public sealed class QrCodeImageService : IQrCodeImageService
{
    public BitmapSource GenerateQrCode(string payload)
    {
        // Keep a clear quiet zone and avoid excessive downscaling in the WPF image host.
        var pngBytes = PngByteQRCodeHelper.GetQRCode(payload, QRCodeGenerator.ECCLevel.Q, 12, true);

        using var stream = new MemoryStream(pngBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }
}
