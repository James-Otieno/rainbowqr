using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using System.Data.SQLite;

namespace QRCodeRegenerator.Services.Database
{
    public class ConfigService : IConfigService
    {
        private readonly ILogger<ConfigService> _logger;
        private const string CONFIG_DB_PATH = @"C:\FBtemp\DB\ConfigSettings.db";

        public ConfigService(ILogger<ConfigService> logger)
        {
            _logger = logger;
        }

        public async Task<SAPSettings> GetSAPSettingsAsync()
        {
            var settings = new SAPSettings();

            try
            {
                using var connection = new SQLiteConnection($"Data Source={CONFIG_DB_PATH}");
                await connection.OpenAsync();

                var query = "SELECT Key, Value FROM Settings WHERE Category = 'SAPSettings'";
                using var command = new SQLiteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var key = reader["Key"].ToString() ?? "";
                    var value = reader["Value"].ToString() ?? "";

                    switch (key)
                    {
                        case "SAP.BaseUrl":
                            settings.BaseUrl = value;
                            break;
                        case "SAP.DocumentsEndpoint":
                            settings.DocumentsEndpoint = value;
                            break;
                        case "SAP.Username":
                            settings.Username = value;
                            break;
                        case "SAP.Password":
                            settings.Password = value;
                            break;
                        case "SAP.SapClient":
                            settings.SapClient = value;
                            break;
                        case "SAP.FromDate":
                            settings.FromDate = value;
                            break;
                        case "SAP.ToDate":
                            settings.ToDate = value;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load SAP settings");
                throw;
            }

            return settings;
        }

        public async Task SaveSAPSettingsAsync(SAPSettings settings)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={CONFIG_DB_PATH}");
                await connection.OpenAsync();

                var updates = new Dictionary<string, string>
                {
                    ["SAP.BaseUrl"] = settings.BaseUrl,
                    ["SAP.DocumentsEndpoint"] = settings.DocumentsEndpoint,
                    ["SAP.Username"] = settings.Username,
                    ["SAP.Password"] = settings.Password,
                    ["SAP.SapClient"] = settings.SapClient,
                    ["SAP.FromDate"] = settings.FromDate,
                    ["SAP.ToDate"] = settings.ToDate
                };

                foreach (var kvp in updates)
                {
                    var query = @"UPDATE Settings SET Value = @value WHERE Key = @key";
                    using var command = new SQLiteCommand(query, connection);
                    command.Parameters.AddWithValue("@value", kvp.Value);
                    command.Parameters.AddWithValue("@key", kvp.Key);
                    await command.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("SAP settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save SAP settings");
                throw;
            }
        }

        public async Task<string> GetSettingValueAsync(string key)
        {
            using var connection = new SQLiteConnection($"Data Source={CONFIG_DB_PATH}");
            await connection.OpenAsync();

            var query = "SELECT Value FROM Settings WHERE Key = @key";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@key", key);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }

        public async Task SaveSettingAsync(string key, string value, string category = "AppSettings")
        {
            using var connection = new SQLiteConnection($"Data Source={CONFIG_DB_PATH}");
            await connection.OpenAsync();

            var query = @"INSERT OR REPLACE INTO Settings (Key, Value, Category) VALUES (@key, @value, @category)";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            command.Parameters.AddWithValue("@category", category);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<string> GetQRCodesPathAsync()
        {
            var path = await GetSettingValueAsync("DTR.QRCodesPath");
            if (string.IsNullOrEmpty(path))
            {
                path = @"C:\FBtemp\QRCodes";
                _logger.LogInformation("Using default QR codes path: {Path}", path);
            }
            return path;
        }
    }
}