using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using QRCodeRegenerator.Services.Database;
using QRCodeRegenerator.Services.SAP;

namespace QRCodeRegenerator.Forms
{
    public partial class SAPDeleteTestForm : Form
    {
        private readonly IConfigService _configService;
        private readonly ILogger<SAPDeleteTestForm> _logger;
        private SAPSettings _currentSettings;

        // UI Controls
        private Label lblTitle;
        private Label lblDocNum;
        private TextBox txtDocNum;
        private Button btnLoadSettings;
        private Button btnTestDelete;
        private TextBox txtLog;
        private Label lblStatus;
        
        // Settings display
        private Label lblBaseUrl;
        private TextBox txtBaseUrl;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblSapClient;
        private TextBox txtSapClient;

        public SAPDeleteTestForm(IConfigService configService, ILogger<SAPDeleteTestForm> logger)
        {
            _configService = configService;
            _logger = logger;
            _currentSettings = new SAPSettings();
            
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            // Form properties
            Text = "SAP Delete Test Tool";
            Size = new Size(1200, 900);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(240, 242, 247);
            Font = new Font("Segoe UI", 10F);
            MinimumSize = new Size(1000, 800);

            // Create main container with proper sections
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(25)
            };

            // Header section
            var headerPanel = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(37, 99, 235),
                Margin = new Padding(0, 0, 0, 20)
            };

            lblTitle = new Label
            {
                Text = "🗑️ SAP Delete Test Tool",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblTitle);

            // Content area with cards
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 20, 0, 0)
            };

            // Settings card
            var settingsCard = CreateCard("📋 Current SAP Settings", 200);
            var settingsContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(20)
            };
            settingsContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            settingsContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            for (int i = 0; i < 4; i++)
                settingsContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Add settings controls
            AddSettingsControls(settingsContent);
            settingsCard.Controls.Add(settingsContent);

            // Test card
            var testCard = CreateCard("🧪 Delete Test", 250);
            var testContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            for (int i = 0; i < 4; i++)
                testContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AddTestControls(testContent);
            testCard.Controls.Add(testContent);

            // Log card
            var logCard = CreateCard("📋 Test Logs", 0);
            logCard.Dock = DockStyle.Fill;
            
            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = new Padding(20)
            };
            logCard.Controls.Add(txtLog);

            // Layout the cards
            contentPanel.Controls.Add(logCard);
            contentPanel.Controls.Add(testCard);
            contentPanel.Controls.Add(settingsCard);

            mainContainer.Controls.Add(contentPanel);
            mainContainer.Controls.Add(headerPanel);
            Controls.Add(mainContainer);

        }

        private Panel CreateCard(string title, int height)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = height,
                Dock = height == 0 ? DockStyle.Fill : DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };

            // Add shadow effect
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                using (var brush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(brush, rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
                }
                using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            // Card header
            var header = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            header.Controls.Add(titleLabel);
            card.Controls.Add(header);

            return card;
        }

        private void AddSettingsControls(TableLayoutPanel parent)
        {
            // Base URL
            lblBaseUrl = new Label
            {
                Text = "Base URL:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 0, 8)
            };
            parent.Controls.Add(lblBaseUrl, 0, 0);

            txtBaseUrl = new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F),
                Margin = new Padding(0, 8, 0, 8),
                Height = 30
            };
            parent.Controls.Add(txtBaseUrl, 1, 0);

            // Username
            lblUsername = new Label
            {
                Text = "Username:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 0, 8)
            };
            parent.Controls.Add(lblUsername, 0, 1);

            txtUsername = new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 8, 0, 8),
                Height = 30
            };
            parent.Controls.Add(txtUsername, 1, 1);

            // SAP Client
            lblSapClient = new Label
            {
                Text = "SAP Client:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 0, 8)
            };
            parent.Controls.Add(lblSapClient, 0, 2);

            txtSapClient = new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 8, 0, 8),
                Height = 30
            };
            parent.Controls.Add(txtSapClient, 1, 2);

            // Load Settings Button
            btnLoadSettings = new Button
            {
                Text = "🔄 Reload Settings",
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 40,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 15, 0, 0)
            };
            btnLoadSettings.FlatAppearance.BorderSize = 0;
            btnLoadSettings.Click += BtnLoadSettings_Click;
            parent.SetColumnSpan(btnLoadSettings, 2);
            parent.Controls.Add(btnLoadSettings, 0, 3);
        }

        private void AddTestControls(TableLayoutPanel parent)
        {
            // Document Number Label
            lblDocNum = new Label
            {
                Text = "Document Number:",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 10, 0, 10)
            };
            parent.Controls.Add(lblDocNum, 0, 0);

            // Document Number Input
            txtDocNum = new TextBox
            {
                PlaceholderText = "Enter document number (e.g., KE00001548)",
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11F),
                Height = 40,
                Margin = new Padding(0, 0, 0, 15)
            };
            parent.Controls.Add(txtDocNum, 0, 1);

            // Test Delete Button
            btnTestDelete = new Button
            {
                Text = "🗑️ Test Delete",
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Height = 50,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 15)
            };
            btnTestDelete.FlatAppearance.BorderSize = 0;
            btnTestDelete.Click += BtnTestDelete_Click;
            parent.Controls.Add(btnTestDelete, 0, 2);

            // Status
            lblStatus = new Label
            {
                Text = "ℹ️ Ready to test deletion",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                BackColor = Color.FromArgb(219, 234, 254),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(15),
                Height = 50,
                Margin = new Padding(0, 5, 0, 0)
            };
            parent.Controls.Add(lblStatus, 0, 3);
        }

        private async void LoadCurrentSettings()
        {
            try
            {
                LogToUI("=== LOADING SAP SETTINGS ===");
                _currentSettings = await _configService.GetSAPSettingsAsync();
                
                txtBaseUrl.Text = _currentSettings.BaseUrl ?? "";
                txtUsername.Text = _currentSettings.Username ?? "";
                txtSapClient.Text = _currentSettings.SapClient ?? "";
                
                LogToUI($"✅ Settings loaded successfully");
                LogToUI($"Base URL: {_currentSettings.BaseUrl}");
                LogToUI($"Username: {_currentSettings.Username}");
                LogToUI($"SAP Client: {_currentSettings.SapClient}");
                LogToUI($"Documents Endpoint: {_currentSettings.DocumentsEndpoint}");
                LogToUI("");
            }
            catch (Exception ex)
            {
                LogToUI($"❌ Failed to load settings: {ex.Message}");
                _logger.LogError(ex, "Failed to load SAP settings");
            }
        }

        private async void BtnLoadSettings_Click(object? sender, EventArgs e)
        {
            btnLoadSettings.Enabled = false;
            btnLoadSettings.Text = "🔄 Loading...";
            
            try
            {
                await Task.Run(() => LoadCurrentSettings());
            }
            finally
            {
                btnLoadSettings.Text = "🔄 Reload Settings";
                btnLoadSettings.Enabled = true;
            }
        }

        private async void BtnTestDelete_Click(object? sender, EventArgs e)
        {
            var docNum = txtDocNum.Text.Trim();
            
            if (string.IsNullOrEmpty(docNum))
            {
                UpdateStatus("❌ Please enter a document number", Color.FromArgb(220, 53, 69));
                return;
            }

            btnTestDelete.Enabled = false;
            btnTestDelete.Text = "🔄 Testing...";
            UpdateStatus("🔄 Testing SAP delete...", Color.FromArgb(255, 193, 7));
            
            try
            {
                LogToUI("=== SAP DELETE TEST STARTED ===");
                LogToUI($"Document Number: {docNum}");
                LogToUI($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LogToUI("");

                // Get SAP service
                var sapLogger = Program.ServiceProvider?.GetService<ILogger<SAPIntegrationService>>() ?? 
                    Program.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<SAPIntegrationService>();
                var sapService = Program.ServiceProvider?.GetService<ISAPIntegrationService>() ?? new SAPIntegrationService(sapLogger ?? _logger as ILogger<SAPIntegrationService>);
                
                LogToUI("🔧 SAPIntegrationService created");
                LogToUI($"Using settings:");
                LogToUI($"  - Base URL: {_currentSettings.BaseUrl}");
                LogToUI($"  - Documents Endpoint: {_currentSettings.DocumentsEndpoint}");
                LogToUI($"  - SAP Client: {_currentSettings.SapClient}");
                LogToUI($"  - Username: {_currentSettings.Username}");
                LogToUI("");

                // Test the delete
                LogToUI("🗑️ Calling DeleteDocumentFromSAP...");
                var deleteResult = await sapService.DeleteDocumentFromSAP(docNum, _currentSettings);
                
                LogToUI($"📊 Delete Result: {deleteResult}");
                
                if (deleteResult)
                {
                    LogToUI("✅ DELETE SUCCESSFUL!");
                    UpdateStatus($"✅ Successfully deleted document {docNum}", Color.FromArgb(40, 167, 69));
                }
                else
                {
                    LogToUI("❌ DELETE FAILED!");
                    UpdateStatus($"❌ Failed to delete document {docNum}", Color.FromArgb(220, 53, 69));
                }

                LogToUI("");
                LogToUI("=== DELETE TEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                LogToUI($"💥 EXCEPTION OCCURRED: {ex.Message}");
                LogToUI($"Stack Trace: {ex.StackTrace}");
                UpdateStatus("❌ Exception during delete test", Color.FromArgb(220, 53, 69));
                _logger.LogError(ex, "Exception during SAP delete test for document {DocNum}", docNum);
            }
            finally
            {
                btnTestDelete.Text = "🗑️ Test Delete";
                btnTestDelete.Enabled = true;
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.BackColor = Color.FromArgb(
                Math.Min(255, color.R + 40), 
                Math.Min(255, color.G + 40), 
                Math.Min(255, color.B + 40)
            );
            lblStatus.ForeColor = color;
        }

        private void LogToUI(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";
            
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => 
                {
                    txtLog.AppendText(logMessage + Environment.NewLine);
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }));
            }
            else
            {
                txtLog.AppendText(logMessage + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            
            // Also log to console for immediate visibility
            Console.WriteLine($"[SAP-DELETE-TEST] {logMessage}");
            _logger.LogInformation(message);
        }
    }
}