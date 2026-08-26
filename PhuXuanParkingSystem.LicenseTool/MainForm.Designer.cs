namespace PhuXuanParkingSystem.LicenseTool
{
    partial class MainForm
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
            tabControlMain = new TabControl();
            tabGenerate = new TabPage();
            grpOutput = new GroupBox();
            btnExportLicFile = new Button();
            btnCopyKey = new Button();
            txtGeneratedKey = new TextBox();
            lblStatusMessage = new Label();
            btnGenerateKey = new Button();
            grpLimits = new GroupBox();
            chkAdvancedReport = new CheckBox();
            chkDualCamera = new CheckBox();
            chkBarrier = new CheckBox();
            chkAnpr = new CheckBox();
            numMaxControllers = new NumericUpDown();
            lblMaxControllers = new Label();
            numMaxCameras = new NumericUpDown();
            lblMaxCameras = new Label();
            numMaxLanes = new NumericUpDown();
            lblMaxLanes = new Label();
            grpDuration = new GroupBox();
            dtpExpiryDate = new DateTimePicker();
            radCustom = new RadioButton();
            radPermanent = new RadioButton();
            rad3Years = new RadioButton();
            rad1Year = new RadioButton();
            rad90Days = new RadioButton();
            rad30Days = new RadioButton();
            grpCustomer = new GroupBox();
            txtNote = new TextBox();
            lblNote = new Label();
            btnPasteMachineCode = new Button();
            btnGetThisMachineCode = new Button();
            txtMachineCode = new TextBox();
            lblMachineCode = new Label();
            txtCustomerName = new TextBox();
            lblCustomerName = new Label();
            tabVerify = new TabPage();
            grpDecodedResult = new GroupBox();
            txtDecodedInfo = new TextBox();
            grpInputKey = new GroupBox();
            btnOpenLicFile = new Button();
            btnVerifyInputKey = new Button();
            txtVerifyMachineCode = new TextBox();
            lblVerifyMachineCode = new Label();
            txtVerifyKey = new TextBox();
            lblVerifyKey = new Label();
            pnlHeader = new Panel();
            lblKeyStatus = new Label();
            lblSubTitle = new Label();
            lblTitle = new Label();
            tabControlMain.SuspendLayout();
            tabGenerate.SuspendLayout();
            grpOutput.SuspendLayout();
            grpLimits.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxControllers).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxCameras).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxLanes).BeginInit();
            grpDuration.SuspendLayout();
            grpCustomer.SuspendLayout();
            tabVerify.SuspendLayout();
            grpDecodedResult.SuspendLayout();
            grpInputKey.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabGenerate);
            tabControlMain.Controls.Add(tabVerify);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Font = new Font("Segoe UI", 9.5F);
            tabControlMain.Location = new Point(0, 108);
            tabControlMain.Margin = new Padding(4, 5, 4, 5);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1263, 799);
            tabControlMain.TabIndex = 0;
            // 
            // tabGenerate
            // 
            tabGenerate.BackColor = Color.FromArgb(248, 250, 252);
            tabGenerate.Controls.Add(grpOutput);
            tabGenerate.Controls.Add(btnGenerateKey);
            tabGenerate.Controls.Add(grpLimits);
            tabGenerate.Controls.Add(grpDuration);
            tabGenerate.Controls.Add(grpCustomer);
            tabGenerate.Location = new Point(4, 34);
            tabGenerate.Margin = new Padding(4, 5, 4, 5);
            tabGenerate.Name = "tabGenerate";
            tabGenerate.Padding = new Padding(17, 20, 17, 20);
            tabGenerate.Size = new Size(1255, 904);
            tabGenerate.TabIndex = 0;
            tabGenerate.Text = "  🔑 Phát Hành License Key  ";
            // 
            // grpOutput
            // 
            grpOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpOutput.Controls.Add(btnExportLicFile);
            grpOutput.Controls.Add(btnCopyKey);
            grpOutput.Controls.Add(txtGeneratedKey);
            grpOutput.Controls.Add(lblStatusMessage);
            grpOutput.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpOutput.ForeColor = Color.FromArgb(15, 23, 42);
            grpOutput.Location = new Point(17, 667);
            grpOutput.Margin = new Padding(4, 5, 4, 5);
            grpOutput.Name = "grpOutput";
            grpOutput.Padding = new Padding(4, 5, 4, 5);
            grpOutput.Size = new Size(1217, 205);
            grpOutput.TabIndex = 4;
            grpOutput.TabStop = false;
            grpOutput.Text = "3. Chuỗi License Key Đã Tạo";
            // 
            // btnExportLicFile
            // 
            btnExportLicFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportLicFile.BackColor = Color.FromArgb(16, 185, 129);
            btnExportLicFile.Cursor = Cursors.Hand;
            btnExportLicFile.FlatStyle = FlatStyle.Flat;
            btnExportLicFile.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnExportLicFile.ForeColor = Color.White;
            btnExportLicFile.Location = new Point(1017, 100);
            btnExportLicFile.Margin = new Padding(4, 5, 4, 5);
            btnExportLicFile.Name = "btnExportLicFile";
            btnExportLicFile.Size = new Size(186, 53);
            btnExportLicFile.TabIndex = 3;
            btnExportLicFile.Text = "💾 Xuất File .lic";
            btnExportLicFile.UseVisualStyleBackColor = false;
            btnExportLicFile.Click += btnExportLicFile_Click;
            // 
            // btnCopyKey
            // 
            btnCopyKey.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyKey.BackColor = Color.FromArgb(37, 99, 235);
            btnCopyKey.Cursor = Cursors.Hand;
            btnCopyKey.FlatStyle = FlatStyle.Flat;
            btnCopyKey.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCopyKey.ForeColor = Color.White;
            btnCopyKey.Location = new Point(1017, 37);
            btnCopyKey.Margin = new Padding(4, 5, 4, 5);
            btnCopyKey.Name = "btnCopyKey";
            btnCopyKey.Size = new Size(186, 53);
            btnCopyKey.TabIndex = 2;
            btnCopyKey.Text = "📋 Sao Chép";
            btnCopyKey.UseVisualStyleBackColor = false;
            btnCopyKey.Click += btnCopyKey_Click;
            // 
            // txtGeneratedKey
            // 
            txtGeneratedKey.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtGeneratedKey.BackColor = Color.FromArgb(241, 245, 249);
            txtGeneratedKey.Font = new Font("Consolas", 9F);
            txtGeneratedKey.Location = new Point(14, 37);
            txtGeneratedKey.Margin = new Padding(4, 5, 4, 5);
            txtGeneratedKey.Multiline = true;
            txtGeneratedKey.Name = "txtGeneratedKey";
            txtGeneratedKey.ReadOnly = true;
            txtGeneratedKey.ScrollBars = ScrollBars.Vertical;
            txtGeneratedKey.Size = new Size(993, 116);
            txtGeneratedKey.TabIndex = 0;
            // 
            // lblStatusMessage
            // 
            lblStatusMessage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatusMessage.AutoSize = true;
            lblStatusMessage.Font = new Font("Segoe UI", 9F);
            lblStatusMessage.ForeColor = Color.DarkGreen;
            lblStatusMessage.Location = new Point(14, 165);
            lblStatusMessage.Margin = new Padding(4, 0, 4, 0);
            lblStatusMessage.Name = "lblStatusMessage";
            lblStatusMessage.Size = new Size(343, 25);
            lblStatusMessage.TabIndex = 1;
            lblStatusMessage.Text = "Sẵn sàng tạo License Key cho khách hàng.";
            // 
            // btnGenerateKey
            // 
            btnGenerateKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnGenerateKey.BackColor = Color.FromArgb(79, 70, 229);
            btnGenerateKey.Cursor = Cursors.Hand;
            btnGenerateKey.FlatAppearance.BorderSize = 0;
            btnGenerateKey.FlatStyle = FlatStyle.Flat;
            btnGenerateKey.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnGenerateKey.ForeColor = Color.White;
            btnGenerateKey.Location = new Point(17, 583);
            btnGenerateKey.Margin = new Padding(4, 5, 4, 5);
            btnGenerateKey.Name = "btnGenerateKey";
            btnGenerateKey.Size = new Size(1217, 73);
            btnGenerateKey.TabIndex = 3;
            btnGenerateKey.Text = "⚡ TẠO VÀ KÝ SỐ LICENSE KEY (RSA 3072-BIT)";
            btnGenerateKey.UseVisualStyleBackColor = false;
            btnGenerateKey.Click += btnGenerateKey_Click;
            // 
            // grpLimits
            // 
            grpLimits.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpLimits.Controls.Add(chkAdvancedReport);
            grpLimits.Controls.Add(chkDualCamera);
            grpLimits.Controls.Add(chkBarrier);
            grpLimits.Controls.Add(chkAnpr);
            grpLimits.Controls.Add(numMaxControllers);
            grpLimits.Controls.Add(lblMaxControllers);
            grpLimits.Controls.Add(numMaxCameras);
            grpLimits.Controls.Add(lblMaxCameras);
            grpLimits.Controls.Add(numMaxLanes);
            grpLimits.Controls.Add(lblMaxLanes);
            grpLimits.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpLimits.ForeColor = Color.FromArgb(15, 23, 42);
            grpLimits.Location = new Point(17, 383);
            grpLimits.Margin = new Padding(4, 5, 4, 5);
            grpLimits.Name = "grpLimits";
            grpLimits.Padding = new Padding(4, 5, 4, 5);
            grpLimits.Size = new Size(1217, 183);
            grpLimits.TabIndex = 2;
            grpLimits.TabStop = false;
            grpLimits.Text = "2. Giới Hạn Bản Quyền & Tính Năng (Quota Limits)";
            // 
            // chkAdvancedReport
            // 
            chkAdvancedReport.AutoSize = true;
            chkAdvancedReport.Checked = true;
            chkAdvancedReport.CheckState = CheckState.Checked;
            chkAdvancedReport.Font = new Font("Segoe UI", 9F);
            chkAdvancedReport.Location = new Point(886, 113);
            chkAdvancedReport.Margin = new Padding(4, 5, 4, 5);
            chkAdvancedReport.Name = "chkAdvancedReport";
            chkAdvancedReport.Size = new Size(255, 29);
            chkAdvancedReport.TabIndex = 9;
            chkAdvancedReport.Text = "Báo cáo thống kê nâng cao";
            chkAdvancedReport.UseVisualStyleBackColor = true;
            // 
            // chkDualCamera
            // 
            chkDualCamera.AutoSize = true;
            chkDualCamera.Checked = true;
            chkDualCamera.CheckState = CheckState.Checked;
            chkDualCamera.Font = new Font("Segoe UI", 9F);
            chkDualCamera.Location = new Point(514, 113);
            chkDualCamera.Margin = new Padding(4, 5, 4, 5);
            chkDualCamera.Name = "chkDualCamera";
            chkDualCamera.Size = new Size(267, 29);
            chkDualCamera.TabIndex = 8;
            chkDualCamera.Text = "2 Camera / Làn (Biển + Cảnh)";
            chkDualCamera.UseVisualStyleBackColor = true;
            // 
            // chkBarrier
            // 
            chkBarrier.AutoSize = true;
            chkBarrier.Checked = true;
            chkBarrier.CheckState = CheckState.Checked;
            chkBarrier.Font = new Font("Segoe UI", 9F);
            chkBarrier.Location = new Point(886, 53);
            chkBarrier.Margin = new Padding(4, 5, 4, 5);
            chkBarrier.Name = "chkBarrier";
            chkBarrier.Size = new Size(249, 29);
            chkBarrier.TabIndex = 7;
            chkBarrier.Text = "Điều khiển Barie & Cảm biến";
            chkBarrier.UseVisualStyleBackColor = true;
            // 
            // chkAnpr
            // 
            chkAnpr.AutoSize = true;
            chkAnpr.Checked = true;
            chkAnpr.CheckState = CheckState.Checked;
            chkAnpr.Font = new Font("Segoe UI", 9F);
            chkAnpr.Location = new Point(514, 53);
            chkAnpr.Margin = new Padding(4, 5, 4, 5);
            chkAnpr.Name = "chkAnpr";
            chkAnpr.Size = new Size(294, 29);
            chkAnpr.TabIndex = 6;
            chkAnpr.Text = "Nhận diện biển số AI (ANPR VN)";
            chkAnpr.UseVisualStyleBackColor = true;
            // 
            // numMaxControllers
            // 
            numMaxControllers.Location = new Point(200, 120);
            numMaxControllers.Margin = new Padding(4, 5, 4, 5);
            numMaxControllers.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numMaxControllers.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxControllers.Name = "numMaxControllers";
            numMaxControllers.Size = new Size(100, 33);
            numMaxControllers.TabIndex = 5;
            numMaxControllers.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblMaxControllers
            // 
            lblMaxControllers.AutoSize = true;
            lblMaxControllers.Font = new Font("Segoe UI", 9F);
            lblMaxControllers.Location = new Point(14, 123);
            lblMaxControllers.Margin = new Padding(4, 0, 4, 0);
            lblMaxControllers.Name = "lblMaxControllers";
            lblMaxControllers.Size = new Size(174, 25);
            lblMaxControllers.TabIndex = 4;
            lblMaxControllers.Text = "Bộ điều khiển tối đa:";
            // 
            // numMaxCameras
            // 
            numMaxCameras.Location = new Point(364, 50);
            numMaxCameras.Margin = new Padding(4, 5, 4, 5);
            numMaxCameras.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numMaxCameras.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxCameras.Name = "numMaxCameras";
            numMaxCameras.Size = new Size(100, 33);
            numMaxCameras.TabIndex = 3;
            numMaxCameras.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // lblMaxCameras
            // 
            lblMaxCameras.AutoSize = true;
            lblMaxCameras.Font = new Font("Segoe UI", 9F);
            lblMaxCameras.Location = new Point(250, 53);
            lblMaxCameras.Margin = new Padding(4, 0, 4, 0);
            lblMaxCameras.Name = "lblMaxCameras";
            lblMaxCameras.Size = new Size(103, 25);
            lblMaxCameras.TabIndex = 2;
            lblMaxCameras.Text = "Cam tối đa:";
            // 
            // numMaxLanes
            // 
            numMaxLanes.Location = new Point(129, 50);
            numMaxLanes.Margin = new Padding(4, 5, 4, 5);
            numMaxLanes.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numMaxLanes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxLanes.Name = "numMaxLanes";
            numMaxLanes.Size = new Size(100, 33);
            numMaxLanes.TabIndex = 1;
            numMaxLanes.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblMaxLanes
            // 
            lblMaxLanes.AutoSize = true;
            lblMaxLanes.Font = new Font("Segoe UI", 9F);
            lblMaxLanes.Location = new Point(14, 53);
            lblMaxLanes.Margin = new Padding(4, 0, 4, 0);
            lblMaxLanes.Name = "lblMaxLanes";
            lblMaxLanes.Size = new Size(116, 25);
            lblMaxLanes.TabIndex = 0;
            lblMaxLanes.Text = "Làn xe tối đa:";
            // 
            // grpDuration
            // 
            grpDuration.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpDuration.Controls.Add(dtpExpiryDate);
            grpDuration.Controls.Add(radCustom);
            grpDuration.Controls.Add(radPermanent);
            grpDuration.Controls.Add(rad3Years);
            grpDuration.Controls.Add(rad1Year);
            grpDuration.Controls.Add(rad90Days);
            grpDuration.Controls.Add(rad30Days);
            grpDuration.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpDuration.ForeColor = Color.FromArgb(15, 23, 42);
            grpDuration.Location = new Point(17, 233);
            grpDuration.Margin = new Padding(4, 5, 4, 5);
            grpDuration.Name = "grpDuration";
            grpDuration.Padding = new Padding(4, 5, 4, 5);
            grpDuration.Size = new Size(1217, 133);
            grpDuration.TabIndex = 1;
            grpDuration.TabStop = false;
            grpDuration.Text = "Thời Hạn Bản Quyền";
            // 
            // dtpExpiryDate
            // 
            dtpExpiryDate.CustomFormat = "dd/MM/yyyy HH:mm";
            dtpExpiryDate.Enabled = false;
            dtpExpiryDate.Format = DateTimePickerFormat.Custom;
            dtpExpiryDate.Location = new Point(886, 53);
            dtpExpiryDate.Margin = new Padding(4, 5, 4, 5);
            dtpExpiryDate.Name = "dtpExpiryDate";
            dtpExpiryDate.Size = new Size(227, 33);
            dtpExpiryDate.TabIndex = 6;
            // 
            // radCustom
            // 
            radCustom.AutoSize = true;
            radCustom.Font = new Font("Segoe UI", 9F);
            radCustom.Location = new Point(757, 57);
            radCustom.Margin = new Padding(4, 5, 4, 5);
            radCustom.Name = "radCustom";
            radCustom.Size = new Size(113, 29);
            radCustom.TabIndex = 5;
            radCustom.Text = "Tùy chọn:";
            radCustom.UseVisualStyleBackColor = true;
            radCustom.CheckedChanged += radDuration_CheckedChanged;
            // 
            // radPermanent
            // 
            radPermanent.AutoSize = true;
            radPermanent.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            radPermanent.ForeColor = Color.DarkMagenta;
            radPermanent.Location = new Point(586, 57);
            radPermanent.Margin = new Padding(4, 5, 4, 5);
            radPermanent.Name = "radPermanent";
            radPermanent.Size = new Size(130, 29);
            radPermanent.TabIndex = 4;
            radPermanent.Text = "VĨNH VIỄN";
            radPermanent.UseVisualStyleBackColor = true;
            radPermanent.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad3Years
            // 
            rad3Years.AutoSize = true;
            rad3Years.Font = new Font("Segoe UI", 9F);
            rad3Years.Location = new Point(443, 57);
            rad3Years.Margin = new Padding(4, 5, 4, 5);
            rad3Years.Name = "rad3Years";
            rad3Years.Size = new Size(90, 29);
            rad3Years.TabIndex = 3;
            rad3Years.Text = "3 Năm";
            rad3Years.UseVisualStyleBackColor = true;
            rad3Years.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad1Year
            // 
            rad1Year.AutoSize = true;
            rad1Year.Checked = true;
            rad1Year.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            rad1Year.ForeColor = Color.FromArgb(37, 99, 235);
            rad1Year.Location = new Point(286, 57);
            rad1Year.Margin = new Padding(4, 5, 4, 5);
            rad1Year.Name = "rad1Year";
            rad1Year.Size = new Size(92, 29);
            rad1Year.TabIndex = 2;
            rad1Year.TabStop = true;
            rad1Year.Text = "1 Năm";
            rad1Year.UseVisualStyleBackColor = true;
            rad1Year.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad90Days
            // 
            rad90Days.AutoSize = true;
            rad90Days.Font = new Font("Segoe UI", 9F);
            rad90Days.Location = new Point(143, 57);
            rad90Days.Margin = new Padding(4, 5, 4, 5);
            rad90Days.Name = "rad90Days";
            rad90Days.Size = new Size(104, 29);
            rad90Days.TabIndex = 1;
            rad90Days.Text = "90 Ngày";
            rad90Days.UseVisualStyleBackColor = true;
            rad90Days.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad30Days
            // 
            rad30Days.AutoSize = true;
            rad30Days.Font = new Font("Segoe UI", 9F);
            rad30Days.Location = new Point(14, 57);
            rad30Days.Margin = new Padding(4, 5, 4, 5);
            rad30Days.Name = "rad30Days";
            rad30Days.Size = new Size(104, 29);
            rad30Days.TabIndex = 0;
            rad30Days.Text = "30 Ngày";
            rad30Days.UseVisualStyleBackColor = true;
            rad30Days.CheckedChanged += radDuration_CheckedChanged;
            // 
            // grpCustomer
            // 
            grpCustomer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpCustomer.Controls.Add(txtNote);
            grpCustomer.Controls.Add(lblNote);
            grpCustomer.Controls.Add(btnPasteMachineCode);
            grpCustomer.Controls.Add(btnGetThisMachineCode);
            grpCustomer.Controls.Add(txtMachineCode);
            grpCustomer.Controls.Add(lblMachineCode);
            grpCustomer.Controls.Add(txtCustomerName);
            grpCustomer.Controls.Add(lblCustomerName);
            grpCustomer.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpCustomer.ForeColor = Color.FromArgb(15, 23, 42);
            grpCustomer.Location = new Point(17, 20);
            grpCustomer.Margin = new Padding(4, 5, 4, 5);
            grpCustomer.Name = "grpCustomer";
            grpCustomer.Padding = new Padding(4, 5, 4, 5);
            grpCustomer.Size = new Size(1217, 200);
            grpCustomer.TabIndex = 0;
            grpCustomer.TabStop = false;
            grpCustomer.Text = "1. Thông Tin Khách Hàng & Máy Tính Cần Cấp Bản Quyền";
            // 
            // txtNote
            // 
            txtNote.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtNote.Font = new Font("Segoe UI", 9F);
            txtNote.Location = new Point(800, 80);
            txtNote.Margin = new Padding(4, 5, 4, 5);
            txtNote.Multiline = true;
            txtNote.Name = "txtNote";
            txtNote.PlaceholderText = "Ghi chú hợp đồng, số điện thoại, địa chỉ...";
            txtNote.Size = new Size(398, 91);
            txtNote.TabIndex = 7;
            // 
            // lblNote
            // 
            lblNote.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblNote.AutoSize = true;
            lblNote.Font = new Font("Segoe UI", 9F);
            lblNote.Location = new Point(800, 43);
            lblNote.Margin = new Padding(4, 0, 4, 0);
            lblNote.Name = "lblNote";
            lblNote.Size = new Size(75, 25);
            lblNote.TabIndex = 6;
            lblNote.Text = "Ghi chú:";
            // 
            // btnPasteMachineCode
            // 
            btnPasteMachineCode.BackColor = Color.FromArgb(226, 232, 240);
            btnPasteMachineCode.Cursor = Cursors.Hand;
            btnPasteMachineCode.FlatStyle = FlatStyle.Flat;
            btnPasteMachineCode.Font = new Font("Segoe UI", 8.5F);
            btnPasteMachineCode.ForeColor = Color.Black;
            btnPasteMachineCode.Location = new Point(586, 130);
            btnPasteMachineCode.Margin = new Padding(4, 5, 4, 5);
            btnPasteMachineCode.Name = "btnPasteMachineCode";
            btnPasteMachineCode.Size = new Size(86, 43);
            btnPasteMachineCode.TabIndex = 5;
            btnPasteMachineCode.Text = "Dán";
            btnPasteMachineCode.UseVisualStyleBackColor = false;
            btnPasteMachineCode.Click += btnPasteMachineCode_Click;
            // 
            // btnGetThisMachineCode
            // 
            btnGetThisMachineCode.BackColor = Color.FromArgb(226, 232, 240);
            btnGetThisMachineCode.Cursor = Cursors.Hand;
            btnGetThisMachineCode.FlatStyle = FlatStyle.Flat;
            btnGetThisMachineCode.Font = new Font("Segoe UI", 8.5F);
            btnGetThisMachineCode.ForeColor = Color.Black;
            btnGetThisMachineCode.Location = new Point(679, 130);
            btnGetThisMachineCode.Margin = new Padding(4, 5, 4, 5);
            btnGetThisMachineCode.Name = "btnGetThisMachineCode";
            btnGetThisMachineCode.Size = new Size(107, 43);
            btnGetThisMachineCode.TabIndex = 4;
            btnGetThisMachineCode.Text = "Máy Này";
            btnGetThisMachineCode.UseVisualStyleBackColor = false;
            btnGetThisMachineCode.Click += btnGetThisMachineCode_Click;
            // 
            // txtMachineCode
            // 
            txtMachineCode.Font = new Font("Consolas", 10F, FontStyle.Bold);
            txtMachineCode.Location = new Point(200, 133);
            txtMachineCode.Margin = new Padding(4, 5, 4, 5);
            txtMachineCode.Name = "txtMachineCode";
            txtMachineCode.PlaceholderText = "VD: PX-A1B2-C3D4-E5F6-7890";
            txtMachineCode.Size = new Size(370, 31);
            txtMachineCode.TabIndex = 3;
            // 
            // lblMachineCode
            // 
            lblMachineCode.AutoSize = true;
            lblMachineCode.Font = new Font("Segoe UI", 9F);
            lblMachineCode.Location = new Point(14, 140);
            lblMachineCode.Margin = new Padding(4, 0, 4, 0);
            lblMachineCode.Name = "lblMachineCode";
            lblMachineCode.Size = new Size(164, 25);
            lblMachineCode.TabIndex = 2;
            lblMachineCode.Text = "Mã Máy Tính (ID) *:";
            // 
            // txtCustomerName
            // 
            txtCustomerName.Font = new Font("Segoe UI", 9F);
            txtCustomerName.Location = new Point(200, 60);
            txtCustomerName.Margin = new Padding(4, 5, 4, 5);
            txtCustomerName.Name = "txtCustomerName";
            txtCustomerName.PlaceholderText = "VD: Bãi Đỗ Xe Tòa Nhà ABC";
            txtCustomerName.Size = new Size(584, 31);
            txtCustomerName.TabIndex = 1;
            // 
            // lblCustomerName
            // 
            lblCustomerName.AutoSize = true;
            lblCustomerName.Font = new Font("Segoe UI", 9F);
            lblCustomerName.Location = new Point(14, 67);
            lblCustomerName.Margin = new Padding(4, 0, 4, 0);
            lblCustomerName.Name = "lblCustomerName";
            lblCustomerName.Size = new Size(155, 25);
            lblCustomerName.TabIndex = 0;
            lblCustomerName.Text = "Tên Khách Hàng *:";
            // 
            // tabVerify
            // 
            tabVerify.BackColor = Color.FromArgb(248, 250, 252);
            tabVerify.Controls.Add(grpDecodedResult);
            tabVerify.Controls.Add(grpInputKey);
            tabVerify.Location = new Point(4, 34);
            tabVerify.Margin = new Padding(4, 5, 4, 5);
            tabVerify.Name = "tabVerify";
            tabVerify.Padding = new Padding(17, 20, 17, 20);
            tabVerify.Size = new Size(1255, 761);
            tabVerify.TabIndex = 1;
            tabVerify.Text = "  🔍 Kiểm Tra & Giải Mã Key  ";
            // 
            // grpDecodedResult
            // 
            grpDecodedResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpDecodedResult.Controls.Add(txtDecodedInfo);
            grpDecodedResult.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpDecodedResult.ForeColor = Color.FromArgb(15, 23, 42);
            grpDecodedResult.Location = new Point(17, 317);
            grpDecodedResult.Margin = new Padding(4, 5, 4, 5);
            grpDecodedResult.Name = "grpDecodedResult";
            grpDecodedResult.Padding = new Padding(4, 5, 4, 5);
            grpDecodedResult.Size = new Size(1217, 412);
            grpDecodedResult.TabIndex = 1;
            grpDecodedResult.TabStop = false;
            grpDecodedResult.Text = "Thông Tin Chi Tiết Gói Bản Quyền Giải Mã";
            // 
            // txtDecodedInfo
            // 
            txtDecodedInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtDecodedInfo.BackColor = Color.White;
            txtDecodedInfo.Font = new Font("Consolas", 10F);
            txtDecodedInfo.Location = new Point(17, 47);
            txtDecodedInfo.Margin = new Padding(4, 5, 4, 5);
            txtDecodedInfo.Multiline = true;
            txtDecodedInfo.Name = "txtDecodedInfo";
            txtDecodedInfo.ReadOnly = true;
            txtDecodedInfo.ScrollBars = ScrollBars.Vertical;
            txtDecodedInfo.Size = new Size(1181, 343);
            txtDecodedInfo.TabIndex = 0;
            // 
            // grpInputKey
            // 
            grpInputKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpInputKey.Controls.Add(btnOpenLicFile);
            grpInputKey.Controls.Add(btnVerifyInputKey);
            grpInputKey.Controls.Add(txtVerifyMachineCode);
            grpInputKey.Controls.Add(lblVerifyMachineCode);
            grpInputKey.Controls.Add(txtVerifyKey);
            grpInputKey.Controls.Add(lblVerifyKey);
            grpInputKey.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpInputKey.ForeColor = Color.FromArgb(15, 23, 42);
            grpInputKey.Location = new Point(17, 20);
            grpInputKey.Margin = new Padding(4, 5, 4, 5);
            grpInputKey.Name = "grpInputKey";
            grpInputKey.Padding = new Padding(4, 5, 4, 5);
            grpInputKey.Size = new Size(1217, 280);
            grpInputKey.TabIndex = 0;
            grpInputKey.TabStop = false;
            grpInputKey.Text = "Nhập License Key hoặc Mở File .lic";
            // 
            // btnOpenLicFile
            // 
            btnOpenLicFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenLicFile.BackColor = Color.FromArgb(226, 232, 240);
            btnOpenLicFile.Cursor = Cursors.Hand;
            btnOpenLicFile.FlatAppearance.BorderSize = 0;
            btnOpenLicFile.FlatStyle = FlatStyle.Flat;
            btnOpenLicFile.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnOpenLicFile.ForeColor = Color.Black;
            btnOpenLicFile.Location = new Point(889, 47);
            btnOpenLicFile.Margin = new Padding(4, 5, 4, 5);
            btnOpenLicFile.Name = "btnOpenLicFile";
            btnOpenLicFile.Size = new Size(259, 58);
            btnOpenLicFile.TabIndex = 5;
            btnOpenLicFile.Text = "📂 Mở File .lic";
            btnOpenLicFile.UseVisualStyleBackColor = false;
            btnOpenLicFile.Click += btnOpenLicFile_Click;
            // 
            // btnVerifyInputKey
            // 
            btnVerifyInputKey.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnVerifyInputKey.BackColor = Color.FromArgb(37, 99, 235);
            btnVerifyInputKey.Cursor = Cursors.Hand;
            btnVerifyInputKey.FlatStyle = FlatStyle.Flat;
            btnVerifyInputKey.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnVerifyInputKey.ForeColor = Color.White;
            btnVerifyInputKey.Location = new Point(889, 111);
            btnVerifyInputKey.Margin = new Padding(4, 5, 4, 5);
            btnVerifyInputKey.Name = "btnVerifyInputKey";
            btnVerifyInputKey.Size = new Size(259, 58);
            btnVerifyInputKey.TabIndex = 4;
            btnVerifyInputKey.Text = "🔍 GIẢI MÃ && KIỂM TRA";
            btnVerifyInputKey.UseVisualStyleBackColor = false;
            btnVerifyInputKey.Click += btnVerifyInputKey_Click;
            // 
            // txtVerifyMachineCode
            // 
            txtVerifyMachineCode.Font = new Font("Consolas", 9.5F);
            txtVerifyMachineCode.Location = new Point(231, 217);
            txtVerifyMachineCode.Margin = new Padding(4, 5, 4, 5);
            txtVerifyMachineCode.Name = "txtVerifyMachineCode";
            txtVerifyMachineCode.PlaceholderText = "Để trống nếu muốn kiểm tra với mã máy tính này";
            txtVerifyMachineCode.Size = new Size(498, 30);
            txtVerifyMachineCode.TabIndex = 3;
            // 
            // lblVerifyMachineCode
            // 
            lblVerifyMachineCode.AutoSize = true;
            lblVerifyMachineCode.Font = new Font("Segoe UI", 9F);
            lblVerifyMachineCode.Location = new Point(14, 217);
            lblVerifyMachineCode.Margin = new Padding(4, 0, 4, 0);
            lblVerifyMachineCode.Name = "lblVerifyMachineCode";
            lblVerifyMachineCode.Size = new Size(209, 25);
            lblVerifyMachineCode.TabIndex = 2;
            lblVerifyMachineCode.Text = "Mã máy muốn đối chiếu:";
            // 
            // txtVerifyKey
            // 
            txtVerifyKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtVerifyKey.Font = new Font("Consolas", 8.5F);
            txtVerifyKey.Location = new Point(140, 40);
            txtVerifyKey.Margin = new Padding(4, 5, 4, 5);
            txtVerifyKey.Multiline = true;
            txtVerifyKey.Name = "txtVerifyKey";
            txtVerifyKey.PlaceholderText = "Dán chuỗi License Key (PX-LIC-...) vào đây...";
            txtVerifyKey.ScrollBars = ScrollBars.Vertical;
            txtVerifyKey.Size = new Size(741, 142);
            txtVerifyKey.TabIndex = 1;
            // 
            // lblVerifyKey
            // 
            lblVerifyKey.AutoSize = true;
            lblVerifyKey.Font = new Font("Segoe UI", 9F);
            lblVerifyKey.Location = new Point(14, 47);
            lblVerifyKey.Margin = new Padding(4, 0, 4, 0);
            lblVerifyKey.Name = "lblVerifyKey";
            lblVerifyKey.Size = new Size(105, 25);
            lblVerifyKey.TabIndex = 0;
            lblVerifyKey.Text = "License Key:";
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.Teal;
            pnlHeader.Controls.Add(lblKeyStatus);
            pnlHeader.Controls.Add(lblSubTitle);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(4, 5, 4, 5);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new Padding(23, 20, 23, 20);
            pnlHeader.Size = new Size(1263, 108);
            pnlHeader.TabIndex = 1;
            // 
            // lblKeyStatus
            // 
            lblKeyStatus.Dock = DockStyle.Right;
            lblKeyStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblKeyStatus.ForeColor = Color.FromArgb(128, 255, 128);
            lblKeyStatus.Location = new Point(996, 20);
            lblKeyStatus.Margin = new Padding(4, 0, 4, 0);
            lblKeyStatus.Name = "lblKeyStatus";
            lblKeyStatus.Size = new Size(244, 68);
            lblKeyStatus.TabIndex = 2;
            lblKeyStatus.Text = "Khóa RSA: Đang kiểm tra...";
            lblKeyStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.Font = new Font("Segoe UI", 8.5F);
            lblSubTitle.ForeColor = Color.White;
            lblSubTitle.Location = new Point(23, 63);
            lblSubTitle.Margin = new Padding(4, 0, 4, 0);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new Size(611, 23);
            lblSubTitle.TabIndex = 1;
            lblSubTitle.Text = "Hệ thống phát hành và quản lý bản quyền phần mềm bãi đỗ xe (RSA 3072-bit)";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(23, 17);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(571, 36);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "PHÚ XUÂN PARKING — LICENSE GENERATOR";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1263, 907);
            Controls.Add(tabControlMain);
            Controls.Add(pnlHeader);
            Margin = new Padding(4, 5, 4, 5);
            MinimumSize = new Size(1205, 963);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Phú Xuân Parking — Công Cụ Tạo & Quản Lý License Key (Vendor Tool)";
            tabControlMain.ResumeLayout(false);
            tabGenerate.ResumeLayout(false);
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            grpLimits.ResumeLayout(false);
            grpLimits.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxControllers).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxCameras).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxLanes).EndInit();
            grpDuration.ResumeLayout(false);
            grpDuration.PerformLayout();
            grpCustomer.ResumeLayout(false);
            grpCustomer.PerformLayout();
            tabVerify.ResumeLayout(false);
            grpDecodedResult.ResumeLayout(false);
            grpDecodedResult.PerformLayout();
            grpInputKey.ResumeLayout(false);
            grpInputKey.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControlMain;
        private TabPage tabGenerate;
        private TabPage tabVerify;
        private Panel pnlHeader;
        private Label lblSubTitle;
        private Label lblTitle;
        private Label lblKeyStatus;
        private GroupBox grpCustomer;
        private TextBox txtCustomerName;
        private Label lblCustomerName;
        private Button btnGetThisMachineCode;
        private TextBox txtMachineCode;
        private Label lblMachineCode;
        private Button btnPasteMachineCode;
        private TextBox txtNote;
        private Label lblNote;
        private GroupBox grpDuration;
        private RadioButton rad30Days;
        private RadioButton rad90Days;
        private RadioButton rad1Year;
        private RadioButton rad3Years;
        private RadioButton radPermanent;
        private RadioButton radCustom;
        private DateTimePicker dtpExpiryDate;
        private GroupBox grpLimits;
        private NumericUpDown numMaxLanes;
        private Label lblMaxLanes;
        private NumericUpDown numMaxCameras;
        private Label lblMaxCameras;
        private NumericUpDown numMaxControllers;
        private Label lblMaxControllers;
        private CheckBox chkAnpr;
        private CheckBox chkBarrier;
        private CheckBox chkDualCamera;
        private CheckBox chkAdvancedReport;
        private Button btnGenerateKey;
        private GroupBox grpOutput;
        private TextBox txtGeneratedKey;
        private Label lblStatusMessage;
        private Button btnCopyKey;
        private Button btnExportLicFile;
        private GroupBox grpInputKey;
        private Label lblVerifyKey;
        private TextBox txtVerifyKey;
        private Label lblVerifyMachineCode;
        private TextBox txtVerifyMachineCode;
        private Button btnVerifyInputKey;
        private Button btnOpenLicFile;
        private GroupBox grpDecodedResult;
        private TextBox txtDecodedInfo;
    }
}
