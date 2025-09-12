// Forms/MainForm.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using QRCodeRegenerator.Services.Database;
using QRCodeRegenerator.Services.Processing;

namespace QRCodeRegenerator.Forms
{
    public partial class MainForm : Form
    {
        private readonly IProcessingService _processingService;
        private readonly ITransactionService _transactionService;
        private readonly IConfigService _configService;
        private readonly ILogger<MainForm> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private ProcessingOptions _currentOptions = new();

        public MainForm(
            IProcessingService processingService,
            ITransactionService transactionService,
            IConfigService configService,
            ILogger<MainForm> logger)
        {
            _processingService = processingService;
            _transactionService = transactionService;
            _configService = configService;
            _logger = logger;
            
            InitializeComponent();
            InitializeEventHandlers();
        }

        private void InitializeComponent()
        {
            // Main Form Setup
            Text = "Rainbow QR - Fiscal Document QR Code Regenerator";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            Icon = SystemIcons.Application;

            // Create main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            // Header Panel
            var headerPanel = CreateHeaderPanel();
            
            // Options Panel
            var optionsPanel = CreateOptionsPanel();
            
            // Progress Panel
            var progressPanel = CreateProgressPanel();
            
            // Log Panel
            var logPanel = CreateLogPanel();

            // Add controls to layout
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(optionsPanel, 0, 1);
            mainLayout.Controls.Add(progressPanel, 0, 2);
            mainLayout.Controls.Add(logPanel, 0, 3);

            // Set row styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(mainLayout);
        }

        // Header controls
        private Label lblTitle = new();
        private Label lblSubtitle = new();
        private Button btnConfiguration = new();

        // Options controls  
        private ComboBox cmbDuplicateHandling = new();
        private ComboBox cmbProcessingMode = new();
        private Button btnSelectRecords = new();
        private Label lblSelectedCount = new();

        // Progress controls
        private ProgressBar progressBar = new();
        private Label lblProgressText = new();
        private Label lblStats = new();
        private Button btnStart = new();
        private Button btnStop = new();
        private Button btnPause = new();

        // Log controls
        private TextBox txtLog = new();
        private Button btnClearLog = new();

        private Panel CreateHeaderPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            
            // Add subtle border
            panel.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };

            lblTitle.Text = "🌈 Rainbow QR Generator";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(31, 41, 55);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;

            lblSubtitle.Text = "Regenerate fiscal QR codes and synchronize with SAP S/4HANA system";
            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = Color.FromArgb(107, 114, 128);
            lblSubtitle.Location = new Point(20, 50);
            lblSubtitle.AutoSize = true;

            btnConfiguration.Text = "⚙️ SAP Configuration";
            btnConfiguration.Size = new Size(160, 40);
            btnConfiguration.Location = new Point(panel.Width - 180, 15);
            btnConfiguration.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConfiguration.BackColor = Color.FromArgb(59, 130, 246);
            btnConfiguration.ForeColor = Color.White;
            btnConfiguration.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnConfiguration.FlatStyle = FlatStyle.Flat;
            btnConfiguration.FlatAppearance.BorderSize = 0;
            btnConfiguration.Cursor = Cursors.Hand;

            panel.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnConfiguration });
            return panel;
        }

        private Panel CreateOptionsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(10, 5, 10, 5) };
            panel.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var titleLabel = new Label
            {
                Text = "📋 Processing Options",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(15, 10),
                AutoSize = true
            };

            var layout = new TableLayoutPanel
            {
                Location = new Point(0, 35),
                Size = new Size(panel.Width, panel.Height - 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(15, 5, 15, 15),
                BackColor = Color.Transparent
            };

            // Row 1
            var lblDuplicate = new Label { Text = "Duplicate Handling:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            layout.Controls.Add(lblDuplicate, 0, 0);
            
            cmbDuplicateHandling.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDuplicateHandling.Items.AddRange(Enum.GetNames<DuplicateHandling>());
            cmbDuplicateHandling.SelectedIndex = 1; // Default to Overwrite
            cmbDuplicateHandling.Font = new Font("Segoe UI", 9F);
            cmbDuplicateHandling.FlatStyle = FlatStyle.Flat;
            layout.Controls.Add(cmbDuplicateHandling, 1, 0);

            var lblProcessing = new Label { Text = "Processing Mode:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 85, 104) };
            layout.Controls.Add(lblProcessing, 2, 0);
            
            cmbProcessingMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProcessingMode.Items.AddRange(Enum.GetNames<ProcessingMode>());
            cmbProcessingMode.SelectedIndex = 0; // Default to BulkOperation
            cmbProcessingMode.Font = new Font("Segoe UI", 9F);
            cmbProcessingMode.FlatStyle = FlatStyle.Flat;
            layout.Controls.Add(cmbProcessingMode, 3, 0);

            // Row 2
            btnSelectRecords.Text = "🔍 Select Records";
            btnSelectRecords.Size = new Size(140, 35);
            btnSelectRecords.BackColor = Color.FromArgb(16, 185, 129);
            btnSelectRecords.ForeColor = Color.White;
            btnSelectRecords.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSelectRecords.FlatStyle = FlatStyle.Flat;
            btnSelectRecords.FlatAppearance.BorderSize = 0;
            btnSelectRecords.Cursor = Cursors.Hand;
            layout.Controls.Add(btnSelectRecords, 0, 1);

            lblSelectedCount.Text = "All records selected";
            lblSelectedCount.AutoSize = true;
            lblSelectedCount.Anchor = AnchorStyles.Left;
            layout.Controls.Add(lblSelectedCount, 1, 1);

            // Set column styles
            for (int i = 0; i < 4; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateProgressPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(10, 5, 10, 5) };
            panel.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };
            
            var titleLabel = new Label
            {
                Text = "⏱️ Processing Progress",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(15, 10),
                AutoSize = true
            };

            var layout = new TableLayoutPanel
            {
                Location = new Point(0, 35),
                Size = new Size(panel.Width, panel.Height - 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(15, 5, 15, 15),
                BackColor = Color.Transparent
            };

            // Progress bar (spans 2 columns)
            progressBar.Dock = DockStyle.Fill;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Height = 25;
            progressBar.ForeColor = Color.FromArgb(59, 130, 246);
            layout.SetColumnSpan(progressBar, 2);
            layout.Controls.Add(progressBar, 0, 0);

            // Progress text and stats
            lblProgressText.Text = "ℹ️ Ready to process fiscal documents";
            lblProgressText.AutoSize = true;
            lblProgressText.Anchor = AnchorStyles.Left;
            lblProgressText.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblProgressText.ForeColor = Color.FromArgb(107, 114, 128);
            layout.Controls.Add(lblProgressText, 0, 1);

            lblStats.Text = "📈 Total: 0 | Processed: 0 | Success: 0 | Failed: 0";
            lblStats.AutoSize = true;
            lblStats.Anchor = AnchorStyles.Right;
            lblStats.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStats.ForeColor = Color.FromArgb(74, 85, 104);
            layout.Controls.Add(lblStats, 1, 1);

            // Control buttons
            var buttonPanel = new Panel { Dock = DockStyle.Fill };
            
            btnStart.Text = "▶️ Start Processing";
            btnStart.Size = new Size(140, 40);
            btnStart.Location = new Point(0, 5);
            btnStart.BackColor = Color.FromArgb(16, 185, 129);
            btnStart.ForeColor = Color.White;
            btnStart.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Cursor = Cursors.Hand;

            btnPause.Text = "⏸️ Pause";
            btnPause.Size = new Size(90, 40);
            btnPause.Location = new Point(150, 5);
            btnPause.BackColor = Color.FromArgb(245, 158, 11);
            btnPause.ForeColor = Color.White;
            btnPause.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnPause.FlatStyle = FlatStyle.Flat;
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.Cursor = Cursors.Hand;
            btnPause.Enabled = false;

            btnStop.Text = "⏹️ Stop";
            btnStop.Size = new Size(90, 40);
            btnStop.Location = new Point(250, 5);
            btnStop.BackColor = Color.FromArgb(239, 68, 68);
            btnStop.ForeColor = Color.White;
            btnStop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Cursor = Cursors.Hand;
            btnStop.Enabled = false;

            buttonPanel.Controls.AddRange(new Control[] { btnStart, btnPause, btnStop });
            layout.SetColumnSpan(buttonPanel, 2);
            layout.Controls.Add(buttonPanel, 0, 2);

            // Set row styles
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateLogPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = SystemColors.Window };
            panel.BorderStyle = BorderStyle.FixedSingle;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // Header
            layout.Controls.Add(new Label { Text = "Processing Log:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true }, 0, 0);
            
            btnClearLog.Text = "Clear Log";
            btnClearLog.Size = new Size(80, 25);
            btnClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            layout.Controls.Add(btnClearLog, 1, 0);

            // Log text box
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Font = new Font("Consolas", 8);
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.LightGreen;
            txtLog.Dock = DockStyle.Fill;
            layout.SetColumnSpan(txtLog, 2);
            layout.Controls.Add(txtLog, 0, 1);

            // Set row styles
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            panel.Controls.Add(layout);
            return panel;
        }

        private void InitializeEventHandlers()
        {
            Load += MainForm_Load;
            btnConfiguration.Click += BtnConfiguration_Click;
            btnSelectRecords.Click += BtnSelectRecords_Click;
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnPause.Click += BtnPause_Click;
            btnClearLog.Click += BtnClearLog_Click;
            
            cmbDuplicateHandling.SelectedIndexChanged += CmbDuplicateHandling_SelectedIndexChanged;
            cmbProcessingMode.SelectedIndexChanged += CmbProcessingMode_SelectedIndexChanged;

            _processingService.ProgressChanged += ProcessingService_ProgressChanged;
            _processingService.ProcessingCompleted += ProcessingService_ProcessingCompleted;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Application started");
                await LoadTransactionCount();
            }
            catch (Exception ex)
            {
                LogMessage($"Error during initialization: {ex.Message}");
                _logger.LogError(ex, "Failed to initialize main form");
            }
        }

        private async Task LoadTransactionCount()
        {
            try
            {
                var count = await _transactionService.GetTotalTransactionCountAsync();
                lblSelectedCount.Text = $"All records selected ({count} total)";
                LogMessage($"Found {count} transactions in database");
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading transaction count: {ex.Message}");
            }
        }

        private void BtnConfiguration_Click(object sender, EventArgs e)
        {
            try
            {
                var configLogger = Program.ServiceProvider.GetService<ILogger<ConfigurationForm>>();
                using var configForm = new ConfigurationForm(_configService, configLogger);
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    LogMessage("Configuration updated successfully");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error opening configuration: {ex.Message}");
                MessageBox.Show($"Error opening configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSelectRecords_Click(object sender, EventArgs e)
        {
            try
            {
                var selectionLogger = Program.ServiceProvider.GetService<ILogger<RecordSelectionForm>>();
                using var selectionForm = new RecordSelectionForm(_transactionService, selectionLogger);
                if (selectionForm.ShowDialog() == DialogResult.OK)
                {
                    var selectedIds = selectionForm.SelectedRecordIds;
                    if (selectedIds.Any())
                    {
                        _currentOptions.ProcessAllRecords = false;
                        _currentOptions.SelectedRecordIds = selectedIds;
                        lblSelectedCount.Text = $"{selectedIds.Count} records selected";
                        LogMessage($"Selected {selectedIds.Count} specific records for processing");
                    }
                    else
                    {
                        _currentOptions.ProcessAllRecords = true;
                        _currentOptions.SelectedRecordIds = null;
                        await LoadTransactionCount();
                        LogMessage("Reset to process all records");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error selecting records: {ex.Message}");
                MessageBox.Show($"Error selecting records: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                // Update processing options
                _currentOptions.DuplicateHandling = (DuplicateHandling)cmbDuplicateHandling.SelectedIndex;
                _currentOptions.ProcessingMode = (ProcessingMode)cmbProcessingMode.SelectedIndex;

                // Reset UI
                progressBar.Value = 0;
                lblProgressText.Text = "Starting processing...";
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnPause.Enabled = true;

                LogMessage("Starting QR code regeneration process...");

                // Create cancellation token
                _cancellationTokenSource = new CancellationTokenSource();

                // Start processing
                var result = await _processingService.ProcessTransactionsAsync(_currentOptions, _cancellationTokenSource.Token);

                LogMessage($"Processing completed. Success: {result.Successful}, Failed: {result.Failed}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error during processing: {ex.Message}");
                MessageBox.Show($"Error during processing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI();
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                LogMessage("Processing stopped by user");
                ResetUI();
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping processing: {ex.Message}");
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            // TODO: Implement pause functionality if needed
            MessageBox.Show("Pause functionality not implemented yet", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void CmbDuplicateHandling_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogMessage($"Duplicate handling changed to: {cmbDuplicateHandling.SelectedItem}");
        }

        private void CmbProcessingMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogMessage($"Processing mode changed to: {cmbProcessingMode.SelectedItem}");
        }

        private void ProcessingService_ProgressChanged(object? sender, ProcessingProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => ProcessingService_ProgressChanged(sender, e));
                return;
            }

            try
            {
                // Update progress bar
                if (e.TotalRecords > 0)
                {
                    progressBar.Maximum = e.TotalRecords;
                    progressBar.Value = e.ProcessedRecords;
                }

                // Update labels
                lblProgressText.Text = $"Processing: {e.CurrentDocNum} - {e.Status}";
                lblStats.Text = $"Total: {e.TotalRecords} | Processed: {e.ProcessedRecords} | Success: {e.SuccessfulRecords} | Failed: {e.FailedRecords}";

                // Log progress
                if (e.ProcessedRecords % 10 == 0 || e.Status == "Failed")
                {
                    LogMessage($"Progress: {e.ProcessedRecords}/{e.TotalRecords} - {e.CurrentDocNum}: {e.Status}");
                }

                // Show recent errors
                foreach (var error in e.RecentErrors.TakeLast(1))
                {
                    LogMessage($"ERROR: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress UI");
            }
        }

        private void ProcessingService_ProcessingCompleted(object? sender, ProcessingCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => ProcessingService_ProcessingCompleted(sender, e));
                return;
            }

            try
            {
                ResetUI();

                var message = e.WasCancelled 
                    ? "Processing was cancelled by user"
                    : $"Processing completed!\n\nTotal: {e.Result.TotalRecords}\nSuccessful: {e.Result.Successful}\nFailed: {e.Result.Failed}";

                LogMessage(message.Replace("\n", " "));

                if (!e.WasCancelled)
                {
                    MessageBox.Show(message, "Processing Completed", MessageBoxButtons.OK, 
                        e.Result.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling processing completion");
            }
        }

        private void ResetUI()
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnPause.Enabled = false;
            lblProgressText.Text = "Ready to process";
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogMessage(message));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}\r\n");
            txtLog.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}