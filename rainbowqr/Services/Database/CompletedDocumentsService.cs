using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using System.Data.SQLite;

namespace QRCodeRegenerator.Services.Database
{
    public class CompletedDocumentsService : ICompletedDocumentsService
    {
        private readonly ILogger<CompletedDocumentsService> _logger;
        private const string ERROR_LOG_DB_PATH = @"C:\FBtemp\DB\ErrorLog.db";

        public CompletedDocumentsService(ILogger<CompletedDocumentsService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> UpsertCompletedDocumentAsync(CompletedDocument document)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={ERROR_LOG_DB_PATH}");
                await connection.OpenAsync();

                var query = @"INSERT OR REPLACE INTO CompletedDocuments 
                             (DocNum, DocType, CUIN, CUSN, FiscalSeal, QRCodePath, Timestamp)
                             VALUES (@docNum, @docType, @cuin, @cusn, @fiscalSeal, @qrCodePath, @timestamp)";

                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@docNum", document.DocNum);
                command.Parameters.AddWithValue("@docType", document.DocType);
                command.Parameters.AddWithValue("@cuin", document.CUIN);
                command.Parameters.AddWithValue("@cusn", document.CUSN);
                command.Parameters.AddWithValue("@fiscalSeal", document.FiscalSeal);
                command.Parameters.AddWithValue("@qrCodePath", document.QRCodePath ?? "");
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("O"));

                var rowsAffected = await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Upserted completed document: {DocNum}", document.DocNum);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert completed document: {DocNum}", document.DocNum);
                return false;
            }
        }

        public async Task<CompletedDocument?> GetCompletedDocumentAsync(string docNum)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={ERROR_LOG_DB_PATH}");
                await connection.OpenAsync();

                var query = @"SELECT Id, DocNum, DocType, CUIN, CUSN, FiscalSeal, QRCodePath, Timestamp 
                             FROM CompletedDocuments WHERE DocNum = @docNum";

                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@docNum", docNum);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new CompletedDocument
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        DocNum = reader["DocNum"].ToString() ?? "",
                        DocType = reader["DocType"].ToString() ?? "",
                        CUIN = reader["CUIN"].ToString() ?? "",
                        CUSN = reader["CUSN"].ToString() ?? "",
                        FiscalSeal = reader["FiscalSeal"].ToString() ?? "",
                        QRCodePath = reader["QRCodePath"] is DBNull ? null : reader["QRCodePath"].ToString(),
                        Timestamp = reader["Timestamp"].ToString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get completed document: {DocNum}", docNum);
            }

            return null;
        }

        public async Task<List<CompletedDocument>> GetAllCompletedDocumentsAsync()
        {
            var documents = new List<CompletedDocument>();

            try
            {
                using var connection = new SQLiteConnection($"Data Source={ERROR_LOG_DB_PATH}");
                await connection.OpenAsync();

                var query = @"SELECT Id, DocNum, DocType, CUIN, CUSN, FiscalSeal, QRCodePath, Timestamp 
                             FROM CompletedDocuments ORDER BY Timestamp DESC";

                using var command = new SQLiteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    documents.Add(new CompletedDocument
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        DocNum = reader["DocNum"].ToString() ?? "",
                        DocType = reader["DocType"].ToString() ?? "",
                        CUIN = reader["CUIN"].ToString() ?? "",
                        CUSN = reader["CUSN"].ToString() ?? "",
                        FiscalSeal = reader["FiscalSeal"].ToString() ?? "",
                        QRCodePath = reader["QRCodePath"] is DBNull ? null : reader["QRCodePath"].ToString(),
                        Timestamp = reader["Timestamp"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all completed documents");
                throw;
            }

            return documents;
        }

        public async Task<bool> DocumentExistsAsync(string docNum)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={ERROR_LOG_DB_PATH}");
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM CompletedDocuments WHERE DocNum = @docNum";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@docNum", docNum);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if document exists: {DocNum}", docNum);
                return false;
            }
        }
    }
}