using QRCodeRegenerator.Models;

namespace QRCodeRegenerator.Services.Processing
{
    public interface IProcessingService
    {
        event EventHandler<ProcessingProgressEventArgs>? ProgressChanged;
        event EventHandler<ProcessingCompletedEventArgs>? ProcessingCompleted;

        Task<ProcessingResult> ProcessTransactionsAsync(ProcessingOptions options, CancellationToken cancellationToken = default);
        Task<ProcessingResult> ProcessSingleTransactionAsync(TransactionRecord transaction, DuplicateHandling duplicateHandling);
    }

    public class ProcessingProgressEventArgs : EventArgs
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public string CurrentDocNum { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> RecentErrors { get; set; } = new();
    }

    public class ProcessingCompletedEventArgs : EventArgs
    {
        public ProcessingResult Result { get; set; } = new();
        public bool WasCancelled { get; set; }
    }
}