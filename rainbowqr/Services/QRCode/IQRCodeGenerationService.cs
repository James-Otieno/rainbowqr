namespace QRCodeRegenerator.Services.QRCode
{
    public interface IQRCodeGenerationService
    {
        Task<string> GenerateQRCodeAsync(string url, string docNum, string qrCodesPath);
        Task EnsureQRCodeDirectoryAsync(string qrCodesPath);
    }
}