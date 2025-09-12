using QRCodeRegenerator.Models;

namespace QRCodeRegenerator.Services.Database
{
    public interface ICompletedDocumentsService
    {
        Task<bool> UpsertCompletedDocumentAsync(CompletedDocument document);
        Task<CompletedDocument?> GetCompletedDocumentAsync(string docNum);
        Task<List<CompletedDocument>> GetAllCompletedDocumentsAsync();
        Task<bool> DocumentExistsAsync(string docNum);
    }
}