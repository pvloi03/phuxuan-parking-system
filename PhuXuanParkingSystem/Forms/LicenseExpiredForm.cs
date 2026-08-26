using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PhuXuanParkingSystem.Licensing;

namespace PhuXuanParkingSystem.Forms
{
    public partial class LicenseExpiredForm : Form
    {
        public bool IsActivatedSuccessfully { get; private set; } = false;
        public string ActivatedKey { get; private set; } = string.Empty;

        public LicenseExpiredForm(string reasonMessage = "")
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(reasonMessage))
            {
                lblReason.Text = reasonMessage;
            }
            txtMachineCode.Text = HardwareFingerprint.GetMachineCode();
        }

        private void btnCopyMachineCode_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMachineCode.Text))
            {
                Clipboard.SetText(txtMachineCode.Text);
                MessageBox.Show("Đã sao chép Mã Máy Tính vào Clipboard!\nVui lòng gửi mã này cho nhà cung cấp để nhận License Key mới.", "Sao Chép Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "License File (*.lic;*.txt)|*.lic;*.txt|All Files (*.*)|*.*";
                ofd.Title = "Chọn file bản quyền (.lic) do nhà cung cấp cấp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        txtLicenseKey.Text = File.ReadAllText(ofd.FileName).Trim();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể đọc file: " + ex.Message, "Lỗi đọc file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            string key = txtLicenseKey.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("Vui lòng dán License Key hoặc chọn File License (.lic).", "Chưa nhập Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLicenseKey.Focus();
                return;
            }

            // Xác thực bản quyền với Public Key và Machine Code hiện tại
            var result = LicenseCrypto.ValidateLicense(key);

            if (!result.IsValid)
            {
                MessageBox.Show($"Kích hoạt thất bại!\n\nChi tiết: {result.Message}", "Bản Quyền Không Hợp Lệ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Lưu License Key thành công
            ActivatedKey = key;
            IsActivatedSuccessfully = true;

            string durationText = result.Payload?.IsPermanent == true
                ? "VĨNH VIỄN"
                : $"{result.DaysRemaining} ngày (Đến ngày {result.Payload?.ExpiryDate:dd/MM/yyyy})";

            MessageBox.Show(
                $"Chúc mừng! Bản quyền phần mềm đã được kích hoạt thành công!\n\n" +
                $"• Đơn vị: {result.Payload?.CustomerName}\n" +
                $"• Thời hạn: {durationText}\n" +
                $"• Giới hạn: {result.Payload?.MaxLanes} làn xe, {result.Payload?.MaxCameras} camera\n\n" +
                $"Hệ thống sẽ chuyển sang giao diện vận hành bãi đỗ xe.",
                "Kích Hoạt Thành Công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
