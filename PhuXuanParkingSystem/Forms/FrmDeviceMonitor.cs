using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.DeviceHealth;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem.Forms
{
    public partial class FrmDeviceMonitor : Form
    {
        private readonly IDeviceHealthMonitorService _healthService;
        private readonly IRepository<Device> _deviceRepo;

        private List<Device> _devices = new();
        private readonly Dictionary<string, DevicePingResult> _latestResults = new();
        private bool _isChecking = false;
        private CancellationTokenSource? _cts;

        public FrmDeviceMonitor()
        {
            InitializeComponent();

            _deviceRepo = Program.ServiceProvider != null
                ? (Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IRepository<Device>>(Program.ServiceProvider) ?? new MongoRepository<Device>())
                : new MongoRepository<Device>();

            _healthService = Program.ServiceProvider != null
                ? (Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IDeviceHealthMonitorService>(Program.ServiceProvider) ?? new DeviceHealthMonitorService(_deviceRepo))
                : new DeviceHealthMonitorService(_deviceRepo);

            // Đăng ký sự kiện từ service
            _healthService.OnDeviceChecked += HealthService_OnDeviceChecked;
        }

        public FrmDeviceMonitor(IDeviceHealthMonitorService healthService, IRepository<Device> deviceRepo)
        {
            InitializeComponent();
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));

            _healthService.OnDeviceChecked += HealthService_OnDeviceChecked;
        }

        private async void FrmDeviceMonitor_Load(object sender, EventArgs e)
        {
            cboAutoCheckInterval.SelectedIndex = 1; // Mặc định mỗi 30 giây
            await LoadDevicesAndCheckAllAsync();
        }

        private void HealthService_OnDeviceChecked(object? sender, DevicePingResult e)
        {
            if (e?.Device == null) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateDeviceRow(e)));
            }
            else
            {
                UpdateDeviceRow(e);
            }
        }

        private async Task LoadDevicesAndCheckAllAsync()
        {
            if (_isChecking) return;

            try
            {
                SetStatus("Đang tải danh sách thiết bị từ CSDL...", true);
                _devices = (await _deviceRepo.FindAsync(d => !d.IsDeleted)).ToList();

                if (_devices.Count == 0)
                {
                    SetStatus("Chưa có thiết bị nào được cấu hình trong hệ thống.", false);
                    UpdateStats(0, 0, 0);
                    dgvDevices.Rows.Clear();
                    return;
                }

                // Nạp trước vào grid với trạng thái ban đầu
                PopulateGrid();

                // Tiến hành kiểm tra kết nối đồng thời
                await CheckAllDevicesAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi nạp danh sách thiết bị");
                SetStatus($"Lỗi: {ex.Message}", false);
            }
        }

        private void PopulateGrid()
        {
            dgvDevices.Rows.Clear();

            foreach (var dev in _devices)
            {
                string typeName = GetDeviceTypeName(dev.Type);
                string statusText = dev.Status == DeviceStatus.Connected ? "🟢 Đang kết nối" : "🔴 Mất kết nối";
                string heartbeatText = dev.LastHeartbeat.HasValue ? dev.LastHeartbeat.Value.ToString("HH:mm:ss dd/MM") : "Chưa có";

                int rowIndex = dgvDevices.Rows.Add(
                    statusText,
                    typeName,
                    dev.Code,
                    dev.Name,
                    dev.IpAddress,
                    dev.Port > 0 ? dev.Port.ToString() : "-",
                    "-",
                    heartbeatText,
                    dev.ErrorMessage ?? "Sẵn sàng kiểm tra"
                );

                var row = dgvDevices.Rows[rowIndex];
                row.Tag = dev;

                // Tô màu theo trạng thái
                if (dev.Status == DeviceStatus.Connected)
                {
                    row.Cells[0].Style.ForeColor = Color.FromArgb(40, 140, 70);
                }
                else
                {
                    row.Cells[0].Style.ForeColor = Color.FromArgb(200, 40, 40);
                }
            }

            int onlineCount = _devices.Count(d => d.Status == DeviceStatus.Connected);
            UpdateStats(_devices.Count, onlineCount, _devices.Count - onlineCount);
        }

        private async Task CheckAllDevicesAsync()
        {
            if (_isChecking) return;

            _isChecking = true;
            btnCheckAll.Enabled = false;
            btnCheckSelected.Enabled = false;
            SetStatus("Đang kiểm tra kết nối tất cả thiết bị...", true);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                // Đánh dấu tất cả row thành Đang kiểm tra
                foreach (DataGridViewRow row in dgvDevices.Rows)
                {
                    row.Cells[0].Value = "🟡 Đang kiểm tra...";
                    row.Cells[0].Style.ForeColor = Color.FromArgb(200, 140, 0);
                    row.Cells[6].Value = "...";
                }

                var results = await _healthService.CheckAllAndSyncAsync(_cts.Token);

                int online = results.Count(r => r.IsSuccess);
                int offline = results.Count - online;
                UpdateStats(results.Count, online, offline);

                SetStatus($"Hoàn tất kiểm tra lúc {DateTime.Now:HH:mm:ss}: {online} Online, {offline} Offline. Đã đồng bộ lên Web Admin.", false);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Đã hủy quá trình kiểm tra.", false);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi kiểm tra toàn bộ thiết bị");
                SetStatus($"Lỗi kiểm tra: {ex.Message}", false);
            }
            finally
            {
                _isChecking = false;
                btnCheckAll.Enabled = true;
                btnCheckSelected.Enabled = true;
            }
        }

        private void UpdateDeviceRow(DevicePingResult result)
        {
            if (result?.Device == null) return;

            _latestResults[result.Device.Id] = result;

            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (row.Tag is Device dev && dev.Id == result.Device.Id)
                {
                    if (result.IsSuccess)
                    {
                        row.Cells[0].Value = "🟢 Đang kết nối";
                        row.Cells[0].Style.ForeColor = Color.FromArgb(40, 140, 70);
                        row.Cells[6].Value = $"{result.LatencyMs} ms";
                        row.Cells[6].Style.ForeColor = result.LatencyMs < 50 ? Color.FromArgb(40, 140, 70) : Color.FromArgb(200, 140, 0);
                        row.Cells[7].Value = result.CheckedAt.ToString("HH:mm:ss dd/MM");
                        row.Cells[8].Value = result.Details;
                    }
                    else
                    {
                        row.Cells[0].Value = "🔴 Mất kết nối";
                        row.Cells[0].Style.ForeColor = Color.FromArgb(200, 40, 40);
                        row.Cells[6].Value = $"{result.LatencyMs} ms";
                        row.Cells[6].Style.ForeColor = Color.FromArgb(200, 40, 40);
                        row.Cells[7].Value = result.CheckedAt.ToString("HH:mm:ss dd/MM");
                        row.Cells[8].Value = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Không có phản hồi" : result.ErrorMessage;
                    }
                    break;
                }
            }

            // Cập nhật lại số liệu thống kê nhanh
            int total = dgvDevices.Rows.Count;
            int online = 0;
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (row.Cells[0].Value?.ToString()?.Contains("🟢") == true) online++;
            }
            UpdateStats(total, online, total - online);
        }

        private async void BtnCheckSelected_Click(object sender, EventArgs e)
        {
            if (dgvDevices.SelectedRows.Count == 0 || _isChecking) return;

            var selectedRow = dgvDevices.SelectedRows[0];
            if (selectedRow.Tag is not Device device) return;

            btnCheckSelected.Enabled = false;
            selectedRow.Cells[0].Value = "🟡 Đang kiểm tra...";
            selectedRow.Cells[0].Style.ForeColor = Color.FromArgb(200, 140, 0);
            selectedRow.Cells[6].Value = "...";
            SetStatus($"Đang kiểm tra thiết bị: {device.Name} ({device.IpAddress}:{device.Port})...", true);

            try
            {
                var result = await _healthService.PingDeviceAsync(device, 3000);
                await _healthService.SyncStatusToDbAsync(result);
                UpdateDeviceRow(result);
                SetStatus($"Hoàn tất kiểm tra '{device.Name}': {(result.IsSuccess ? "Online (" + result.LatencyMs + "ms)" : "Offline")}. Đã đồng bộ CSDL.", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi kiểm tra '{device.Name}': {ex.Message}", false);
            }
            finally
            {
                btnCheckSelected.Enabled = true;
            }
        }

        private async void BtnCheckAll_Click(object sender, EventArgs e)
        {
            await CheckAllDevicesAsync();
        }

        private void UpdateStats(int total, int online, int offline)
        {
            lblStatTotalVal.Text = total.ToString();
            lblStatOnlineVal.Text = online.ToString();
            lblStatOfflineVal.Text = offline.ToString();
        }

        private void SetStatus(string message, bool isBusy)
        {
            lblFooterStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            prgStatus.Visible = isBusy;
        }

        private void CboAutoCheckInterval_SelectedIndexChanged(object sender, EventArgs e)
        {
            timerAutoCheck.Stop();

            switch (cboAutoCheckInterval.SelectedIndex)
            {
                case 0: // 10s
                    timerAutoCheck.Interval = 10000;
                    timerAutoCheck.Start();
                    break;
                case 1: // 30s
                    timerAutoCheck.Interval = 30000;
                    timerAutoCheck.Start();
                    break;
                case 2: // 1m
                    timerAutoCheck.Interval = 60000;
                    timerAutoCheck.Start();
                    break;
                case 3: // 5m
                    timerAutoCheck.Interval = 300000;
                    timerAutoCheck.Start();
                    break;
                case 4: // Tắt
                default:
                    timerAutoCheck.Stop();
                    break;
            }
        }

        private async void TimerAutoCheck_Tick(object sender, EventArgs e)
        {
            if (!_isChecking)
            {
                await CheckAllDevicesAsync();
            }
        }

        private void FrmDeviceMonitor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                _ = CheckAllDevicesAsync();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FrmDeviceMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            timerAutoCheck.Stop();
            _cts?.Cancel();
            _healthService.OnDeviceChecked -= HealthService_OnDeviceChecked;
        }

        private string GetDeviceTypeName(DeviceType type)
        {
            return type switch
            {
                DeviceType.PlateCamera => "📷 Camera Biển Số",
                DeviceType.OverviewCamera => "📹 Camera Toàn Cảnh",
                DeviceType.Controller => "🎛️ Access Controller",
                _ => "⚙️ Thiết Bị Khác"
            };
        }
    }
}
