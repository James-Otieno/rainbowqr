using Microsoft.Extensions.Logging;
using QRCoder;

namespace QRCodeRegenerator.Services.QRCode
{
    public class QRCodeGenerationService : IQRCodeGenerationService
    {
        private readonly ILogger<QRCodeGenerationService> _logger;

        public QRCodeGenerationService(ILogger<QRCodeGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateQRCodeAsync(string url, string docNum, string qrCodesPath)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            if (string.IsNullOrEmpty(docNum))
                throw new ArgumentException("Document number cannot be null or empty", nameof(docNum));

            try
            {
                await EnsureQRCodeDirectoryAsync(qrCodesPath);

                _logger.LogInformation("Generating QR code for document: {DocNum} with URL: {Url}", docNum, url);

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new BitmapByteQRCode(qrCodeData);

                var qrCodeImage = qrCode.GetGraphic(25);

                string fileName = $"QR_{docNum}_{DateTime.Now:yyyyMMddHHmmss}.png";
                string filePath = Path.Combine(qrCodesPath, fileName);

                await File.WriteAllBytesAsync(filePath, qrCodeImage);
                _logger.LogInformation("Saved QR code to file: {FilePath}", filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate QR code for document {DocNum}", docNum);
                throw;
            }
        }

        public async Task EnsureQRCodeDirectoryAsync(string qrCodesPath)
        {
            try
            {
                if (!Directory.Exists(qrCodesPath))
                {
                    Directory.CreateDirectory(qrCodesPath);
                    _logger.LogInformation("Created QR codes directory: {Path}", qrCodesPath);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create QR codes directory: {Path}", qrCodesPath);
                throw;
            }
        }
    }
}