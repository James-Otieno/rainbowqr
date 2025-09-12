using QRCodeRegenerator.Models;

namespace QRCodeRegenerator.Services.SAP
{
    public interface ISAPIntegrationService
    {
        Task<bool> UpdateDocumentInSAPAsync(TransactionRecord transaction, string qrCodePath, SAPSettings settings);
        Task<(bool IsConnected, string Message, string ResponseContent)> TestConnectionAsync(SAPSettings settings);
    }
}