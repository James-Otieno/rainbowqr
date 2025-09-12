// Forms/RecordSelectionForm.cs
using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Models;
using QRCodeRegenerator.Services.Database;

namespace QRCodeRegenerator.Forms
{
    public partial class RecordSelectionForm : Form
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<RecordSelectionForm> _logger;
        private List<TransactionRecord> _allTransactions = new();

        public List<int> SelectedRecordIds { get; private set; } = new();

        public RecordSelectionForm(ITransactionService transactionService, ILogger<RecordSelectionForm> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
            InitializeComponent();
        }

        // Controls
        private DataGridView dgvTransactions = new();
        private Button btnSelectAll = new();
        private Button btnSelectNone = new();
        private Button btnOK = new();
        private Button btnCancel = new();
        private Label lblSelectedCount = new();
        private TextBox txtSearch = new();
        private ComboBox cmbFilter = new();

        private void InitializeComponent()
        {
            Text = "Select Records to Process";
            Size = new Size(1200, 700);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 500);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            // Filter Panel
            var filterPanel = CreateFilterPanel();
            mainLayout.Controls.Add(filterPanel, 0, 0);

            // Data Grid
            CreateDataGrid();
            mainLayout.Controls.Add(dgvTransactions, 0, 1);

            // Selection Info Panel
            var infoPanel = CreateInfoPanel();
            mainLayout.Controls.Add(infoPanel, 0, 2);

            // Button Panel
            var buttonPanel = CreateButtonPanel();
            mainLayout.Controls.Add(buttonPanel, 0, 3);

            // Set row styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Filter
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Grid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Info
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Buttons

            Controls.Add(mainLayout);

            Load += RecordSelectionForm_Load;
        }

        private Panel CreateFilterPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(5)
            };

            layout.Controls.Add(new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText = "Enter TsNum or other criteria...";
            txtSearch.TextChanged += TxtSearch_TextChanged;
            layout.Controls.Add(txtSearch, 1, 0);

            layout.Controls.Add(new Label { Text = "Filter:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
            
            cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilter.Items.AddRange(new string[] { "All Records", "Has QR Code", "Missing QR Code", "Recent (Last 30 days)" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.Dock = DockStyle.Fill;
            cmbFilter.SelectedIndexChanged += CmbFilter_SelectedIndexChanged;
            layout.Controls.Add(cmbFilter, 3, 0);

            // Set column styles
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            panel.Controls.Add(layout);
            return panel;
        }

        private void CreateDataGrid()
        {
            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.AllowUserToAddRows = false;
            dgvTransactions.AllowUserToDeleteRows = false;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.MultiSelect = true;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.RowHeadersVisible = false;

            // Add checkbox column
            var checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "Select",
                Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            dgvTransactions.Columns.Add(checkColumn);

            // Add data columns
            dgvTransactions.Columns.Add("Id", "ID");
            dgvTransactions.Columns["Id"].Width = 60;
            dgvTransactions.Columns["Id"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgvTransactions.Columns.Add("TsNum", "Document Number");
            dgvTransactions.Columns.Add("Date", "Date");
            dgvTransactions.Columns["Date"].Width = 100;
            dgvTransactions.Columns["Date"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgvTransactions.Columns.Add("TotalAmount", "Total Amount");
            dgvTransactions.Columns["TotalAmount"].Width = 100;
            dgvTransactions.Columns["TotalAmount"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgvTransactions.Columns.Add("ControlCode", "Control Code");
            dgvTransactions.Columns.Add("SerialNumber", "Serial Number");
            dgvTransactions.Columns.Add("QrCode", "QR Code URL");

            dgvTransactions.CellValueChanged += DgvTransactions_CellValueChanged;
        }

        private Panel CreateInfoPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };

            btnSelectAll.Text = "Select All";
            btnSelectAll.Size = new Size(100, 30);
            btnSelectAll.Click += BtnSelectAll_Click;
            layout.Controls.Add(btnSelectAll, 0, 0);

            btnSelectNone.Text = "Select None";
            btnSelectNone.Size = new Size(100, 30);
            btnSelectNone.Click += BtnSelectNone_Click;
            layout.Controls.Add(btnSelectNone, 1, 0);

            lblSelectedCount.Text = "0 records selected";
            lblSelectedCount.AutoSize = true;
            lblSelectedCount.Anchor = AnchorStyles.Right;
            layout.Controls.Add(lblSelectedCount, 2, 0);

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            btnOK.Text = "OK";
            btnOK.Size = new Size(100, 35);
            btnOK.Location = new Point(panel.Width - 220, 12);
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.BackColor = Color.LightGreen;
            btnOK.Click += BtnOK_Click;

            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 35);
            btnCancel.Location = new Point(panel.Width - 110, 12);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;

            panel.Controls.AddRange(new Control[] { btnOK, btnCancel });
            return panel;
        }

        private async void RecordSelectionForm_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadTransactions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load transactions");
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadTransactions()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                dgvTransactions.Rows.Clear();

                _allTransactions = await _transactionService.GetAllTransactionsAsync();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load transactions");
                throw;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ApplyFilters()
        {
            dgvTransactions.Rows.Clear();

            var filteredTransactions = _allTransactions.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var searchText = txtSearch.Text.ToLower();
                filteredTransactions = filteredTransactions.Where(t =>
                    (t.TsNum?.ToLower().Contains(searchText) ?? false) ||
                    (t.ControlCode?.ToLower().Contains(searchText) ?? false) ||
                    (t.SerialNumber?.ToLower().Contains(searchText) ?? false));
            }

            // Apply dropdown filter
            switch (cmbFilter.SelectedIndex)
            {
                case 1: // Has QR Code
                    filteredTransactions = filteredTransactions.Where(t => !string.IsNullOrEmpty(t.QrCode));
                    break;
                case 2: // Missing QR Code
                    filteredTransactions = filteredTransactions.Where(t => string.IsNullOrEmpty(t.QrCode));
                    break;
                case 3: // Recent (Last 30 days)
                    var cutoffDate = DateTime.Now.AddDays(-30);
                    filteredTransactions = filteredTransactions.Where(t => t.Date >= cutoffDate);
                    break;
            }

            foreach (var transaction in filteredTransactions.OrderBy(t => t.Id))
            {
                var rowIndex = dgvTransactions.Rows.Add(
                    false, // Selected checkbox
                    transaction.Id,
                    transaction.TsNum,
                    transaction.Date?.ToString("yyyy-MM-dd") ?? "",
                    transaction.TotalAmount.ToString("F2"),
                    transaction.ControlCode ?? "",
                    transaction.SerialNumber ?? "",
                    string.IsNullOrEmpty(transaction.QrCode) ? "(Missing)" : "Available"
                );

                dgvTransactions.Rows[rowIndex].Tag = transaction;

                // Color code rows based on QR code availability
                if (string.IsNullOrEmpty(transaction.QrCode))
                {
                    dgvTransactions.Rows[rowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
                }
            }

            UpdateSelectedCount();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void CmbFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                row.Cells["Selected"].Value = true;
            }
            UpdateSelectedCount();
        }

        private void BtnSelectNone_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                row.Cells["Selected"].Value = false;
            }
            UpdateSelectedCount();
        }

        private void DgvTransactions_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0) // Selected column
            {
                UpdateSelectedCount();
            }
        }

        private void UpdateSelectedCount()
        {
            var selectedCount = dgvTransactions.Rows.Cast<DataGridViewRow>()
                .Count(row => Convert.ToBoolean(row.Cells["Selected"].Value));

            lblSelectedCount.Text = $"{selectedCount} records selected";
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SelectedRecordIds = dgvTransactions.Rows.Cast<DataGridViewRow>()
                .Where(row => Convert.ToBoolean(row.Cells["Selected"].Value))
                .Select(row => Convert.ToInt32(row.Cells["Id"].Value))
                .ToList();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}