using QRCodeRegenerator.Models;

namespace QRCodeRegenerator.Services.Database
{
    public interface IConfigService
    {
        Task<SAPSettings> GetSAPSettingsAsync();
        Task SaveSAPSettingsAsync(SAPSettings settings);
        Task<string> GetSettingValueAsync(string key);
        Task SaveSettingAsync(string key, string value, string category = "AppSettings");
        Task<string> GetQRCodesPathAsync();
    }
}