namespace PhuXuanParkingSystem.Forms
{
    partial class LicenseExpiredForm
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblReason = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlBody = new System.Windows.Forms.Panel();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnActivate = new System.Windows.Forms.Button();
            this.grpKeyInput = new System.Windows.Forms.GroupBox();
            this.btnBrowseFile = new System.Windows.Forms.Button();
            this.txtLicenseKey = new System.Windows.Forms.TextBox();
            this.lblKeyHelp = new System.Windows.Forms.Label();
            this.grpMachine = new System.Windows.Forms.GroupBox();
            this.btnCopyMachineCode = new System.Windows.Forms.Button();
            this.txtMachineCode = new System.Windows.Forms.TextBox();
            this.lblMachineHelp = new System.Windows.Forms.Label();
            this.pnlHeader.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.grpKeyInput.SuspendLayout();
            this.grpMachine.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.Tomato;
            this.pnlHeader.Controls.Add(this.lblReason);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.pnlHeader.Size = new System.Drawing.Size(1060, 86);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblReason
            // 
            this.lblReason.AutoSize = true;
            this.lblReason.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblReason.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.lblReason.Location = new System.Drawing.Point(24, 49);
            this.lblReason.Name = "lblReason";
            this.lblReason.Size = new System.Drawing.Size(742, 25);
            this.lblReason.TabIndex = 1;
            this.lblReason.Text = "Bản quyền phần mềm đã hết hạn sử dụng. Vui lòng liên hệ nhà cung cấp để gia hạn d" +
    "ịch vụ.";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(16, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(528, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "🔒 THÔNG BÁO BẢN QUYỀN PHẦN MỀM";
            // 
            // pnlBody
            // 
            this.pnlBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlBody.Controls.Add(this.btnExit);
            this.pnlBody.Controls.Add(this.btnActivate);
            this.pnlBody.Controls.Add(this.grpKeyInput);
            this.pnlBody.Controls.Add(this.grpMachine);
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.Location = new System.Drawing.Point(0, 86);
            this.pnlBody.Name = "pnlBody";
            this.pnlBody.Padding = new System.Windows.Forms.Padding(16);
            this.pnlBody.Size = new System.Drawing.Size(1060, 551);
            this.pnlBody.TabIndex = 1;
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.BackColor = System.Drawing.Color.Tomato;
            this.btnExit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.Location = new System.Drawing.Point(926, 498);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(123, 42);
            this.btnExit.TabIndex = 3;
            this.btnExit.Text = "❌ Thoát";
            this.btnExit.UseVisualStyleBackColor = false;
            // 
            // btnActivate
            // 
            this.btnActivate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnActivate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(185)))), ((int)(((byte)(129)))));
            this.btnActivate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnActivate.FlatAppearance.BorderSize = 0;
            this.btnActivate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActivate.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnActivate.ForeColor = System.Drawing.Color.White;
            this.btnActivate.Location = new System.Drawing.Point(655, 497);
            this.btnActivate.Name = "btnActivate";
            this.btnActivate.Size = new System.Drawing.Size(265, 42);
            this.btnActivate.TabIndex = 2;
            this.btnActivate.Text = "⚡ KÍCH HOẠT NGAY";
            this.btnActivate.UseVisualStyleBackColor = false;
            // 
            // grpKeyInput
            // 
            this.grpKeyInput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpKeyInput.Controls.Add(this.btnBrowseFile);
            this.grpKeyInput.Controls.Add(this.txtLicenseKey);
            this.grpKeyInput.Controls.Add(this.lblKeyHelp);
            this.grpKeyInput.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpKeyInput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.grpKeyInput.Location = new System.Drawing.Point(13, 176);
            this.grpKeyInput.Name = "grpKeyInput";
            this.grpKeyInput.Padding = new System.Windows.Forms.Padding(12);
            this.grpKeyInput.Size = new System.Drawing.Size(1028, 271);
            this.grpKeyInput.TabIndex = 1;
            this.grpKeyInput.TabStop = false;
            this.grpKeyInput.Text = "2. Nạp License Key Hoặc File Bản Quyền Mới";
            // 
            // btnBrowseFile
            // 
            this.btnBrowseFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseFile.BackColor = System.Drawing.Color.Teal;
            this.btnBrowseFile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseFile.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowseFile.ForeColor = System.Drawing.Color.White;
            this.btnBrowseFile.Location = new System.Drawing.Point(836, 14);
            this.btnBrowseFile.Name = "btnBrowseFile";
            this.btnBrowseFile.Size = new System.Drawing.Size(192, 36);
            this.btnBrowseFile.TabIndex = 2;
            this.btnBrowseFile.Text = "📂 Chọn File (.lic)";
            this.btnBrowseFile.UseVisualStyleBackColor = false;
            // 
            // txtLicenseKey
            // 
            this.txtLicenseKey.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLicenseKey.BackColor = System.Drawing.Color.White;
            this.txtLicenseKey.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLicenseKey.Location = new System.Drawing.Point(12, 76);
            this.txtLicenseKey.Multiline = true;
            this.txtLicenseKey.Name = "txtLicenseKey";
            this.txtLicenseKey.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLicenseKey.Size = new System.Drawing.Size(1004, 183);
            this.txtLicenseKey.TabIndex = 1;
            // 
            // lblKeyHelp
            // 
            this.lblKeyHelp.AutoSize = true;
            this.lblKeyHelp.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblKeyHelp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblKeyHelp.Location = new System.Drawing.Point(12, 25);
            this.lblKeyHelp.Name = "lblKeyHelp";
            this.lblKeyHelp.Size = new System.Drawing.Size(521, 23);
            this.lblKeyHelp.TabIndex = 0;
            this.lblKeyHelp.Text = "Dán chuỗi License Key hoặc bấm nút Chọn File .lic để nạp tự động:";
            // 
            // grpMachine
            // 
            this.grpMachine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpMachine.Controls.Add(this.btnCopyMachineCode);
            this.grpMachine.Controls.Add(this.txtMachineCode);
            this.grpMachine.Controls.Add(this.lblMachineHelp);
            this.grpMachine.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpMachine.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.grpMachine.Location = new System.Drawing.Point(13, 30);
            this.grpMachine.Name = "grpMachine";
            this.grpMachine.Padding = new System.Windows.Forms.Padding(12);
            this.grpMachine.Size = new System.Drawing.Size(1028, 131);
            this.grpMachine.TabIndex = 0;
            this.grpMachine.TabStop = false;
            this.grpMachine.Text = "1. Mã Máy Tính Của Trạm Này (Machine Code)";
            // 
            // btnCopyMachineCode
            // 
            this.btnCopyMachineCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyMachineCode.BackColor = System.Drawing.Color.Teal;
            this.btnCopyMachineCode.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCopyMachineCode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopyMachineCode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCopyMachineCode.ForeColor = System.Drawing.Color.White;
            this.btnCopyMachineCode.Location = new System.Drawing.Point(875, 54);
            this.btnCopyMachineCode.Name = "btnCopyMachineCode";
            this.btnCopyMachineCode.Size = new System.Drawing.Size(150, 36);
            this.btnCopyMachineCode.TabIndex = 2;
            this.btnCopyMachineCode.Text = "📋 Sao Chép Mã";
            this.btnCopyMachineCode.UseVisualStyleBackColor = false;
            // 
            // txtMachineCode
            // 
            this.txtMachineCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMachineCode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.txtMachineCode.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMachineCode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.txtMachineCode.Location = new System.Drawing.Point(12, 54);
            this.txtMachineCode.Name = "txtMachineCode";
            this.txtMachineCode.ReadOnly = true;
            this.txtMachineCode.Size = new System.Drawing.Size(846, 36);
            this.txtMachineCode.TabIndex = 1;
            this.txtMachineCode.Text = "PX-0000-0000-0000-0000";
            // 
            // lblMachineHelp
            // 
            this.lblMachineHelp.AutoSize = true;
            this.lblMachineHelp.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMachineHelp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblMachineHelp.Location = new System.Drawing.Point(12, 28);
            this.lblMachineHelp.Name = "lblMachineHelp";
            this.lblMachineHelp.Size = new System.Drawing.Size(676, 23);
            this.lblMachineHelp.TabIndex = 0;
            this.lblMachineHelp.Text = "Sao chép mã máy tính bên dưới và gửi cho nhà cung cấp để được cấp License Key mới" +
    ":";
            // 
            // LicenseExpiredForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1060, 637);
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LicenseExpiredForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Thông Báo Hết Hạn Bản Quyền";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlBody.ResumeLayout(false);
            this.grpKeyInput.ResumeLayout(false);
            this.grpKeyInput.PerformLayout();
            this.grpMachine.ResumeLayout(false);
            this.grpMachine.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblReason;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.GroupBox grpMachine;
        private System.Windows.Forms.Label lblMachineHelp;
        private System.Windows.Forms.TextBox txtMachineCode;
        private System.Windows.Forms.Button btnCopyMachineCode;
        private System.Windows.Forms.GroupBox grpKeyInput;
        private System.Windows.Forms.Label lblKeyHelp;
        private System.Windows.Forms.TextBox txtLicenseKey;
        private System.Windows.Forms.Button btnBrowseFile;
        private System.Windows.Forms.Button btnActivate;
        private System.Windows.Forms.Button btnExit;
    }
}
