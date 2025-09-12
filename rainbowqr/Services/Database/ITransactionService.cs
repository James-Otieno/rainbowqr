using QRCodeRegenerator.Models;

namespace QRCodeRegenerator.Services.Database
{
    public interface ITransactionService
    {
        Task<List<TransactionRecord>> GetAllTransactionsAsync();
        Task<List<TransactionRecord>> GetTransactionsByIdsAsync(List<int> ids);
        Task<TransactionRecord?> GetTransactionByIdAsync(int id);
        Task<int> GetTotalTransactionCountAsync();
    }
}