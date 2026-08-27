using PhuXuanParkingSystem.Licensing;

namespace PhuXuanParkingSystem.LicenseTool
{
    public partial class MainForm : Form
    {
        private string _currentPrivateKeyXml = string.Empty;
        private string _currentPublicKeyXml = string.Empty;
        private readonly string _keyStoragePath;

        public MainForm()
        {
            InitializeComponent();
            _keyStoragePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PhuXuanParkingSystem",
                "VendorKeys"
            );
            EnsureKeysLoaded();
        }

        private void EnsureKeysLoaded()
        {
            try
            {
                if (!Directory.Exists(_keyStoragePath))
                    Directory.CreateDirectory(_keyStoragePath);

                string privFile = Path.Combine(_keyStoragePath, "vendor_private_key.xml");
                string pubFile = Path.Combine(_keyStoragePath, "vendor_public_key.xml");

                if (File.Exists(privFile) && File.Exists(pubFile))
                {
                    _currentPrivateKeyXml = File.ReadAllText(privFile);
                    _currentPublicKeyXml = File.ReadAllText(pubFile);
                }
                else
                {
                    // Tự động sinh cặp khóa mới lần đầu tiên
                    var (pub, priv) = LicenseCrypto.GenerateKeyPair();
                    _currentPublicKeyXml = pub;
                    _currentPrivateKeyXml = priv;

                    File.WriteAllText(pubFile, pub);
                    File.WriteAllText(privFile, priv);
                }

                lblKeyStatus.Text = "Khóa RSA 3072-bit: Đã sẵn sàng";
            }
            catch (Exception ex)
            {
                lblKeyStatus.Text = "Lỗi nạp khóa: " + ex.Message;
                lblKeyStatus.ForeColor = Color.Red;
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

                var features = new List<string>();
                if (chkAnpr.Checked) features.Add("ANPR_Vietnam");
                if (chkBarrier.Checked) features.Add("AutoBarrier");
                if (chkDualCamera.Checked) features.Add("DualCameraPerLane");
                if (chkAdvancedReport.Checked) features.Add("AdvancedReport");

                var payload = new LicensePayload
                {
                    CustomerName = customerName,
                    MachineCode = machineCode,
                    ExpiryDate = dtpExpiryDate.Value,
                    IssuedAt = DateTime.Now,
                    MaxLanes = (int)numMaxLanes.Value,
                    MaxCameras = (int)numMaxCameras.Value,
                    MaxControllers = (int)numMaxControllers.Value,
                    Features = features,
                    Note = txtNote.Text.Trim()
                };

                string licenseKey = LicenseCrypto.SignLicense(payload, _currentPrivateKeyXml);
                txtGeneratedKey.Text = licenseKey;

                // Tự động giải mã để kiểm tra lại
                var valResult = LicenseCrypto.ValidateLicense(licenseKey, _currentPublicKeyXml, machineCode);
                if (valResult.IsValid)
                {
                    lblStatusMessage.Text = $"Tạo Key thành công cho [{customerName}]! Hạn dùng: {(payload.IsPermanent ? "Vĩnh viễn" : payload.ExpiryDate.ToString("dd/MM/yyyy"))}";
                    lblStatusMessage.ForeColor = Color.DarkGreen;
                }
                else
                {
                    lblStatusMessage.Text = "Cảnh báo xác thực: " + valResult.Message;
                    lblStatusMessage.ForeColor = Color.OrangeRed;
                }
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
                    $"• Giới hạn Làn xe: {p.MaxLanes} làn\r\n" +
                    $"• Giới hạn Camera: {p.MaxCameras} camera\r\n" +
                    $"• Giới hạn Controller: {p.MaxControllers} bộ điều khiển\r\n" +
                    $"• Tính năng: {string.Join(", ", p.Features)}\r\n" +
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
