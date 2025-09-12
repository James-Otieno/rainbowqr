// Forms/ConfigurationForm.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using QRCodeRegenerator.Services.Database;
using QRCodeRegenerator.Services.SAP;

namespace QRCodeRegenerator.Forms
{
    public partial class ConfigurationForm : Form
    {
        private readonly IConfigService _configService;
        private readonly ILogger<ConfigurationForm> _logger;
        private SAPSettings _currentSettings = new();

        public ConfigurationForm(IConfigService configService, ILogger<ConfigurationForm> logger)
        {
            _configService = configService;
            _logger = logger;
            InitializeComponent();
        }

        // Controls
        private TextBox txtBaseUrl = new();
        private TextBox txtDocumentsEndpoint = new();
        private TextBox txtUsername = new();
        private TextBox txtPassword = new();
        private TextBox txtSapClient = new();
        private DateTimePicker dtpFromDate = new();
        private DateTimePicker dtpToDate = new();
        private Button btnTestConnection = new();
        private Button btnSave = new();
        private Button btnCancel = new();
        private Label lblConnectionStatus = new();

        private void InitializeComponent()
        {
            Text = "SAP Configuration";
            Size = new Size(650, 550);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            var titleLabel = new Label
            {
                Text = "SAP S/4HANA Connection Configuration",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 55, 72),
                AutoSize = true,
                Padding = new Padding(20, 20, 20, 10)
            };

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                Padding = new Padding(25, 60, 25, 25),
                BackColor = Color.Transparent
            };

            // Base URL
            var lblBaseUrl = new Label { Text = "Base URL:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblBaseUrl, 0, 0);
            txtBaseUrl.Dock = DockStyle.Fill;
            txtBaseUrl.Text = "https://172.17.22.43:44300";
            txtBaseUrl.Font = new Font("Segoe UI", 9F);
            txtBaseUrl.BorderStyle = BorderStyle.FixedSingle;
            txtBaseUrl.BackColor = Color.White;
            txtBaseUrl.Padding = new Padding(8);
            mainPanel.Controls.Add(txtBaseUrl, 1, 0);

            // Documents Endpoint
            var lblEndpoint = new Label { Text = "Documents Endpoint:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblEndpoint, 0, 1);
            txtDocumentsEndpoint.Dock = DockStyle.Fill;
            txtDocumentsEndpoint.Text = "/ZREST_FISCAL/FISCAL";
            txtDocumentsEndpoint.Font = new Font("Segoe UI", 9F);
            txtDocumentsEndpoint.BorderStyle = BorderStyle.FixedSingle;
            txtDocumentsEndpoint.BackColor = Color.White;
            txtDocumentsEndpoint.Padding = new Padding(8);
            mainPanel.Controls.Add(txtDocumentsEndpoint, 1, 1);

            // Username
            var lblUsername = new Label { Text = "Username:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblUsername, 0, 2);
            txtUsername.Dock = DockStyle.Fill;
            txtUsername.Font = new Font("Segoe UI", 9F);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.BackColor = Color.White;
            txtUsername.Padding = new Padding(8);
            mainPanel.Controls.Add(txtUsername, 1, 2);

            // Password
            var lblPassword = new Label { Text = "Password:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblPassword, 0, 3);
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Font = new Font("Segoe UI", 9F);
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.BackColor = Color.White;
            txtPassword.Padding = new Padding(8);
            mainPanel.Controls.Add(txtPassword, 1, 3);

            // SAP Client
            var lblSapClient = new Label { Text = "SAP Client:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblSapClient, 0, 4);
            txtSapClient.Dock = DockStyle.Fill;
            txtSapClient.Text = "100";
            txtSapClient.Font = new Font("Segoe UI", 9F);
            txtSapClient.BorderStyle = BorderStyle.FixedSingle;
            txtSapClient.BackColor = Color.White;
            txtSapClient.Padding = new Padding(8);
            mainPanel.Controls.Add(txtSapClient, 1, 4);

            // From Date
            var lblFromDate = new Label { Text = "From Date:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblFromDate, 0, 5);
            dtpFromDate.Dock = DockStyle.Fill;
            dtpFromDate.Format = DateTimePickerFormat.Short;
            dtpFromDate.Font = new Font("Segoe UI", 9F);
            mainPanel.Controls.Add(dtpFromDate, 1, 5);

            // To Date
            var lblToDate = new Label { Text = "To Date:", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            mainPanel.Controls.Add(lblToDate, 0, 6);
            dtpToDate.Dock = DockStyle.Fill;
            dtpToDate.Format = DateTimePickerFormat.Short;
            dtpToDate.Font = new Font("Segoe UI", 9F);
            mainPanel.Controls.Add(dtpToDate, 1, 6);

            // Connection Test
            btnTestConnection.Text = "🔗 Test Connection";
            btnTestConnection.Dock = DockStyle.Fill;
            btnTestConnection.BackColor = Color.FromArgb(59, 130, 246);
            btnTestConnection.ForeColor = Color.White;
            btnTestConnection.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnTestConnection.FlatStyle = FlatStyle.Flat;
            btnTestConnection.FlatAppearance.BorderSize = 0;
            btnTestConnection.Height = 40;
            btnTestConnection.Cursor = Cursors.Hand;
            btnTestConnection.Click += BtnTestConnection_Click;
            mainPanel.SetColumnSpan(btnTestConnection, 2);
            mainPanel.Controls.Add(btnTestConnection, 0, 7);

            // Connection Status
            lblConnectionStatus.Text = "ℹ️ Connection not tested";
            lblConnectionStatus.ForeColor = Color.FromArgb(107, 114, 128);
            lblConnectionStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblConnectionStatus.Dock = DockStyle.Fill;
            lblConnectionStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblConnectionStatus.BackColor = Color.FromArgb(249, 250, 251);
            lblConnectionStatus.BorderStyle = BorderStyle.FixedSingle;
            mainPanel.SetColumnSpan(lblConnectionStatus, 2);
            mainPanel.Controls.Add(lblConnectionStatus, 0, 8);

            // Buttons Panel
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(249, 250, 251), Padding = new Padding(10) };
            
            btnSave.Text = "✓ Save Configuration";
            btnSave.Size = new Size(140, 40);
            btnSave.Location = new Point(buttonPanel.Width - 300, 10);
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.BackColor = Color.FromArgb(16, 185, 129);
            btnSave.ForeColor = Color.White;
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += BtnSave_Click;

            btnCancel.Text = "✕ Cancel";
            btnCancel.Size = new Size(120, 40);
            btnCancel.Location = new Point(buttonPanel.Width - 140, 10);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.BackColor = Color.FromArgb(107, 114, 128);
            btnCancel.ForeColor = Color.White;
            btnCancel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;

            buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            // Set column styles
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Set row styles  
            for (int i = 0; i < 7; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Test button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Status

            Controls.Add(titleLabel);
            Controls.Add(mainPanel);
            Controls.Add(buttonPanel);

            Load += ConfigurationForm_Load;
        }

        private async void ConfigurationForm_Load(object sender, EventArgs e)
        {
            _logger.LogInformation("=== CONFIGURATION FORM LOADING ===");
            _logger.LogInformation("ConfigurationForm_Load event triggered at {Time}", DateTime.Now);
            
            try
            {
                _logger.LogInformation("Loading SAP settings from database...");
                _currentSettings = await _configService.GetSAPSettingsAsync();
                
                _logger.LogInformation("Settings loaded from database:");
                _logger.LogInformation("  BaseUrl: '{BaseUrl}'", _currentSettings.BaseUrl ?? "[NULL]");
                _logger.LogInformation("  DocumentsEndpoint: '{Endpoint}'", _currentSettings.DocumentsEndpoint ?? "[NULL]");
                _logger.LogInformation("  Username: '{Username}'", _currentSettings.Username ?? "[NULL]");
                _logger.LogInformation("  Password: '{PasswordMask}'", string.IsNullOrEmpty(_currentSettings.Password) ? "[EMPTY/NULL]" : "[" + _currentSettings.Password.Length + " characters]");
                _logger.LogInformation("  SapClient: '{Client}'", _currentSettings.SapClient ?? "[NULL]");
                _logger.LogInformation("  FromDate: '{FromDate}'", _currentSettings.FromDate ?? "[NULL]");
                _logger.LogInformation("  ToDate: '{ToDate}'", _currentSettings.ToDate ?? "[NULL]");
                
                // If settings are empty, pre-fill with test credentials
                if (string.IsNullOrWhiteSpace(_currentSettings.Username) || string.IsNullOrWhiteSpace(_currentSettings.Password))
                {
                    _logger.LogInformation("Username or password is empty, pre-filling with test credentials");
                    _currentSettings.Username = "Mercy";
                    _currentSettings.Password = "Imani2025#";
                    _logger.LogInformation("Pre-filled form with test credentials: Username='Mercy', Password='[8 characters]'");
                }
                else
                {
                    _logger.LogInformation("Using existing credentials from database");
                }
                
                _logger.LogInformation("Loading settings to UI controls...");
                LoadSettingsToUI();
                _logger.LogInformation("Settings loaded to UI successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("=== ERROR LOADING CONFIGURATION ===");
                _logger.LogError(ex, "Failed to load SAP settings");
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            _logger.LogInformation("=== CONFIGURATION FORM LOAD COMPLETED ===");
        }

        private void LoadSettingsToUI()
        {
            _logger.LogInformation("=== LOADING SETTINGS TO UI CONTROLS ===");
            
            _logger.LogInformation("Setting txtBaseUrl.Text = '{Value}'", _currentSettings.BaseUrl ?? "[NULL]");
            txtBaseUrl.Text = _currentSettings.BaseUrl;
            
            _logger.LogInformation("Setting txtDocumentsEndpoint.Text = '{Value}'", _currentSettings.DocumentsEndpoint ?? "[NULL]");
            txtDocumentsEndpoint.Text = _currentSettings.DocumentsEndpoint;
            
            _logger.LogInformation("Setting txtUsername.Text = '{Value}'", _currentSettings.Username ?? "[NULL]");
            txtUsername.Text = _currentSettings.Username;
            
            _logger.LogInformation("Setting txtPassword.Text = '{PasswordMask}'", string.IsNullOrEmpty(_currentSettings.Password) ? "[EMPTY/NULL]" : "[" + _currentSettings.Password.Length + " characters]");
            txtPassword.Text = _currentSettings.Password; // Will be raw password for editing
            
            _logger.LogInformation("Setting txtSapClient.Text = '{Value}'", _currentSettings.SapClient ?? "[NULL]");
            txtSapClient.Text = _currentSettings.SapClient;

            // Parse dates
            _logger.LogInformation("Parsing FromDate: '{FromDate}'", _currentSettings.FromDate ?? "[NULL]");
            if (DateTime.TryParseExact(_currentSettings.FromDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fromDate))
            {
                _logger.LogInformation("FromDate parsed successfully to: {Date}", fromDate);
                dtpFromDate.Value = fromDate;
            }
            else
            {
                var defaultFromDate = DateTime.Today.AddMonths(-1);
                _logger.LogInformation("FromDate parse failed, using default: {Date}", defaultFromDate);
                dtpFromDate.Value = defaultFromDate;
            }

            _logger.LogInformation("Parsing ToDate: '{ToDate}'", _currentSettings.ToDate ?? "[NULL]");
            if (DateTime.TryParseExact(_currentSettings.ToDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime toDate))
            {
                _logger.LogInformation("ToDate parsed successfully to: {Date}", toDate);
                dtpToDate.Value = toDate;
            }
            else
            {
                var defaultToDate = DateTime.Today.AddMonths(1);
                _logger.LogInformation("ToDate parse failed, using default: {Date}", defaultToDate);
                dtpToDate.Value = defaultToDate;
            }
            
            _logger.LogInformation("=== UI CONTROLS LOADED SUCCESSFULLY ===");
        }

        private void SaveSettingsFromUI()
        {
            _logger.LogInformation("=== SAVING UI VALUES TO SETTINGS ===");
            
            _logger.LogInformation("Reading txtBaseUrl.Text = '{Value}'", txtBaseUrl.Text ?? "[NULL]");
            _currentSettings.BaseUrl = txtBaseUrl.Text.Trim();
            _logger.LogInformation("Saved BaseUrl = '{Value}'", _currentSettings.BaseUrl);
            
            _logger.LogInformation("Reading txtDocumentsEndpoint.Text = '{Value}'", txtDocumentsEndpoint.Text ?? "[NULL]");
            _currentSettings.DocumentsEndpoint = txtDocumentsEndpoint.Text.Trim();
            _logger.LogInformation("Saved DocumentsEndpoint = '{Value}'", _currentSettings.DocumentsEndpoint);
            
            _logger.LogInformation("Reading txtUsername.Text = '{Value}'", txtUsername.Text ?? "[NULL]");
            _currentSettings.Username = txtUsername.Text.Trim();
            _logger.LogInformation("Saved Username = '{Value}'", _currentSettings.Username);
            
            _logger.LogInformation("Reading txtPassword.Text = '{PasswordMask}'", string.IsNullOrEmpty(txtPassword.Text) ? "[EMPTY/NULL]" : "[" + txtPassword.Text.Length + " characters]");
            _currentSettings.Password = txtPassword.Text; // Raw password
            _logger.LogInformation("Saved Password = '{PasswordMask}'", string.IsNullOrEmpty(_currentSettings.Password) ? "[EMPTY/NULL]" : "[" + _currentSettings.Password.Length + " characters]");
            
            _logger.LogInformation("Reading txtSapClient.Text = '{Value}'", txtSapClient.Text ?? "[NULL]");
            _currentSettings.SapClient = txtSapClient.Text.Trim();
            _logger.LogInformation("Saved SapClient = '{Value}'", _currentSettings.SapClient);
            
            _logger.LogInformation("Reading dtpFromDate.Value = {Date}", dtpFromDate.Value);
            _currentSettings.FromDate = dtpFromDate.Value.ToString("yyyyMMdd");
            _logger.LogInformation("Saved FromDate = '{Value}'", _currentSettings.FromDate);
            
            _logger.LogInformation("Reading dtpToDate.Value = {Date}", dtpToDate.Value);
            _currentSettings.ToDate = dtpToDate.Value.ToString("yyyyMMdd");
            _logger.LogInformation("Saved ToDate = '{Value}'", _currentSettings.ToDate);
            
            _logger.LogInformation("=== UI VALUES SAVED TO SETTINGS SUCCESSFULLY ===");
        }

        private void LogToUI(string message)
        {
            _logger.LogInformation(message);
            // Also show in console for immediate visibility
            Console.WriteLine($"[CONFIG] {DateTime.Now:HH:mm:ss} - {message}");
        }

        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            LogToUI("=== TEST CONNECTION BUTTON CLICKED ===");
            LogToUI($"Button click event triggered at {DateTime.Now}");
            
            // Immediate feedback to show button is working
            lblConnectionStatus.Text = "🔄 Testing connection...";
            lblConnectionStatus.ForeColor = Color.Blue;
            lblConnectionStatus.BackColor = Color.LightBlue;
            btnTestConnection.Enabled = false;
            btnTestConnection.Text = "🔄 Testing...";
            
            LogToUI("UI updated - Status changed to 'Testing connection...'");
            Application.DoEvents(); // Force UI update

            try
            {
                LogToUI("=== SAVING SETTINGS FROM UI ===");
                SaveSettingsFromUI();
                
                LogToUI("Current settings after save:");
                LogToUI($"  BaseUrl: '{_currentSettings.BaseUrl}'");
                LogToUI($"  DocumentsEndpoint: '{_currentSettings.DocumentsEndpoint}'");
                LogToUI($"  Username: '{_currentSettings.Username}'");
                LogToUI($"  Password: '{(string.IsNullOrEmpty(_currentSettings.Password) ? "[EMPTY]" : "[" + _currentSettings.Password.Length + " characters]")}'");
                LogToUI($"  SapClient: '{_currentSettings.SapClient}'");
                LogToUI($"  FromDate: '{_currentSettings.FromDate}'");
                LogToUI($"  ToDate: '{_currentSettings.ToDate}'");

                LogToUI("=== USING SAP INTEGRATION SERVICE ===");
                
                // Use the SAPIntegrationService which has the proven pattern with 30-second timeout
                var sapLogger = Program.ServiceProvider?.GetService<ILogger<SAPIntegrationService>>() ?? 
                    Program.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<SAPIntegrationService>();
                var sapService = Program.ServiceProvider?.GetService<ISAPIntegrationService>() ?? new SAPIntegrationService(sapLogger ?? _logger as ILogger<SAPIntegrationService>);
                
                LogToUI("SAPIntegrationService created with proven DocumentService pattern");
                
                LogToUI("=== CALLING TEST CONNECTION METHOD ===");
                LogToUI($"Test started at: {DateTime.Now}");
                
                var result = await sapService.TestConnectionAsync(_currentSettings);
                bool isConnected = result.IsConnected;
                string message = result.Message;
                string responseContent = result.ResponseContent;
                
                LogToUI($"Test completed at: {DateTime.Now}");
                LogToUI("=== SAP SERVICE RESPONSE ===");
                LogToUI($"Is Connected: {isConnected}");
                LogToUI($"Message: '{message}'");
                LogToUI($"Response Content Length: {responseContent?.Length ?? 0} characters");
                LogToUI($"Response Content: {(responseContent?.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent ?? "No content")}");

                if (isConnected)
                {
                    lblConnectionStatus.Text = "✅ Connection successful!";
                    lblConnectionStatus.ForeColor = Color.Green;
                    lblConnectionStatus.BackColor = Color.LightGreen;
                    LogToUI("=== CONNECTION SUCCESS ===");
                    LogToUI(message);
                }
                else
                {
                    lblConnectionStatus.Text = message.Length > 50 ? message.Substring(0, 47) + "..." : message;
                    lblConnectionStatus.ForeColor = Color.Red;
                    lblConnectionStatus.BackColor = Color.LightPink;
                    LogToUI("=== CONNECTION FAILED ===");
                    LogToUI(message);
                }
            }
            catch (HttpRequestException ex)
            {
                lblConnectionStatus.Text = "❌ Network error - Check server address";
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.BackColor = Color.LightPink;
                _logger.LogError("=== NETWORK ERROR ===");
                _logger.LogError(ex, "HttpRequestException during connection test");
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
                if (ex.InnerException != null)
                    _logger.LogError("Inner Exception: {InnerException}", ex.InnerException.Message);
            }
            catch (TaskCanceledException ex)
            {
                lblConnectionStatus.Text = "❌ Connection timeout";
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.BackColor = Color.LightPink;
                _logger.LogError("=== CONNECTION TIMEOUT ===");
                _logger.LogError(ex, "TaskCanceledException - Connection timed out after 10 seconds");
                _logger.LogError("Exception Message: {Message}", ex.Message);
                if (ex.InnerException != null)
                    _logger.LogError("Inner Exception: {InnerException}", ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                lblConnectionStatus.Text = $"❌ Error: {ex.Message}";
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.BackColor = Color.LightPink;
                _logger.LogError("=== UNEXPECTED ERROR ===");
                _logger.LogError(ex, "Unexpected exception during connection test");
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                if (ex.InnerException != null)
                    _logger.LogError("Inner Exception: {InnerException}", ex.InnerException.Message);
            }
            finally
            {
                btnTestConnection.Enabled = true;
                btnTestConnection.Text = "🔗 Test Connection";
                _logger.LogInformation("=== TEST CONNECTION COMPLETED ===");
                _logger.LogInformation("Button re-enabled and text restored");
                _logger.LogInformation("Final status label text: '{StatusText}'", lblConnectionStatus.Text);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(txtBaseUrl.Text) ||
                    string.IsNullOrWhiteSpace(txtDocumentsEndpoint.Text) ||
                    string.IsNullOrWhiteSpace(txtUsername.Text) ||
                    string.IsNullOrWhiteSpace(txtPassword.Text) ||
                    string.IsNullOrWhiteSpace(txtSapClient.Text))
                {
                    MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveSettingsFromUI();

                await _configService.SaveSAPSettingsAsync(_currentSettings);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save SAP settings");
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}