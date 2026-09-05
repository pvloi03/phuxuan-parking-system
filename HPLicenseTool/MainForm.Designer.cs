namespace HPLicenseTool
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
            btnGenerateKey = new Button();
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
            tabControlMain.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tabControlMain.Location = new Point(0, 79);
            tabControlMain.Margin = new Padding(4, 5, 4, 5);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1263, 828);
            tabControlMain.TabIndex = 0;
            // 
            // tabGenerate
            // 
            tabGenerate.BackColor = Color.FromArgb(248, 250, 252);
            tabGenerate.Controls.Add(grpOutput);
            tabGenerate.Controls.Add(btnGenerateKey);
            tabGenerate.Controls.Add(grpDuration);
            tabGenerate.Controls.Add(grpCustomer);
            tabGenerate.Location = new Point(4, 32);
            tabGenerate.Margin = new Padding(4, 5, 4, 5);
            tabGenerate.Name = "tabGenerate";
            tabGenerate.Padding = new Padding(17, 20, 17, 20);
            tabGenerate.Size = new Size(1255, 792);
            tabGenerate.TabIndex = 0;
            tabGenerate.Text = "  🔑 Phát Hành License Key  ";
            // 
            // grpOutput
            // 
            grpOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpOutput.Controls.Add(btnExportLicFile);
            grpOutput.Controls.Add(btnCopyKey);
            grpOutput.Controls.Add(txtGeneratedKey);
            grpOutput.Font = new Font("Consolas", 10F, FontStyle.Bold);
            grpOutput.ForeColor = Color.FromArgb(15, 23, 42);
            grpOutput.Location = new Point(17, 435);
            grpOutput.Margin = new Padding(0);
            grpOutput.Name = "grpOutput";
            grpOutput.Padding = new Padding(0);
            grpOutput.Size = new Size(1217, 350);
            grpOutput.TabIndex = 4;
            grpOutput.TabStop = false;
            grpOutput.Text = "2. Chuỗi License Key Đã Tạo";
            // 
            // btnExportLicFile
            // 
            btnExportLicFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportLicFile.BackColor = Color.Teal;
            btnExportLicFile.Cursor = Cursors.Hand;
            btnExportLicFile.FlatStyle = FlatStyle.Flat;
            btnExportLicFile.Font = new Font("Consolas", 9F, FontStyle.Bold);
            btnExportLicFile.ForeColor = Color.White;
            btnExportLicFile.Location = new Point(976, 290);
            btnExportLicFile.Margin = new Padding(4, 5, 4, 5);
            btnExportLicFile.Name = "btnExportLicFile";
            btnExportLicFile.Size = new Size(222, 40);
            btnExportLicFile.TabIndex = 8;
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
            btnCopyKey.Font = new Font("Consolas", 9F, FontStyle.Bold);
            btnCopyKey.ForeColor = Color.White;
            btnCopyKey.ImageAlign = ContentAlignment.TopLeft;
            btnCopyKey.Location = new Point(795, 290);
            btnCopyKey.Margin = new Padding(4, 5, 4, 5);
            btnCopyKey.Name = "btnCopyKey";
            btnCopyKey.Size = new Size(170, 40);
            btnCopyKey.TabIndex = 7;
            btnCopyKey.Text = "📋 Sao Chép";
            btnCopyKey.UseVisualStyleBackColor = false;
            btnCopyKey.Click += btnCopyKey_Click;
            // 
            // txtGeneratedKey
            // 
            txtGeneratedKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtGeneratedKey.BackColor = Color.FromArgb(241, 245, 249);
            txtGeneratedKey.Font = new Font("Consolas", 9F);
            txtGeneratedKey.Location = new Point(14, 31);
            txtGeneratedKey.Margin = new Padding(0);
            txtGeneratedKey.Multiline = true;
            txtGeneratedKey.Name = "txtGeneratedKey";
            txtGeneratedKey.ReadOnly = true;
            txtGeneratedKey.ScrollBars = ScrollBars.Vertical;
            txtGeneratedKey.Size = new Size(1184, 240);
            txtGeneratedKey.TabIndex = 0;
            // 
            // btnGenerateKey
            // 
            btnGenerateKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnGenerateKey.BackColor = Color.FromArgb(79, 70, 229);
            btnGenerateKey.Cursor = Cursors.Hand;
            btnGenerateKey.FlatAppearance.BorderSize = 0;
            btnGenerateKey.FlatStyle = FlatStyle.Flat;
            btnGenerateKey.Font = new Font("Consolas", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnGenerateKey.ForeColor = Color.White;
            btnGenerateKey.Location = new Point(17, 350);
            btnGenerateKey.Margin = new Padding(4, 5, 4, 5);
            btnGenerateKey.Name = "btnGenerateKey";
            btnGenerateKey.Size = new Size(1217, 73);
            btnGenerateKey.TabIndex = 3;
            btnGenerateKey.Text = "⚡ TẠO VÀ KÝ SỐ LICENSE KEY (RSA 3072-BIT)";
            btnGenerateKey.UseVisualStyleBackColor = false;
            btnGenerateKey.Click += btnGenerateKey_Click;
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
            grpDuration.Font = new Font("Consolas", 10F, FontStyle.Bold);
            grpDuration.ForeColor = Color.FromArgb(15, 23, 42);
            grpDuration.Location = new Point(17, 230);
            grpDuration.Margin = new Padding(4, 5, 4, 5);
            grpDuration.Name = "grpDuration";
            grpDuration.Padding = new Padding(4, 5, 4, 5);
            grpDuration.Size = new Size(1217, 106);
            grpDuration.TabIndex = 1;
            grpDuration.TabStop = false;
            grpDuration.Text = "Thời Hạn Bản Quyền";
            // 
            // dtpExpiryDate
            // 
            dtpExpiryDate.CalendarFont = new Font("Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpExpiryDate.CustomFormat = "dd/MM/yyyy HH:mm";
            dtpExpiryDate.Enabled = false;
            dtpExpiryDate.Font = new Font("Consolas", 8F);
            dtpExpiryDate.Format = DateTimePickerFormat.Custom;
            dtpExpiryDate.Location = new Point(886, 53);
            dtpExpiryDate.Margin = new Padding(4, 5, 4, 5);
            dtpExpiryDate.Name = "dtpExpiryDate";
            dtpExpiryDate.Size = new Size(210, 26);
            dtpExpiryDate.TabIndex = 6;
            // 
            // radCustom
            // 
            radCustom.AutoSize = true;
            radCustom.Font = new Font("Consolas", 8F);
            radCustom.Location = new Point(757, 57);
            radCustom.Margin = new Padding(4, 5, 4, 5);
            radCustom.Name = "radCustom";
            radCustom.Size = new Size(115, 23);
            radCustom.TabIndex = 5;
            radCustom.Text = "Tùy chọn:";
            radCustom.UseVisualStyleBackColor = true;
            radCustom.CheckedChanged += radDuration_CheckedChanged;
            // 
            // radPermanent
            // 
            radPermanent.AutoSize = true;
            radPermanent.Font = new Font("Consolas", 8F);
            radPermanent.ForeColor = Color.DarkOrange;
            radPermanent.Location = new Point(586, 57);
            radPermanent.Margin = new Padding(4, 5, 4, 5);
            radPermanent.Name = "radPermanent";
            radPermanent.Size = new Size(115, 23);
            radPermanent.TabIndex = 4;
            radPermanent.Text = "Vĩnh Viễn";
            radPermanent.UseVisualStyleBackColor = true;
            radPermanent.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad3Years
            // 
            rad3Years.AutoSize = true;
            rad3Years.Font = new Font("Consolas", 8F);
            rad3Years.Location = new Point(443, 57);
            rad3Years.Margin = new Padding(4, 5, 4, 5);
            rad3Years.Name = "rad3Years";
            rad3Years.Size = new Size(79, 23);
            rad3Years.TabIndex = 3;
            rad3Years.Text = "3 Năm";
            rad3Years.UseVisualStyleBackColor = true;
            rad3Years.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad1Year
            // 
            rad1Year.AutoSize = true;
            rad1Year.Checked = true;
            rad1Year.Font = new Font("Consolas", 8F);
            rad1Year.ForeColor = Color.FromArgb(37, 99, 235);
            rad1Year.Location = new Point(286, 57);
            rad1Year.Margin = new Padding(4, 5, 4, 5);
            rad1Year.Name = "rad1Year";
            rad1Year.Size = new Size(79, 23);
            rad1Year.TabIndex = 2;
            rad1Year.TabStop = true;
            rad1Year.Text = "1 Năm";
            rad1Year.UseVisualStyleBackColor = true;
            rad1Year.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad90Days
            // 
            rad90Days.AutoSize = true;
            rad90Days.Font = new Font("Consolas", 8F);
            rad90Days.Location = new Point(143, 57);
            rad90Days.Margin = new Padding(4, 5, 4, 5);
            rad90Days.Name = "rad90Days";
            rad90Days.Size = new Size(97, 23);
            rad90Days.TabIndex = 1;
            rad90Days.Text = "90 Ngày";
            rad90Days.UseVisualStyleBackColor = true;
            rad90Days.CheckedChanged += radDuration_CheckedChanged;
            // 
            // rad30Days
            // 
            rad30Days.AutoSize = true;
            rad30Days.Font = new Font("Consolas", 8F);
            rad30Days.Location = new Point(14, 57);
            rad30Days.Margin = new Padding(4, 5, 4, 5);
            rad30Days.Name = "rad30Days";
            rad30Days.Size = new Size(97, 23);
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
            txtNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtNote.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtNote.Location = new Point(800, 60);
            txtNote.Margin = new Padding(4, 5, 4, 5);
            txtNote.Multiline = true;
            txtNote.Name = "txtNote";
            txtNote.PlaceholderText = "Ghi chú hợp đồng, số điện thoại, địa chỉ...";
            txtNote.Size = new Size(398, 104);
            txtNote.TabIndex = 7;
            // 
            // lblNote
            // 
            lblNote.AutoSize = true;
            lblNote.Font = new Font("Consolas", 9F);
            lblNote.Location = new Point(795, 31);
            lblNote.Margin = new Padding(4, 0, 4, 0);
            lblNote.Name = "lblNote";
            lblNote.Size = new Size(90, 22);
            lblNote.TabIndex = 6;
            lblNote.Text = "Ghi chú:";
            // 
            // btnPasteMachineCode
            // 
            btnPasteMachineCode.BackColor = Color.Teal;
            btnPasteMachineCode.Cursor = Cursors.Hand;
            btnPasteMachineCode.FlatAppearance.BorderSize = 0;
            btnPasteMachineCode.FlatStyle = FlatStyle.Flat;
            btnPasteMachineCode.Font = new Font("Consolas", 8F);
            btnPasteMachineCode.ForeColor = Color.White;
            btnPasteMachineCode.Location = new Point(586, 133);
            btnPasteMachineCode.Margin = new Padding(4, 5, 4, 5);
            btnPasteMachineCode.Name = "btnPasteMachineCode";
            btnPasteMachineCode.Size = new Size(81, 31);
            btnPasteMachineCode.TabIndex = 5;
            btnPasteMachineCode.Text = "Dán";
            btnPasteMachineCode.UseVisualStyleBackColor = false;
            btnPasteMachineCode.Click += btnPasteMachineCode_Click;
            // 
            // btnGetThisMachineCode
            // 
            btnGetThisMachineCode.BackColor = Color.Teal;
            btnGetThisMachineCode.Cursor = Cursors.Hand;
            btnGetThisMachineCode.FlatAppearance.BorderSize = 0;
            btnGetThisMachineCode.FlatStyle = FlatStyle.Flat;
            btnGetThisMachineCode.Font = new Font("Consolas", 8F);
            btnGetThisMachineCode.ForeColor = Color.White;
            btnGetThisMachineCode.Location = new Point(675, 133);
            btnGetThisMachineCode.Margin = new Padding(4, 5, 4, 5);
            btnGetThisMachineCode.Name = "btnGetThisMachineCode";
            btnGetThisMachineCode.Size = new Size(109, 31);
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
            lblMachineCode.Font = new Font("Consolas", 9F);
            lblMachineCode.Location = new Point(14, 140);
            lblMachineCode.Margin = new Padding(4, 0, 4, 0);
            lblMachineCode.Name = "lblMachineCode";
            lblMachineCode.Size = new Size(200, 22);
            lblMachineCode.TabIndex = 2;
            lblMachineCode.Text = "Mã Máy Tính (ID) *:";
            // 
            // txtCustomerName
            // 
            txtCustomerName.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtCustomerName.Location = new Point(200, 60);
            txtCustomerName.Margin = new Padding(4, 5, 4, 5);
            txtCustomerName.Name = "txtCustomerName";
            txtCustomerName.PlaceholderText = "VD: Bãi Đỗ Xe Tòa Nhà ABC";
            txtCustomerName.Size = new Size(584, 29);
            txtCustomerName.TabIndex = 1;
            // 
            // lblCustomerName
            // 
            lblCustomerName.AutoSize = true;
            lblCustomerName.Font = new Font("Consolas", 9F);
            lblCustomerName.Location = new Point(14, 67);
            lblCustomerName.Margin = new Padding(4, 0, 4, 0);
            lblCustomerName.Name = "lblCustomerName";
            lblCustomerName.Size = new Size(180, 22);
            lblCustomerName.TabIndex = 0;
            lblCustomerName.Text = "Tên Khách Hàng *:";
            // 
            // tabVerify
            // 
            tabVerify.BackColor = Color.FromArgb(248, 250, 252);
            tabVerify.Controls.Add(grpDecodedResult);
            tabVerify.Controls.Add(grpInputKey);
            tabVerify.Location = new Point(4, 32);
            tabVerify.Margin = new Padding(4, 5, 4, 5);
            tabVerify.Name = "tabVerify";
            tabVerify.Padding = new Padding(17, 20, 17, 20);
            tabVerify.Size = new Size(1255, 792);
            tabVerify.TabIndex = 1;
            tabVerify.Text = "  🔍 Kiểm Tra & Giải Mã Key  ";
            // 
            // grpDecodedResult
            // 
            grpDecodedResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpDecodedResult.Controls.Add(txtDecodedInfo);
            grpDecodedResult.Font = new Font("Consolas", 10F, FontStyle.Bold);
            grpDecodedResult.ForeColor = Color.FromArgb(15, 23, 42);
            grpDecodedResult.Location = new Point(17, 450);
            grpDecodedResult.Margin = new Padding(4, 5, 4, 5);
            grpDecodedResult.Name = "grpDecodedResult";
            grpDecodedResult.Padding = new Padding(4, 5, 4, 5);
            grpDecodedResult.Size = new Size(1217, 317);
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
            txtDecodedInfo.Size = new Size(1181, 260);
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
            grpInputKey.Font = new Font("Consolas", 10F, FontStyle.Bold);
            grpInputKey.ForeColor = Color.FromArgb(15, 23, 42);
            grpInputKey.Location = new Point(17, 20);
            grpInputKey.Margin = new Padding(4, 5, 4, 5);
            grpInputKey.Name = "grpInputKey";
            grpInputKey.Padding = new Padding(4, 5, 4, 5);
            grpInputKey.Size = new Size(1217, 420);
            grpInputKey.TabIndex = 0;
            grpInputKey.TabStop = false;
            grpInputKey.Text = "Nhập License Key hoặc Mở File .lic";
            // 
            // btnOpenLicFile
            // 
            btnOpenLicFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenLicFile.BackColor = Color.Teal;
            btnOpenLicFile.Cursor = Cursors.Hand;
            btnOpenLicFile.FlatAppearance.BorderSize = 0;
            btnOpenLicFile.FlatStyle = FlatStyle.Flat;
            btnOpenLicFile.Font = new Font("Consolas", 9F, FontStyle.Bold);
            btnOpenLicFile.ForeColor = Color.White;
            btnOpenLicFile.Location = new Point(995, 312);
            btnOpenLicFile.Margin = new Padding(4, 5, 4, 5);
            btnOpenLicFile.Name = "btnOpenLicFile";
            btnOpenLicFile.Size = new Size(203, 40);
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
            btnVerifyInputKey.Font = new Font("Consolas", 9F, FontStyle.Bold);
            btnVerifyInputKey.ForeColor = Color.White;
            btnVerifyInputKey.Location = new Point(723, 312);
            btnVerifyInputKey.Margin = new Padding(4, 5, 4, 5);
            btnVerifyInputKey.Name = "btnVerifyInputKey";
            btnVerifyInputKey.Size = new Size(264, 40);
            btnVerifyInputKey.TabIndex = 4;
            btnVerifyInputKey.Text = "🔍 GIẢI MÃ && KIỂM TRA";
            btnVerifyInputKey.UseVisualStyleBackColor = false;
            btnVerifyInputKey.Click += btnVerifyInputKey_Click;
            // 
            // txtVerifyMachineCode
            // 
            txtVerifyMachineCode.Font = new Font("Consolas", 9F);
            txtVerifyMachineCode.Location = new Point(248, 377);
            txtVerifyMachineCode.Margin = new Padding(4, 5, 4, 5);
            txtVerifyMachineCode.Name = "txtVerifyMachineCode";
            txtVerifyMachineCode.PlaceholderText = "Để trống nếu muốn kiểm tra với mã máy tính này";
            txtVerifyMachineCode.Size = new Size(528, 29);
            txtVerifyMachineCode.TabIndex = 3;
            // 
            // lblVerifyMachineCode
            // 
            lblVerifyMachineCode.AutoSize = true;
            lblVerifyMachineCode.Font = new Font("Consolas", 9F);
            lblVerifyMachineCode.Location = new Point(10, 380);
            lblVerifyMachineCode.Margin = new Padding(4, 0, 4, 0);
            lblVerifyMachineCode.Name = "lblVerifyMachineCode";
            lblVerifyMachineCode.Size = new Size(230, 22);
            lblVerifyMachineCode.TabIndex = 2;
            lblVerifyMachineCode.Text = "Mã máy muốn đối chiếu:";
            // 
            // txtVerifyKey
            // 
            txtVerifyKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtVerifyKey.Font = new Font("Consolas", 8.5F);
            txtVerifyKey.Location = new Point(17, 61);
            txtVerifyKey.Margin = new Padding(4, 5, 4, 5);
            txtVerifyKey.Multiline = true;
            txtVerifyKey.Name = "txtVerifyKey";
            txtVerifyKey.PlaceholderText = "Dán chuỗi License Key (PX-LIC-...) vào đây...";
            txtVerifyKey.ScrollBars = ScrollBars.Vertical;
            txtVerifyKey.Size = new Size(1181, 241);
            txtVerifyKey.TabIndex = 1;
            // 
            // lblVerifyKey
            // 
            lblVerifyKey.AutoSize = true;
            lblVerifyKey.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVerifyKey.Location = new Point(10, 31);
            lblVerifyKey.Margin = new Padding(4, 0, 4, 0);
            lblVerifyKey.Name = "lblVerifyKey";
            lblVerifyKey.Size = new Size(130, 22);
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
            pnlHeader.Size = new Size(1263, 79);
            pnlHeader.TabIndex = 1;
            // 
            // lblKeyStatus
            // 
            lblKeyStatus.Dock = DockStyle.Right;
            lblKeyStatus.Font = new Font("Consolas", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblKeyStatus.ForeColor = Color.White;
            lblKeyStatus.Location = new Point(970, 20);
            lblKeyStatus.Margin = new Padding(0);
            lblKeyStatus.Name = "lblKeyStatus";
            lblKeyStatus.Size = new Size(270, 39);
            lblKeyStatus.TabIndex = 2;
            lblKeyStatus.Text = "Khóa RSA: Đang kiểm tra...";
            lblKeyStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubTitle.ForeColor = Color.White;
            lblSubTitle.Location = new Point(27, 45);
            lblSubTitle.Margin = new Padding(4, 0, 4, 0);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new Size(740, 22);
            lblSubTitle.TabIndex = 1;
            lblSubTitle.Text = "Hệ thống phát hành và quản lý bản quyền phần mềm bãi đỗ xe (RSA 3072-bit)";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Consolas", 13F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(23, 9);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(518, 31);
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
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4, 5, 4, 5);
            MinimumSize = new Size(1205, 963);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Phú Xuân Parking — Công Cụ Tạo & Quản Lý License Key (Vendor Tool)";
            tabControlMain.ResumeLayout(false);
            tabGenerate.ResumeLayout(false);
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
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

        private Button btnGenerateKey;
        private GroupBox grpOutput;
        private TextBox txtGeneratedKey;
        private GroupBox grpInputKey;
        private Label lblVerifyKey;
        private TextBox txtVerifyKey;
        private Label lblVerifyMachineCode;
        private TextBox txtVerifyMachineCode;
        private Button btnVerifyInputKey;
        private Button btnOpenLicFile;
        private GroupBox grpDecodedResult;
        private TextBox txtDecodedInfo;
        private Label lblKeyStatus;
        private Button btnExportLicFile;
        private Button btnCopyKey;
    }
}
