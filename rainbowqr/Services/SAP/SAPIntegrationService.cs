using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QRCodeRegenerator.Services.SAP
{
    public class SAPIntegrationService : ISAPIntegrationService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SAPIntegrationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly HttpClientHandler? _handler;

        public SAPIntegrationService(HttpClient httpClient, ILogger<SAPIntegrationService> logger)
        {
            // Don't use the injected HttpClient - create our own with proper SSL configuration
            _logger = logger;
            
            // Initialize JSON serialization options following proven pattern
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Create our own HttpClient with proper SSL bypass (following proven pattern)
            var (client, handler) = CreateConfiguredHttpClientWithHandler(_logger);
            _httpClient = client;
            _handler = handler;

            _logger.LogInformation("SAPIntegrationService initialized with proven DocumentService pattern and SSL bypass");
        }

        public SAPIntegrationService(ILogger<SAPIntegrationService> logger) : this(CreateConfiguredHttpClient(logger), logger)
        {
            // This constructor creates its own HttpClient with proper SSL configuration
        }

        private static (HttpClient client, HttpClientHandler handler) CreateConfiguredHttpClientWithHandler(ILogger<SAPIntegrationService> logger)
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) => {
                    logger.LogDebug("SSL Certificate validation - Errors: {Errors}", errors);
                    // Accept all certificates for internal SAP servers (following proven pattern)
                    return true;
                }
            };

            var httpClient = new HttpClient(handler);
            
            // Set timeout for SAP operations (proven pattern uses reasonable timeouts)
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            logger.LogDebug("HTTP client created with SSL bypass and 30-second timeout");
            return (httpClient, handler);
        }

        private static HttpClient CreateConfiguredHttpClient(ILogger<SAPIntegrationService> logger)
        {
            var (client, _) = CreateConfiguredHttpClientWithHandler(logger);
            return client;
        }

        private void ConfigureHttpClient()
        {
            // Clear default headers and configure following proven pattern
            _httpClient.BaseAddress = null;
            _httpClient.DefaultRequestHeaders.Clear();
            
            // Set timeout for SAP operations (proven pattern uses reasonable timeouts)
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _logger.LogDebug("HTTP client configured for SAP S/4HANA communication");
        }

        public async Task<bool> UpdateDocumentInSAPAsync(TransactionRecord transaction, string qrCodePath, SAPSettings settings)
        {
            try
            {
                var url = $"{settings.BaseUrl}{settings.DocumentsEndpoint}?sap-client={settings.SapClient}";

                var updateData = new SAPUpdatePayload
                {
                    vbeln = transaction.TsNum,
                    cusn = transaction.SerialNumber ?? "",
                    cuin = transaction.ControlCode ?? "",
                    fiscalerror = "",
                    fiscalseal = transaction.QrCode ?? "",
                    status = "1",
                    qrcodepath = qrCodePath
                };

                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

                // Add authentication
                var authBytes = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}");
                var authBase64 = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);

                _logger.LogInformation("Sending SAP update for document: {DocNum}", transaction.TsNum);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var sapResponse = JsonSerializer.Deserialize<SAPResponse>(responseContent);

                    if (sapResponse?.TYPE == "S")
                    {
                        _logger.LogInformation("Successfully updated document {DocNum} in SAP", transaction.TsNum);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("SAP returned non-success type: {Type} for document {DocNum}", sapResponse?.TYPE, transaction.TsNum);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogWarning("Document {DocNum} already exists in SAP fiscalization table - attempting delete and retry", transaction.TsNum);
                    
                    // Try to delete the existing record first
                    var deleteSuccess = await DeleteDocumentFromSAP(transaction.TsNum, settings);
                    if (deleteSuccess)
                    {
                        _logger.LogInformation("Successfully deleted existing record for document {DocNum}, retrying update", transaction.TsNum);
                        
                        // Create new request for retry (original request was already sent)
                        var retryRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                        
                        var retryResponse = await _httpClient.SendAsync(retryRequest);
                        var retryContent = await retryResponse.Content.ReadAsStringAsync();
                        
                        if (retryResponse.IsSuccessStatusCode)
                        {
                            var retrySapResponse = JsonSerializer.Deserialize<SAPResponse>(retryContent);
                            if (retrySapResponse?.TYPE == "S")
                            {
                                _logger.LogInformation("Successfully updated document {DocNum} in SAP after delete-retry", transaction.TsNum);
                                return true;
                            }
                            else
                            {
                                _logger.LogError("Retry failed - SAP returned non-success type: {Type} for document {DocNum}", retrySapResponse?.TYPE, transaction.TsNum);
                            }
                        }
                        else
                        {
                            _logger.LogError("Retry failed with status: {Status}, Content: {Content}", retryResponse.StatusCode, retryContent);
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to delete existing record for document {DocNum}, cannot retry update", transaction.TsNum);
                    }
                }
                else
                {
                    _logger.LogError("SAP request failed with status: {Status}, Content: {Content}", response.StatusCode, responseContent);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update document {DocNum} in SAP", transaction.TsNum);
                return false;
            }
        }

        public async Task<bool> DeleteDocumentFromSAP(string docNum, SAPSettings settings)
        {
            try
            {
                var url = $"{settings.BaseUrl}{settings.DocumentsEndpoint}?sap-client={settings.SapClient}";

                _logger.LogInformation("=== SAP DELETE API REQUEST START ===");
                _logger.LogInformation("Deleting document from S4HANA: {DocNum}", docNum);
                _logger.LogInformation("Target URL: {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                var payload = new { vbeln = docNum };
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                
                _logger.LogInformation("=== RAW REQUEST DETAILS ===");
                _logger.LogInformation("HTTP Method: {Method}", request.Method);
                _logger.LogInformation("Request URI: {Uri}", request.RequestUri);
                _logger.LogInformation("Request Payload (JSON): {Json}", json);

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add Basic Authentication header
                var authBytes = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}");
                var authBase64 = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);

                // Add required headers following proven pattern
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("Connection", "keep-alive");
                request.Headers.Add("Cache-Control", "no-cache");

                // Log all request headers
                _logger.LogInformation("=== RAW REQUEST HEADERS ===");
                foreach (var header in request.Headers)
                {
                    var headerValue = header.Key == "Authorization" ? "Basic [REDACTED]" : string.Join(", ", header.Value);
                    _logger.LogInformation("Header: {Key} = {Value}", header.Key, headerValue);
                }
                if (request.Content?.Headers != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        _logger.LogInformation("Content-Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                    }
                }

                // Clear default headers before sending (following proven pattern)
                _httpClient.DefaultRequestHeaders.Clear();
                
                _logger.LogInformation("=== SENDING HTTP REQUEST ===");
                var requestTimestamp = DateTime.Now;
                _logger.LogInformation("Request sent at: {Timestamp}", requestTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                
                var response = await _httpClient.SendAsync(request);
                
                var responseTimestamp = DateTime.Now;
                var elapsed = responseTimestamp - requestTimestamp;
                _logger.LogInformation("Response received at: {Timestamp} (Elapsed: {Elapsed}ms)", responseTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), elapsed.TotalMilliseconds);
                
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("=== RAW RESPONSE DETAILS ===");
                _logger.LogInformation("HTTP Status Code: {StatusCode} ({StatusCodeNumber})", response.StatusCode, (int)response.StatusCode);
                _logger.LogInformation("Reason Phrase: {ReasonPhrase}", response.ReasonPhrase ?? "N/A");
                
                // Log all response headers
                _logger.LogInformation("=== RAW RESPONSE HEADERS ===");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("Response-Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                }
                if (response.Content?.Headers != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        _logger.LogInformation("Content-Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                    }
                }
                
                _logger.LogInformation("=== RAW RESPONSE BODY ===");
                _logger.LogInformation("Content Length: {Length} bytes", content?.Length ?? 0);
                _logger.LogInformation("Raw Response Body: {Content}", string.IsNullOrEmpty(content) ? "[EMPTY]" : content);
                _logger.LogInformation("=== SAP DELETE API REQUEST END ===");

                // Check for successful status code
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse SAP response
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var sapResponse = JsonSerializer.Deserialize<SAPResponse>(content, _jsonOptions);

                            // Check for SAP success type "S"
                            if (sapResponse != null && sapResponse.TYPE?.Equals("S", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _logger.LogInformation("Successfully deleted document {DocNum} from SAP. Message: {Message}", docNum, sapResponse.MESSAGE);
                                return true;
                            }
                            else if (sapResponse != null)
                            {
                                _logger.LogWarning("SAP returned non-success type for document {DocNum}. Type: {Type}, Message: {Message}", docNum, sapResponse.TYPE, sapResponse.MESSAGE);
                            }
                        }
                        else
                        {
                            // For empty content, check sap-server header
                            if (response.Headers.Contains("sap-server"))
                            {
                                _logger.LogInformation("Successfully deleted document {DocNum} from SAP (verified by sap-server header)", docNum);
                                return true;
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "JSON parsing error for document {DocNum} delete. Content: {Content}", docNum, content?.Substring(0, Math.Min(content.Length, 500)));
                        if (response.Headers.Contains("sap-server"))
                        {
                            return true; // Consider it successful if we have the sap-server header
                        }
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Authentication failed for document {DocNum} delete. Please check credentials.", docNum);
                    return false;
                }

                _logger.LogWarning("SAP returned unexpected response for document {DocNum} delete. Status: {StatusCode}, Content: {Content}", docNum, response.StatusCode, content);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document {DocNum} from SAP", docNum);
                return false;
            }
        }

        public async Task<(bool IsConnected, string Message, string ResponseContent)> TestConnectionAsync(SAPSettings settings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings?.BaseUrl) || string.IsNullOrWhiteSpace(settings.Username))
                {
                    var message = "❌ Invalid SAP settings - BaseUrl and Username are required";
                    _logger.LogError(message);
                    return (false, message, string.Empty);
                }

                _logger.LogInformation("=== SAP TEST CONNECTION API REQUEST START ===");
                _logger.LogInformation("Testing SAP S/4HANA connection to {BaseUrl} following proven DocumentService pattern", settings.BaseUrl);
                
                // Build URL with proper date parameters (following proven pattern)
                var url = $"{settings.BaseUrl}{settings.DocumentsEndpoint}?sap-client={settings.SapClient}&fromdate={settings.FromDate}&todate={settings.ToDate}";

                _logger.LogInformation("=== RAW REQUEST DETAILS ===");
                _logger.LogInformation("HTTP Method: GET");
                _logger.LogInformation("Request URI: {Url}", url);
                _logger.LogInformation("SAP Client: {SapClient}", settings.SapClient);
                _logger.LogInformation("Date Range: {FromDate} to {ToDate}", settings.FromDate, settings.ToDate);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Set authentication header following proven pattern
                var authBytes = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}");
                var authBase64 = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                
                // Add standard SAP headers following proven pattern
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                request.Headers.Add("Connection", "keep-alive");
                request.Headers.Add("Cache-Control", "no-cache");

                // Log all request headers
                _logger.LogInformation("=== RAW REQUEST HEADERS ===");
                foreach (var header in request.Headers)
                {
                    var headerValue = header.Key == "Authorization" ? "Basic [REDACTED]" : string.Join(", ", header.Value);
                    _logger.LogInformation("Header: {Key} = {Value}", header.Key, headerValue);
                }

                _logger.LogInformation("Sending SAP test request with proven headers pattern");
                _logger.LogInformation("Authentication: {Username}/[PASSWORD_MASKED]", settings.Username);

                // Clear default headers before sending (following proven pattern)
                _httpClient.DefaultRequestHeaders.Clear();

                _logger.LogInformation("=== SENDING HTTP REQUEST ===");
                var requestTimestamp = DateTime.Now;
                _logger.LogInformation("Request sent at: {Timestamp}", requestTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                var response = await _httpClient.SendAsync(request);
                
                var responseTimestamp = DateTime.Now;
                var elapsed = responseTimestamp - requestTimestamp;
                _logger.LogInformation("Response received at: {Timestamp} (Elapsed: {Elapsed}ms)", responseTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), elapsed.TotalMilliseconds);
                
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("=== RAW RESPONSE DETAILS ===");
                _logger.LogInformation("HTTP Status Code: {StatusCode} ({StatusCodeNumber})", response.StatusCode, (int)response.StatusCode);
                _logger.LogInformation("Reason Phrase: {ReasonPhrase}", response.ReasonPhrase ?? "N/A");
                
                // Log all response headers
                _logger.LogInformation("=== RAW RESPONSE HEADERS ===");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("Response-Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                }
                if (response.Content?.Headers != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        _logger.LogInformation("Content-Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                    }
                }
                
                _logger.LogInformation("=== RAW RESPONSE BODY ===");
                _logger.LogInformation("Content Length: {Length} bytes", responseContent?.Length ?? 0);
                _logger.LogInformation("Raw Response Body: {Content}", string.IsNullOrEmpty(responseContent) ? "[EMPTY]" : responseContent);
                _logger.LogInformation("=== SAP TEST CONNECTION API REQUEST END ===");

                if (response.IsSuccessStatusCode)
                {
                    // Check for empty content (following proven pattern)
                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        _logger.LogInformation("SAP connection successful but no content returned (empty response)");
                        
                        // Check for sap-server header as success indicator (following proven pattern)
                        if (response.Headers.Contains("sap-server"))
                        {
                            return (true, "✅ Connection successful - SAP server verified (empty response with sap-server header)", string.Empty);
                        }
                        
                        return (true, "✅ Connection successful - SAP server reachable (empty response)", string.Empty);
                    }

                    // Try to parse SAP response for additional validation (following proven pattern)
                    try
                    {
                        // Check if response looks like JSON
                        if (responseContent.TrimStart().StartsWith("[") || responseContent.TrimStart().StartsWith("{"))
                        {
                            var testDeserialization = JsonSerializer.Deserialize<object>(responseContent, _jsonOptions);
                            _logger.LogInformation("SAP returned valid JSON response");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "SAP response is not valid JSON, but connection successful");
                    }

                    var message = $"✅ Connection successful! SAP S/4HANA server responded: {response.StatusCode} ({response.ReasonPhrase})";
                    _logger.LogInformation("SAP connection test successful");
                    return (true, message, responseContent);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var message = "❌ Authentication failed! Please verify SAP username and password credentials";
                    _logger.LogError("SAP authentication failed: 401 Unauthorized");
                    return (false, message, responseContent);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var message = "❌ SAP endpoint not found! Please verify Base URL and Documents Endpoint configuration";
                    _logger.LogError("SAP endpoint not found: 404 Not Found");
                    return (false, message, responseContent);
                }
                else
                {
                    var message = $"❌ SAP connection failed: {response.StatusCode} ({response.ReasonPhrase})";
                    _logger.LogWarning("SAP connection failed with status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent?.Substring(0, Math.Min(responseContent.Length, 500)));
                    return (false, message, responseContent);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("timeout"))
            {
                var message = "❌ Connection timeout! SAP server did not respond within 30 seconds";
                _logger.LogError(ex, "SAP connection test timed out");
                return (false, message, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                var message = $"❌ Network error connecting to SAP: {ex.Message}";
                _logger.LogError(ex, "Network error during SAP connection test");
                return (false, message, string.Empty);
            }
            catch (Exception ex)
            {
                var message = $"❌ Unexpected error during SAP connection test: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during SAP connection test");
                return (false, message, string.Empty);
            }
        }

        public void Dispose()
        {
            // Dispose resources following proven pattern
            _httpClient?.Dispose();
            _handler?.Dispose();
            _logger.LogDebug("SAPIntegrationService disposed");
        }

        /// <summary>
        /// SAP Response structure following proven DocumentService pattern
        /// Used for parsing SAP S/4HANA API responses
        /// </summary>
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

        /// <summary>
        /// Date formatting utility following proven DocumentService pattern
        /// Ensures dates are properly formatted for SAP S/4HANA APIs
        /// </summary>
        private string FormatDateForSAP(string date)
        {
            // First, try to parse as YYYYMMDD (following proven pattern)
            if (DateTime.TryParseExact(date, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
            {
                return date;
            }

            // If it's in a different format, try to parse and convert
            if (DateTime.TryParse(date, out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyyMMdd");
            }

            // If all parsing fails, return original
            _logger.LogWarning("Could not parse date {Date} for SAP formatting, using as-is", date);
            return date;
        }
    }
}