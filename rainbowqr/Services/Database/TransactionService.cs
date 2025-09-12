using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using System.Data.SQLite;

namespace QRCodeRegenerator.Services.Database
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private const string TRANSACTION_DB_PATH = @"C:\FBtemp\DB\FbTransaction.db";

        public TransactionService(ILogger<TransactionService> logger)
        {
            _logger = logger;
        }

        public async Task<List<TransactionRecord>> GetAllTransactionsAsync()
        {
            var transactions = new List<TransactionRecord>();

            try
            {
                using var connection = new SQLiteConnection($"Data Source={TRANSACTION_DB_PATH}");
                await connection.OpenAsync();

                var query = @"SELECT Id, Date, BuyerPIN, TrType, TsNum, MwNum, TotalRounding, TotalAmount, 
                             VatAmountA, VatAmountB, VatAmountC, VatAmountD, VatAmountE, ControlCode, 
                             SendDate, RelevantMwNum, TypeNote, SerialNumber, QrCode 
                             FROM fb_transaction ORDER BY Id";

                using var command = new SQLiteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    transactions.Add(MapReaderToTransaction(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve transactions");
                throw;
            }

            return transactions;
        }

        public async Task<List<TransactionRecord>> GetTransactionsByIdsAsync(List<int> ids)
        {
            if (!ids.Any()) return new List<TransactionRecord>();

            var transactions = new List<TransactionRecord>();
            var idsString = string.Join(",", ids);

            try
            {
                using var connection = new SQLiteConnection($"Data Source={TRANSACTION_DB_PATH}");
                await connection.OpenAsync();

                var query = $@"SELECT Id, Date, BuyerPIN, TrType, TsNum, MwNum, TotalRounding, TotalAmount, 
                              VatAmountA, VatAmountB, VatAmountC, VatAmountD, VatAmountE, ControlCode, 
                              SendDate, RelevantMwNum, TypeNote, SerialNumber, QrCode 
                              FROM fb_transaction WHERE Id IN ({idsString}) ORDER BY Id";

                using var command = new SQLiteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    transactions.Add(MapReaderToTransaction(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve transactions by IDs");
                throw;
            }

            return transactions;
        }

        public async Task<TransactionRecord?> GetTransactionByIdAsync(int id)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={TRANSACTION_DB_PATH}");
                await connection.OpenAsync();

                var query = @"SELECT Id, Date, BuyerPIN, TrType, TsNum, MwNum, TotalRounding, TotalAmount, 
                             VatAmountA, VatAmountB, VatAmountC, VatAmountD, VatAmountE, ControlCode, 
                             SendDate, RelevantMwNum, TypeNote, SerialNumber, QrCode 
                             FROM fb_transaction WHERE Id = @id";

                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapReaderToTransaction(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve transaction by ID: {Id}", id);
                throw;
            }

            return null;
        }

        public async Task<int> GetTotalTransactionCountAsync()
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={TRANSACTION_DB_PATH}");
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM fb_transaction";
                using var command = new SQLiteCommand(query, connection);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get transaction count");
                return 0;
            }
        }

        private static TransactionRecord MapReaderToTransaction(System.Data.Common.DbDataReader reader)
        {
            return new TransactionRecord
            {
                Id = Convert.ToInt32(reader["Id"]),
                Date = reader["Date"] is DBNull ? null : Convert.ToDateTime(reader["Date"]),
                BuyerPIN = reader["BuyerPIN"] is DBNull ? null : reader["BuyerPIN"].ToString(),
                TrType = Convert.ToInt32(reader["TrType"]),
                TsNum = reader["TsNum"].ToString() ?? "",
                MwNum = reader["MwNum"] is DBNull ? null : reader["MwNum"].ToString(),
                TotalRounding = Convert.ToDecimal(reader["TotalRounding"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                VatAmountA = Convert.ToDecimal(reader["VatAmountA"]),
                VatAmountB = Convert.ToDecimal(reader["VatAmountB"]),
                VatAmountC = Convert.ToDecimal(reader["VatAmountC"]),
                VatAmountD = Convert.ToDecimal(reader["VatAmountD"]),
                VatAmountE = Convert.ToDecimal(reader["VatAmountE"]),
                ControlCode = reader["ControlCode"] is DBNull ? null : reader["ControlCode"].ToString(),
                SendDate = reader["SendDate"] is DBNull ? null : Convert.ToDateTime(reader["SendDate"]),
                RelevantMwNum = reader["RelevantMwNum"] is DBNull ? null : reader["RelevantMwNum"].ToString(),
                TypeNote = reader["TypeNote"] is DBNull ? null : reader["TypeNote"].ToString(),
                SerialNumber = reader["SerialNumber"] is DBNull ? null : reader["SerialNumber"].ToString(),
                QrCode = reader["QrCode"] is DBNull ? null : reader["QrCode"].ToString()
            };
        }
    }
}