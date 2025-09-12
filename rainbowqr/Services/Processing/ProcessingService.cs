using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using QRCodeRegenerator.Services.Database;
using QRCodeRegenerator.Services.QRCode;
using QRCodeRegenerator.Services.SAP;

namespace QRCodeRegenerator.Services.Processing
{
    public class ProcessingService : IProcessingService
    {
        private readonly ITransactionService _transactionService;
        private readonly ICompletedDocumentsService _completedDocumentsService;
        private readonly IConfigService _configService;
        private readonly IQRCodeGenerationService _qrCodeService;
        private readonly ISAPIntegrationService _sapService;
        private readonly ILogger<ProcessingService> _logger;

        public event EventHandler<ProcessingProgressEventArgs>? ProgressChanged;
        public event EventHandler<ProcessingCompletedEventArgs>? ProcessingCompleted;

        public ProcessingService(
            ITransactionService transactionService,
            ICompletedDocumentsService completedDocumentsService,
            IConfigService configService,
            IQRCodeGenerationService qrCodeService,
            ISAPIntegrationService sapService,
            ILogger<ProcessingService> logger)
        {
            _transactionService = transactionService;
            _completedDocumentsService = completedDocumentsService;
            _configService = configService;
            _qrCodeService = qrCodeService;
            _sapService = sapService;
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessTransactionsAsync(ProcessingOptions options, CancellationToken cancellationToken = default)
        {
            var result = new ProcessingResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("Starting transaction processing with options: {Options}", options);

                // Get transactions to process
                var transactions = options.ProcessAllRecords
                    ? await _transactionService.GetAllTransactionsAsync()
                    : await _transactionService.GetTransactionsByIdsAsync(options.SelectedRecordIds ?? new List<int>());

                result.TotalRecords = transactions.Count;

                if (transactions.Count == 0)
                {
                    _logger.LogInformation("No transactions to process");
                    result.IsCompleted = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // Get settings
                var qrCodesPath = await _configService.GetQRCodesPathAsync();
                var sapSettings = await _configService.GetSAPSettingsAsync();

                // Process each transaction
                for (int i = 0; i < transactions.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Processing cancelled by user");
                        break;
                    }

                    var transaction = transactions[i];

                    try
                    {
                        var singleResult = await ProcessSingleTransactionAsync(transaction, options.DuplicateHandling);

                        if (singleResult.Successful > 0)
                        {
                            result.Successful++;
                        }
                        else
                        {
                            result.Failed++;
                            result.Errors.AddRange(singleResult.Errors);
                        }

                        result.Processed++;

                        // Report progress
                        OnProgressChanged(new ProcessingProgressEventArgs
                        {
                            TotalRecords = result.TotalRecords,
                            ProcessedRecords = result.Processed,
                            SuccessfulRecords = result.Successful,
                            FailedRecords = result.Failed,
                            CurrentDocNum = transaction.TsNum,
                            Status = singleResult.Successful > 0 ? "Success" : "Failed",
                            RecentErrors = result.Errors.TakeLast(5).ToList()
                        });

                        // Small delay to prevent overwhelming the system
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process transaction {TsNum}", transaction.TsNum);
                        result.Failed++;
                        result.Processed++;
                        result.Errors.Add($"Transaction {transaction.TsNum}: {ex.Message}");
                    }
                }

                result.IsCompleted = !cancellationToken.IsCancellationRequested;
                result.EndTime = DateTime.Now;

                _logger.LogInformation("Processing completed. Total: {Total}, Successful: {Success}, Failed: {Failed}",
                    result.TotalRecords, result.Successful, result.Failed);

                OnProcessingCompleted(new ProcessingCompletedEventArgs
                {
                    Result = result,
                    WasCancelled = cancellationToken.IsCancellationRequested
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process transactions");
                result.Errors.Add($"Processing failed: {ex.Message}");
                result.IsCompleted = true;
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        public async Task<ProcessingResult> ProcessSingleTransactionAsync(TransactionRecord transaction, DuplicateHandling duplicateHandling)
        {
            var result = new ProcessingResult
            {
                TotalRecords = 1,
                StartTime = DateTime.Now
            };

            try
            {
                // Check if we should skip this transaction
                var existingDoc = await _completedDocumentsService.GetCompletedDocumentAsync(transaction.TsNum);
                if (existingDoc != null && duplicateHandling == DuplicateHandling.Skip)
                {
                    _logger.LogInformation("Skipping existing document: {DocNum}", transaction.TsNum);
                    result.Processed = 1;
                    result.Successful = 1;
                    result.IsCompleted = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // Validate required data
                if (string.IsNullOrEmpty(transaction.QrCode))
                {
                    var error = $"Transaction {transaction.TsNum} has no QR code URL";
                    _logger.LogWarning(error);
                    result.Errors.Add(error);
                    result.Failed = 1;
                    result.Processed = 1;
                    result.IsCompleted = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // Get paths and settings
                var qrCodesPath = await _configService.GetQRCodesPathAsync();
                var sapSettings = await _configService.GetSAPSettingsAsync();

                // Generate QR code
                var qrCodeFilePath = await _qrCodeService.GenerateQRCodeAsync(
                    transaction.QrCode,
                    transaction.TsNum,
                    qrCodesPath);

                // Create completed document entry
                var completedDoc = new CompletedDocument
                {
                    DocNum = transaction.TsNum,
                    DocType = existingDoc?.DocType ?? "IN", // Keep existing or default to 'IN'
                    CUIN = transaction.ControlCode ?? "",
                    CUSN = transaction.SerialNumber ?? "",
                    FiscalSeal = transaction.QrCode,
                    QRCodePath = qrCodeFilePath,
                    Timestamp = DateTime.UtcNow.ToString("O")
                };

                // Save to database
                var saveSuccess = await _completedDocumentsService.UpsertCompletedDocumentAsync(completedDoc);
                if (!saveSuccess)
                {
                    result.Errors.Add($"Failed to save completed document for {transaction.TsNum}");
                    result.Failed = 1;
                    result.Processed = 1;
                    result.IsCompleted = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // Update SAP
                var sapSuccess = await _sapService.UpdateDocumentInSAPAsync(transaction, qrCodeFilePath, sapSettings);
                if (!sapSuccess)
                {
                    result.Errors.Add($"Failed to update SAP for document {transaction.TsNum}");
                    result.Failed = 1;
                    result.Processed = 1;
                    result.IsCompleted = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }

                _logger.LogInformation("Successfully processed transaction: {TsNum}", transaction.TsNum);
                result.Successful = 1;
                result.Processed = 1;
                result.IsCompleted = true;
                result.EndTime = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process single transaction: {TsNum}", transaction.TsNum);
                result.Errors.Add($"Transaction {transaction.TsNum}: {ex.Message}");
                result.Failed = 1;
                result.Processed = 1;
                result.IsCompleted = true;
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        protected virtual void OnProgressChanged(ProcessingProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnProcessingCompleted(ProcessingCompletedEventArgs e)
        {
            ProcessingCompleted?.Invoke(this, e);
        }
    }
}