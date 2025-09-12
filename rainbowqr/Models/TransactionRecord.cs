namespace QRCodeRegenerator.Models
{
    public class TransactionRecord
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string? BuyerPIN { get; set; }
        public int TrType { get; set; }
        public string TsNum { get; set; } = string.Empty;
        public string? MwNum { get; set; }
        public decimal TotalRounding { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VatAmountA { get; set; }
        public decimal VatAmountB { get; set; }
        public decimal VatAmountC { get; set; }
        public decimal VatAmountD { get; set; }
        public decimal VatAmountE { get; set; }
        public string? ControlCode { get; set; }
        public DateTime? SendDate { get; set; }
        public string? RelevantMwNum { get; set; }
        public string? TypeNote { get; set; }
        public string? SerialNumber { get; set; }
        public string? QrCode { get; set; }
    }

    public class CompletedDocument
    {
        public int Id { get; set; }
        public string DocNum { get; set; } = string.Empty;
        public string DocType { get; set; } = string.Empty;
        public string CUIN { get; set; } = string.Empty;
        public string CUSN { get; set; } = string.Empty;
        public string FiscalSeal { get; set; } = string.Empty;
        public string? QRCodePath { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }

    public class SAPSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string DocumentsEndpoint { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SapClient { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
    }

    public class ProcessingOptions
    {
        public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.Overwrite;
        public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.BulkOperation;
        public List<int>? SelectedRecordIds { get; set; }
        public bool ProcessAllRecords { get; set; } = true;
    }

    public enum DuplicateHandling
    {
        Skip,
        Overwrite,
        Update
    }

    public enum ProcessingMode
    {
        BulkOperation,
        OngoingMonitoring
    }

    public class ProcessingResult
    {
        public int TotalRecords { get; set; }
        public int Processed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class SAPUpdatePayload
    {
        public string vbeln { get; set; } = string.Empty;
        public string cusn { get; set; } = string.Empty;
        public string cuin { get; set; } = string.Empty;
        public string fiscalerror { get; set; } = string.Empty;
        public string fiscalseal { get; set; } = string.Empty;
        public string status { get; set; } = "1";
        public string qrcodepath { get; set; } = string.Empty;
    }

    public class SAPResponse
    {
        public string TYPE { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public int NUMBER { get; set; }
        public string MESSAGE { get; set; } = string.Empty;
        public string LOG_NO { get; set; } = string.Empty;
        public int LOG_MSG_NO { get; set; }
        public string MESSAGE_V1 { get; set; } = string.Empty;
        public string MESSAGE_V2 { get; set; } = string.Empty;
        public string MESSAGE_V3 { get; set; } = string.Empty;
        public string MESSAGE_V4 { get; set; } = string.Empty;
        public string PARAMETER { get; set; } = string.Empty;
        public int ROW { get; set; }
        public string FIELD { get; set; } = string.Empty;
        public string SYSTEM { get; set; } = string.Empty;
    }
}