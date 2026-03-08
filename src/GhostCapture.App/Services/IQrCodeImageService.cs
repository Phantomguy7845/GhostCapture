using System.Windows.Media.Imaging;

namespace GhostCapture.App.Services;

public interface IQrCodeImageService
{
    BitmapSource GenerateQrCode(string payload);
}

