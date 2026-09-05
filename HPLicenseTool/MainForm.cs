using PhuXuanParkingSystem.Licensing;

namespace HPLicenseTool
{
    public partial class MainForm : Form
    {
        private string _currentPrivateKeyXml = string.Empty;
        private string _currentPublicKeyXml = string.Empty;
        public MainForm()
        {
            InitializeComponent();
            EnsureKeysLoaded();
        }

        private void EnsureKeysLoaded()
        {
            try
            {
                // Nạp cặp khóa RSA 3072-bit từ 1 nguồn duy nhất: file App.config
                _currentPrivateKeyXml = LicenseCrypto.GetConfiguredPrivateKey();
                _currentPublicKeyXml = LicenseCrypto.GetConfiguredPublicKey();

                lblKeyStatus.Text = "Khóa RSA 3072-bit: Đã nạp từ App.config";
            }
            catch (Exception ex)
            {
                _currentPrivateKeyXml = string.Empty;
                _currentPublicKeyXml = string.Empty;
                lblKeyStatus.Text = "Lỗi nạp khóa từ App.config: " + ex.Message;
                lblKeyStatus.ForeColor = Color.Red;

                MessageBox.Show(
                    "Không thể nạp khóa bản quyền:\n" + ex.Message + "\n\nVui lòng cấu hình License_PrivateKey và License_PublicKey trong file App.config của Tool.",
                    "Lỗi Cấu Hình Khóa Bản Quyền",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnGetThisMachineCode_Click(object sender, EventArgs e)
        {
            txtMachineCode.Text = HardwareFingerprint.GetMachineCode();
        }

        private void btnPasteMachineCode_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                txtMachineCode.Text = Clipboard.GetText().Trim();
            }
        }

        private void radDuration_CheckedChanged(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            if (rad30Days.Checked)
            {
                dtpExpiryDate.Value = now.AddDays(30);
                dtpExpiryDate.Enabled = false;
            }
            else if (rad90Days.Checked)
            {
                dtpExpiryDate.Value = now.AddDays(90);
                dtpExpiryDate.Enabled = false;
            }
            else if (rad1Year.Checked)
            {
                dtpExpiryDate.Value = now.AddDays(365);
                dtpExpiryDate.Enabled = false;
            }
            else if (rad3Years.Checked)
            {
                dtpExpiryDate.Value = now.AddDays(365 * 3);
                dtpExpiryDate.Enabled = false;
            }
            else if (radPermanent.Checked)
            {
                dtpExpiryDate.Value = new DateTime(2099, 12, 31, 23, 59, 59);
                dtpExpiryDate.Enabled = false;
            }
            else if (radCustom.Checked)
            {
                dtpExpiryDate.Enabled = true;
            }
        }

        private void btnGenerateKey_Click(object sender, EventArgs e)
        {
            try
            {
                string customerName = txtCustomerName.Text.Trim();
                string machineCode = txtMachineCode.Text.Trim();

                if (string.IsNullOrWhiteSpace(customerName))
                {
                    MessageBox.Show("Vui lòng nhập Tên Khách Hàng / Tên Bãi Đỗ Xe.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCustomerName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(machineCode))
                {
                    MessageBox.Show("Vui lòng nhập Mã Máy Tính (Machine Code) của khách hàng.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMachineCode.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_currentPrivateKeyXml))
                {
                    MessageBox.Show("Chưa có RSA Private Key để ký số. Vui lòng kiểm tra lại cấu hình khóa.", "Lỗi khóa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var payload = new LicensePayload
                {
                    CustomerName = customerName,
                    MachineCode = machineCode,
                    ExpiryDate = dtpExpiryDate.Value,
                    IssuedAt = DateTime.Now,
                    Note = txtNote.Text.Trim()
                };

                string licenseKey = LicenseCrypto.SignLicense(payload, _currentPrivateKeyXml);
                txtGeneratedKey.Text = licenseKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tạo License Key: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCopyKey_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtGeneratedKey.Text))
            {
                Clipboard.SetText(txtGeneratedKey.Text);
                MessageBox.Show("Đã sao chép License Key vào Clipboard!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExportLicFile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtGeneratedKey.Text))
            {
                MessageBox.Show("Chưa có License Key nào được tạo để xuất file.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "License File (*.lic)|*.lic|All Files (*.*)|*.*";
                sfd.FileName = $"License_{txtCustomerName.Text.Trim().Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.lic";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, txtGeneratedKey.Text);
                    MessageBox.Show($"Đã xuất file bản quyền thành công:\n{sfd.FileName}", "Xuất File Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnVerifyInputKey_Click(object sender, EventArgs e)
        {
            string keyToVerify = txtVerifyKey.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyToVerify))
            {
                MessageBox.Show("Vui lòng dán License Key cần kiểm tra.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = LicenseCrypto.ValidateLicense(keyToVerify, _currentPublicKeyXml, txtVerifyMachineCode.Text.Trim());

            if (result.Payload != null)
            {
                var p = result.Payload;
                txtDecodedInfo.Text =
                    $"================ THÔNG TIN BẢN QUYỀN ================\r\n" +
                    $"• Khách hàng: {p.CustomerName}\r\n" +
                    $"• Mã máy tính: {p.MachineCode}\r\n" +
                    $"• Ngày cấp: {p.IssuedAt:dd/MM/yyyy HH:mm:ss}\r\n" +
                    $"• Hạn sử dụng: {(p.IsPermanent ? "VĨNH VIỄN" : p.ExpiryDate.ToString("dd/MM/yyyy HH:mm:ss"))}\r\n" +
                    $"• Số ngày còn lại: {(p.IsPermanent ? "Không giới hạn" : result.DaysRemaining + " ngày")}\r\n" +
                    $"• Ghi chú: {p.Note ?? "--"}\r\n" +
                    $"------------------------------------------------------\r\n" +
                    $"• TRẠNG THÁI: {(result.IsValid ? "✅ HỢP LỆ & KHỚP CHỮ KÝ" : "❌ KHÔNG HỢP LỆ / HẾT HẠN")}\r\n" +
                    $"• Chi tiết: {result.Message}";

                txtDecodedInfo.ForeColor = result.IsValid ? Color.DarkGreen : Color.DarkRed;
            }
            else
            {
                txtDecodedInfo.Text = "❌ Không thể giải mã key: " + result.Message;
                txtDecodedInfo.ForeColor = Color.Red;
            }
        }

        private void btnOpenLicFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "License File (*.lic;*.txt)|*.lic;*.txt|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtVerifyKey.Text = File.ReadAllText(ofd.FileName);
                    btnVerifyInputKey_Click(sender, e);
                }
            }
        }
    }
}
