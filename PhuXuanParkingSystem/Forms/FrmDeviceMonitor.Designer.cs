namespace PhuXuanParkingSystem.Forms
{
    partial class FrmDeviceMonitor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pnlStats = new System.Windows.Forms.Panel();
            this.pnlStatOffline = new System.Windows.Forms.Panel();
            this.lblStatOfflineVal = new System.Windows.Forms.Label();
            this.lblStatOfflineTitle = new System.Windows.Forms.Label();
            this.pnlStatOnline = new System.Windows.Forms.Panel();
            this.lblStatOnlineVal = new System.Windows.Forms.Label();
            this.lblStatOnlineTitle = new System.Windows.Forms.Label();
            this.pnlStatTotal = new System.Windows.Forms.Panel();
            this.lblStatTotalVal = new System.Windows.Forms.Label();
            this.lblStatTotalTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.lblAutoCheck = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cboAutoCheckInterval = new System.Windows.Forms.ComboBox();
            this.btnCheckSelected = new System.Windows.Forms.Button();
            this.btnCheckAll = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.prgStatus = new System.Windows.Forms.ProgressBar();
            this.lblFooterStatus = new System.Windows.Forms.Label();
            this.dgvDevices = new System.Windows.Forms.DataGridView();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLastHeartbeat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetails = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRestart = new System.Windows.Forms.DataGridViewButtonColumn();
            this.timerAutoCheck = new System.Windows.Forms.Timer(this.components);
            this.object_d707e60d_817d_41b4_834f_32184a011f04 = new System.Windows.Forms.Panel();
            this.object_0b722309_7b29_4322_943c_db2f925068ab = new System.Windows.Forms.Panel();
            this.pnlHeader.SuspendLayout();
            this.pnlStats.SuspendLayout();
            this.pnlStatOffline.SuspendLayout();
            this.pnlStatOnline.SuspendLayout();
            this.pnlStatTotal.SuspendLayout();
            this.pnlControls.SuspendLayout();
            this.panel1.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDevices)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.Teal;
            this.pnlHeader.Controls.Add(this.pnlStats);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.pnlHeader.Size = new System.Drawing.Size(1262, 102);
            this.pnlHeader.TabIndex = 0;
            // 
            // pnlStats
            // 
            this.pnlStats.Controls.Add(this.pnlStatOffline);
            this.pnlStats.Controls.Add(this.pnlStatOnline);
            this.pnlStats.Controls.Add(this.pnlStatTotal);
            this.pnlStats.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlStats.Location = new System.Drawing.Point(842, 12);
            this.pnlStats.Name = "pnlStats";
            this.pnlStats.Size = new System.Drawing.Size(404, 78);
            this.pnlStats.TabIndex = 2;
            // 
            // pnlStatOffline
            // 
            this.pnlStatOffline.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(20)))), ((int)(((byte)(25)))));
            this.pnlStatOffline.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlStatOffline.Controls.Add(this.lblStatOfflineVal);
            this.pnlStatOffline.Controls.Add(this.lblStatOfflineTitle);
            this.pnlStatOffline.Location = new System.Drawing.Point(272, 3);
            this.pnlStatOffline.Name = "pnlStatOffline";
            this.pnlStatOffline.Size = new System.Drawing.Size(125, 55);
            this.pnlStatOffline.TabIndex = 2;
            // 
            // lblStatOfflineVal
            // 
            this.lblStatOfflineVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatOfflineVal.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatOfflineVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblStatOfflineVal.Location = new System.Drawing.Point(0, 18);
            this.lblStatOfflineVal.Name = "lblStatOfflineVal";
            this.lblStatOfflineVal.Size = new System.Drawing.Size(123, 35);
            this.lblStatOfflineVal.TabIndex = 1;
            this.lblStatOfflineVal.Text = "0";
            this.lblStatOfflineVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStatOfflineTitle
            // 
            this.lblStatOfflineTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatOfflineTitle.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblStatOfflineTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(140)))), ((int)(((byte)(140)))));
            this.lblStatOfflineTitle.Location = new System.Drawing.Point(0, 0);
            this.lblStatOfflineTitle.Name = "lblStatOfflineTitle";
            this.lblStatOfflineTitle.Size = new System.Drawing.Size(123, 18);
            this.lblStatOfflineTitle.TabIndex = 0;
            this.lblStatOfflineTitle.Text = "MẤT KẾT NỐI";
            this.lblStatOfflineTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlStatOnline
            // 
            this.pnlStatOnline.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(45)))), ((int)(((byte)(30)))));
            this.pnlStatOnline.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlStatOnline.Controls.Add(this.lblStatOnlineVal);
            this.pnlStatOnline.Controls.Add(this.lblStatOnlineTitle);
            this.pnlStatOnline.Location = new System.Drawing.Point(138, 3);
            this.pnlStatOnline.Name = "pnlStatOnline";
            this.pnlStatOnline.Size = new System.Drawing.Size(125, 55);
            this.pnlStatOnline.TabIndex = 1;
            // 
            // lblStatOnlineVal
            // 
            this.lblStatOnlineVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatOnlineVal.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatOnlineVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(220)))), ((int)(((byte)(140)))));
            this.lblStatOnlineVal.Location = new System.Drawing.Point(0, 18);
            this.lblStatOnlineVal.Name = "lblStatOnlineVal";
            this.lblStatOnlineVal.Size = new System.Drawing.Size(123, 35);
            this.lblStatOnlineVal.TabIndex = 1;
            this.lblStatOnlineVal.Text = "0";
            this.lblStatOnlineVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStatOnlineTitle
            // 
            this.lblStatOnlineTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatOnlineTitle.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblStatOnlineTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(200)))), ((int)(((byte)(150)))));
            this.lblStatOnlineTitle.Location = new System.Drawing.Point(0, 0);
            this.lblStatOnlineTitle.Name = "lblStatOnlineTitle";
            this.lblStatOnlineTitle.Size = new System.Drawing.Size(123, 18);
            this.lblStatOnlineTitle.TabIndex = 0;
            this.lblStatOnlineTitle.Text = "ĐANG KẾT NỐI";
            this.lblStatOnlineTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlStatTotal
            // 
            this.pnlStatTotal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(40)))), ((int)(((byte)(65)))));
            this.pnlStatTotal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlStatTotal.Controls.Add(this.lblStatTotalVal);
            this.pnlStatTotal.Controls.Add(this.lblStatTotalTitle);
            this.pnlStatTotal.Location = new System.Drawing.Point(4, 3);
            this.pnlStatTotal.Name = "pnlStatTotal";
            this.pnlStatTotal.Size = new System.Drawing.Size(125, 55);
            this.pnlStatTotal.TabIndex = 0;
            // 
            // lblStatTotalVal
            // 
            this.lblStatTotalVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatTotalVal.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatTotalVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblStatTotalVal.Location = new System.Drawing.Point(0, 18);
            this.lblStatTotalVal.Name = "lblStatTotalVal";
            this.lblStatTotalVal.Size = new System.Drawing.Size(123, 35);
            this.lblStatTotalVal.TabIndex = 1;
            this.lblStatTotalVal.Text = "0";
            this.lblStatTotalVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStatTotalTitle
            // 
            this.lblStatTotalTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatTotalTitle.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblStatTotalTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(190)))), ((int)(((byte)(240)))));
            this.lblStatTotalTitle.Location = new System.Drawing.Point(0, 0);
            this.lblStatTotalTitle.Name = "lblStatTotalTitle";
            this.lblStatTotalTitle.Size = new System.Drawing.Size(123, 18);
            this.lblStatTotalTitle.TabIndex = 0;
            this.lblStatTotalTitle.Text = "TỔNG THIẾT BỊ";
            this.lblStatTotalTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.White;
            this.lblSubtitle.Location = new System.Drawing.Point(12, 48);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(308, 25);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Kiểm tra kết nối Camera IP, Controller";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(9, 7);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(486, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "TRUNG TÂM GIÁM SÁT THIẾT BỊ PHẦN CỨNG";
            // 
            // pnlControls
            // 
            this.pnlControls.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.pnlControls.Controls.Add(this.lblAutoCheck);
            this.pnlControls.Controls.Add(this.panel1);
            this.pnlControls.Controls.Add(this.btnCheckSelected);
            this.pnlControls.Controls.Add(this.btnCheckAll);
            this.pnlControls.Controls.Add(this.btnClose);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlControls.Location = new System.Drawing.Point(0, 102);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.pnlControls.Size = new System.Drawing.Size(1262, 50);
            this.pnlControls.TabIndex = 1;
            // 
            // lblAutoCheck
            // 
            this.lblAutoCheck.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblAutoCheck.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAutoCheck.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(70)))), ((int)(((byte)(85)))));
            this.lblAutoCheck.Location = new System.Drawing.Point(739, 8);
            this.lblAutoCheck.Name = "lblAutoCheck";
            this.lblAutoCheck.Size = new System.Drawing.Size(226, 34);
            this.lblAutoCheck.TabIndex = 4;
            this.lblAutoCheck.Text = "Tự động kiểm tra định kỳ:";
            this.lblAutoCheck.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cboAutoCheckInterval);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(965, 8);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.panel1.Size = new System.Drawing.Size(185, 34);
            this.panel1.TabIndex = 5;
            // 
            // cboAutoCheckInterval
            // 
            this.cboAutoCheckInterval.Dock = System.Windows.Forms.DockStyle.Right;
            this.cboAutoCheckInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAutoCheckInterval.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboAutoCheckInterval.FormattingEnabled = true;
            this.cboAutoCheckInterval.Items.AddRange(new object[] {
            "Mỗi 10 giây",
            "Mỗi 30 giây (Mặc định)",
            "Mỗi 1 phút",
            "Mỗi 5 phút",
            "Tắt tự động"});
            this.cboAutoCheckInterval.Location = new System.Drawing.Point(15, 0);
            this.cboAutoCheckInterval.Name = "cboAutoCheckInterval";
            this.cboAutoCheckInterval.Size = new System.Drawing.Size(160, 33);
            this.cboAutoCheckInterval.TabIndex = 3;
            this.cboAutoCheckInterval.SelectedIndexChanged += new System.EventHandler(this.CboAutoCheckInterval_SelectedIndexChanged);
            // 
            // btnCheckSelected
            // 
            this.btnCheckSelected.BackColor = System.Drawing.Color.White;
            this.btnCheckSelected.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCheckSelected.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCheckSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheckSelected.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCheckSelected.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(60)))), ((int)(((byte)(75)))));
            this.btnCheckSelected.Location = new System.Drawing.Point(267, 8);
            this.btnCheckSelected.Name = "btnCheckSelected";
            this.btnCheckSelected.Size = new System.Drawing.Size(174, 34);
            this.btnCheckSelected.TabIndex = 2;
            this.btnCheckSelected.Text = "⚡ Kiểm Tra";
            this.btnCheckSelected.UseVisualStyleBackColor = false;
            this.btnCheckSelected.Click += new System.EventHandler(this.BtnCheckSelected_Click);
            // 
            // btnCheckAll
            // 
            this.btnCheckAll.BackColor = System.Drawing.Color.Teal;
            this.btnCheckAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCheckAll.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCheckAll.FlatAppearance.BorderSize = 0;
            this.btnCheckAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheckAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCheckAll.ForeColor = System.Drawing.Color.White;
            this.btnCheckAll.Location = new System.Drawing.Point(16, 8);
            this.btnCheckAll.Name = "btnCheckAll";
            this.btnCheckAll.Size = new System.Drawing.Size(251, 34);
            this.btnCheckAll.TabIndex = 1;
            this.btnCheckAll.Text = "🔄 Kiểm Tra Tất Cả (F5)";
            this.btnCheckAll.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCheckAll.UseVisualStyleBackColor = false;
            this.btnCheckAll.Click += new System.EventHandler(this.BtnCheckAll_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.Tomato;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(1150, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(96, 34);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Đóng (Esc)";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(243)))), ((int)(((byte)(248)))));
            this.pnlFooter.Controls.Add(this.prgStatus);
            this.pnlFooter.Controls.Add(this.lblFooterStatus);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 598);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(16, 6, 16, 6);
            this.pnlFooter.Size = new System.Drawing.Size(1262, 35);
            this.pnlFooter.TabIndex = 2;
            // 
            // prgStatus
            // 
            this.prgStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.prgStatus.Location = new System.Drawing.Point(1046, 9);
            this.prgStatus.Name = "prgStatus";
            this.prgStatus.Size = new System.Drawing.Size(200, 16);
            this.prgStatus.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.prgStatus.TabIndex = 1;
            this.prgStatus.Visible = false;
            // 
            // lblFooterStatus
            // 
            this.lblFooterStatus.AutoSize = true;
            this.lblFooterStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFooterStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(80)))), ((int)(((byte)(95)))));
            this.lblFooterStatus.Location = new System.Drawing.Point(16, 10);
            this.lblFooterStatus.Name = "lblFooterStatus";
            this.lblFooterStatus.Size = new System.Drawing.Size(174, 25);
            this.lblFooterStatus.TabIndex = 0;
            this.lblFooterStatus.Text = "Hệ thống sẵn sàng...";
            // 
            // dgvDevices
            // 
            this.dgvDevices.AllowUserToAddRows = false;
            this.dgvDevices.AllowUserToDeleteRows = false;
            this.dgvDevices.AllowUserToResizeRows = false;
            this.dgvDevices.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDevices.BackgroundColor = System.Drawing.Color.White;
            this.dgvDevices.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvDevices.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvDevices.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(60)))), ((int)(((byte)(75)))));
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(6);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(60)))), ((int)(((byte)(75)))));
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvDevices.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvDevices.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDevices.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colStatus,
            this.colType,
            this.colCode,
            this.colName,
            this.colIp,
            this.colPort,
            this.colLatency,
            this.colLastHeartbeat,
            this.colDetails,
            this.colRestart});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(40)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvDevices.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvDevices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDevices.EnableHeadersVisualStyles = false;
            this.dgvDevices.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(235)))), ((int)(((byte)(242)))));
            this.dgvDevices.Location = new System.Drawing.Point(0, 152);
            this.dgvDevices.MultiSelect = false;
            this.dgvDevices.Name = "dgvDevices";
            this.dgvDevices.ReadOnly = true;
            this.dgvDevices.RowHeadersVisible = false;
            this.dgvDevices.RowHeadersWidth = 62;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dgvDevices.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvDevices.RowTemplate.Height = 36;
            this.dgvDevices.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDevices.Size = new System.Drawing.Size(1262, 446);
            this.dgvDevices.TabIndex = 3;
            this.dgvDevices.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvDevices_CellContentClick);
            // 
            // colStatus
            // 
            this.colStatus.FillWeight = 110F;
            this.colStatus.HeaderText = "Trạng Thái";
            this.colStatus.MinimumWidth = 8;
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            // 
            // colType
            // 
            this.colType.FillWeight = 110F;
            this.colType.HeaderText = "Loại Thiết Bị";
            this.colType.MinimumWidth = 8;
            this.colType.Name = "colType";
            this.colType.ReadOnly = true;
            // 
            // colCode
            // 
            this.colCode.FillWeight = 90F;
            this.colCode.HeaderText = "Mã Thiết Bị";
            this.colCode.MinimumWidth = 8;
            this.colCode.Name = "colCode";
            this.colCode.ReadOnly = true;
            // 
            // colName
            // 
            this.colName.FillWeight = 150F;
            this.colName.HeaderText = "Tên Thiết Bị";
            this.colName.MinimumWidth = 8;
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            // 
            // colIp
            // 
            this.colIp.FillWeight = 110F;
            this.colIp.HeaderText = "Địa Chỉ IP";
            this.colIp.MinimumWidth = 8;
            this.colIp.Name = "colIp";
            this.colIp.ReadOnly = true;
            // 
            // colPort
            // 
            this.colPort.FillWeight = 60F;
            this.colPort.HeaderText = "Cổng";
            this.colPort.MinimumWidth = 8;
            this.colPort.Name = "colPort";
            this.colPort.ReadOnly = true;
            // 
            // colLatency
            // 
            this.colLatency.FillWeight = 70F;
            this.colLatency.HeaderText = "Độ Trễ";
            this.colLatency.MinimumWidth = 8;
            this.colLatency.Name = "colLatency";
            this.colLatency.ReadOnly = true;
            // 
            // colLastHeartbeat
            // 
            this.colLastHeartbeat.FillWeight = 120F;
            this.colLastHeartbeat.HeaderText = "Phản Hồi Cuối";
            this.colLastHeartbeat.MinimumWidth = 8;
            this.colLastHeartbeat.Name = "colLastHeartbeat";
            this.colLastHeartbeat.ReadOnly = true;
            // 
            // colDetails
            // 
            this.colDetails.FillWeight = 150F;
            this.colDetails.HeaderText = "Chi Tiết Kết Nối";
            this.colDetails.MinimumWidth = 8;
            this.colDetails.Name = "colDetails";
            this.colDetails.ReadOnly = true;
            // 
            // colRestart
            // 
            this.colRestart.FillWeight = 80F;
            this.colRestart.HeaderText = "Khởi Động Lại";
            this.colRestart.MinimumWidth = 8;
            this.colRestart.Name = "colRestart";
            this.colRestart.ReadOnly = true;
            this.colRestart.Text = "🔄 Restart";
            this.colRestart.UseColumnTextForButtonValue = true;
            // 
            // timerAutoCheck
            // 
            this.timerAutoCheck.Interval = 30000;
            this.timerAutoCheck.Tick += new System.EventHandler(this.TimerAutoCheck_Tick);
            // 
            // object_d707e60d_817d_41b4_834f_32184a011f04
            // 
            this.object_d707e60d_817d_41b4_834f_32184a011f04.Dock = System.Windows.Forms.DockStyle.Right;
            this.object_d707e60d_817d_41b4_834f_32184a011f04.Location = new System.Drawing.Point(842, 12);
            this.object_d707e60d_817d_41b4_834f_32184a011f04.Name = "object_d707e60d_817d_41b4_834f_32184a011f04";
            this.object_d707e60d_817d_41b4_834f_32184a011f04.Size = new System.Drawing.Size(404, 61);
            this.object_d707e60d_817d_41b4_834f_32184a011f04.TabIndex = 2;
            // 
            // object_0b722309_7b29_4322_943c_db2f925068ab
            // 
            this.object_0b722309_7b29_4322_943c_db2f925068ab.Dock = System.Windows.Forms.DockStyle.Right;
            this.object_0b722309_7b29_4322_943c_db2f925068ab.Location = new System.Drawing.Point(842, 12);
            this.object_0b722309_7b29_4322_943c_db2f925068ab.Name = "object_0b722309_7b29_4322_943c_db2f925068ab";
            this.object_0b722309_7b29_4322_943c_db2f925068ab.Size = new System.Drawing.Size(404, 61);
            this.object_0b722309_7b29_4322_943c_db2f925068ab.TabIndex = 2;
            // 
            // FrmDeviceMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1262, 633);
            this.Controls.Add(this.dgvDevices);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(850, 480);
            this.Name = "FrmDeviceMonitor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HP Parking - Trung Tâm Giám Sát Thiết Bị Phần Cứng";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmDeviceMonitor_FormClosing);
            this.Load += new System.EventHandler(this.FrmDeviceMonitor_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmDeviceMonitor_KeyDown);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlStats.ResumeLayout(false);
            this.pnlStatOffline.ResumeLayout(false);
            this.pnlStatOnline.ResumeLayout(false);
            this.pnlStatTotal.ResumeLayout(false);
            this.pnlControls.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDevices)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel pnlStats;
        private System.Windows.Forms.Panel pnlStatTotal;
        private System.Windows.Forms.Label lblStatTotalVal;
        private System.Windows.Forms.Label lblStatTotalTitle;
        private System.Windows.Forms.Panel pnlStatOnline;
        private System.Windows.Forms.Label lblStatOnlineVal;
        private System.Windows.Forms.Label lblStatOnlineTitle;
        private System.Windows.Forms.Panel pnlStatOffline;
        private System.Windows.Forms.Label lblStatOfflineVal;
        private System.Windows.Forms.Label lblStatOfflineTitle;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnCheckAll;
        private System.Windows.Forms.Button btnCheckSelected;
        private System.Windows.Forms.Label lblAutoCheck;
        private System.Windows.Forms.ComboBox cboAutoCheckInterval;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Label lblFooterStatus;
        private System.Windows.Forms.ProgressBar prgStatus;
        private System.Windows.Forms.DataGridView dgvDevices;
        private System.Windows.Forms.Timer timerAutoCheck;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLatency;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLastHeartbeat;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetails;
        private System.Windows.Forms.DataGridViewButtonColumn colRestart;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel object_d707e60d_817d_41b4_834f_32184a011f04;
        private System.Windows.Forms.Panel object_0b722309_7b29_4322_943c_db2f925068ab;
    }
}
