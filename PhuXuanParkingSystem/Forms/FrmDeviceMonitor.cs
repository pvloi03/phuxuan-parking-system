using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Devices.Health;
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
        private readonly IDeviceAdapterFactory _adapterFactory;

        private List<Device> _devices = [];
        private readonly Dictionary<string, DevicePingResult> _latestResults = [];
        private readonly Dictionary<string, DeviceStatus> _deviceStates = [];
        private bool _isChecking = false;
        private CancellationTokenSource? _cts;

        public FrmDeviceMonitor()
        {
            InitializeComponent();

            _deviceRepo = Program.ServiceProvider != null
                ? (Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IRepository<Device>>(Program.ServiceProvider) ?? new MongoRepository<Device>())
                : new MongoRepository<Device>();

            _adapterFactory = Program.ServiceProvider != null
                ? (Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IDeviceAdapterFactory>(Program.ServiceProvider) ?? new DeviceAdapterFactory())
                : new DeviceAdapterFactory();

            _healthService = Program.ServiceProvider != null
                ? (Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IDeviceHealthMonitorService>(Program.ServiceProvider) ?? new DeviceHealthMonitorService(_deviceRepo, _adapterFactory))
                : new DeviceHealthMonitorService(_deviceRepo, _adapterFactory);

            // Đăng ký sự kiện từ service
            _healthService.OnDeviceChecked += HealthService_OnDeviceChecked;
        }

        public FrmDeviceMonitor(IDeviceHealthMonitorService healthService, IRepository<Device> deviceRepo, IDeviceAdapterFactory adapterFactory)
        {
            InitializeComponent();

            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));

            _healthService.OnDeviceChecked += HealthService_OnDeviceChecked;
        }

        private async void FrmDeviceMonitor_Load(object sender, EventArgs e)
        {
            cboAutoCheckInterval.SelectedIndex = 1; // Mặc định mỗi 30 giây
            await LoadDevicesAndCheckAllAsync();
        }

        /// <summary>
        /// Lấy text và màu hiển thị cho trạng thái
        /// </summary>
        private static (string text, Color color) GetStateDisplayInfo(DeviceStatus state)
        {
            return state switch
            {
                DeviceStatus.Disconnected => ("⚪ Chưa kết nối", Color.FromArgb(150, 150, 150)),
                DeviceStatus.Connecting => ("🟡 Đang kết nối...", Color.FromArgb(200, 140, 0)),
                DeviceStatus.Connected => ("🟢 Đã kết nối", Color.FromArgb(40, 140, 70)),
                DeviceStatus.Streaming => ("🔵 Đang Streaming", Color.FromArgb(0, 123, 255)),
                DeviceStatus.Error => ("🔴 Lỗi kết nối", Color.FromArgb(200, 40, 40)),
                DeviceStatus.Maintenance => ("🟠 Đang bảo trì", Color.FromArgb(230, 120, 0)),
                _ => ("❓ Không xác định", Color.FromArgb(100, 100, 100))
            };
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
                _devices = (await _deviceRepo.FindAsync(d => !d.IsDeleted && d.IsActive)).ToList();

                if (_devices.Count == 0)
                {
                    SetStatus("Chưa có thiết bị nào được cấu hình trong hệ thống.", false);
                    UpdateStats(0, 0, 0, 0);
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
            _deviceStates.Clear();

            foreach (var dev in _devices)
            {
                string typeName = GetDeviceTypeName(dev.Type);
                string heartbeatText = dev.LastHeartbeat.HasValue ? dev.LastHeartbeat.Value.ToString("HH:mm:ss dd/MM") : "Chưa có";

                // Khởi tạo state từ Device.Status
                var initialState = dev.Status;
                _deviceStates[dev.Id] = initialState;

                var (statusText, statusColor) = GetStateDisplayInfo(initialState);

                int rowIndex = dgvDevices.Rows.Add(
                    statusText,
                    typeName,
                    dev.Name,
                    dev.IpAddress,
                    dev.Port > 0 ? dev.Port.ToString() : "-",
                    "-",
                    heartbeatText,
                    dev.ErrorMessage ?? "Sẵn sàng kiểm tra"
                );

                var row = dgvDevices.Rows[rowIndex];
                row.Tag = dev;
                row.Cells[colStatus.Index].Style.ForeColor = statusColor;
            }

            UpdateStatsFromStates();
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
                    row.Cells[colStatus.Index].Value = "🟡 Đang kiểm tra...";
                    row.Cells[colStatus.Index].Style.ForeColor = Color.FromArgb(200, 140, 0);
                    row.Cells[colLatency.Index].Value = "...";
                }

                var results = await _healthService.CheckAllAndSyncAsync(_cts.Token);

                int online = results.Count(r => r.IsSuccess);
                int offline = results.Count - online;
                int streaming = _deviceStates.Values.Count(s => s == DeviceStatus.Streaming);

                UpdateStats(results.Count, online, offline, streaming);

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

            // Convert ping result to status
            var state = result.IsSuccess ? DeviceStatus.Connected : DeviceStatus.Error;
            _deviceStates[result.Device.Id] = state;

            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (row.Tag is Device dev && dev.Id == result.Device.Id)
                {
                    var (statusText, statusColor) = GetStateDisplayInfo(state);
                    row.Cells[colStatus.Index].Value = statusText;
                    row.Cells[colStatus.Index].Style.ForeColor = statusColor;

                    if (result.IsSuccess)
                    {
                        row.Cells[colLatency.Index].Value = $"{result.LatencyMs} ms";
                        row.Cells[colLatency.Index].Style.ForeColor = result.LatencyMs < 50 ? Color.FromArgb(40, 140, 70) : Color.FromArgb(200, 140, 0);
                        row.Cells[colLastHeartbeat.Index].Value = result.CheckedAt.ToString("HH:mm:ss dd/MM");
                        row.Cells[colDetails.Index].Value = result.Details;
                    }
                    else
                    {
                        row.Cells[colLatency.Index].Value = $"{result.LatencyMs} ms";
                        row.Cells[colLatency.Index].Style.ForeColor = Color.FromArgb(200, 40, 40);
                        row.Cells[colLastHeartbeat.Index].Value = result.CheckedAt.ToString("HH:mm:ss dd/MM");
                        row.Cells[colDetails.Index].Value = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Không có phản hồi" : result.ErrorMessage;
                    }
                    break;
                }
            }

            UpdateStatsFromStates();
        }

        private void UpdateStatsFromStates()
        {
            int total = _deviceStates.Count;
            int online = _deviceStates.Values.Count(s => s == DeviceStatus.Connected || s == DeviceStatus.Streaming);
            int offline = _deviceStates.Values.Count(s => s == DeviceStatus.Error || s == DeviceStatus.Disconnected);
            int streaming = _deviceStates.Values.Count(s => s == DeviceStatus.Streaming);

            UpdateStats(total, online, offline, streaming);
        }

        private async void BtnCheckSelected_Click(object sender, EventArgs e)
        {
            if (dgvDevices.SelectedRows.Count == 0 || _isChecking) return;

            var selectedRow = dgvDevices.SelectedRows[0];
            if (selectedRow.Tag is not Device device) return;

            btnCheckSelected.Enabled = false;
            selectedRow.Cells[colStatus.Index].Value = "🟡 Đang kiểm tra...";
            selectedRow.Cells[colStatus.Index].Style.ForeColor = Color.FromArgb(200, 140, 0);
            selectedRow.Cells[colLatency.Index].Value = "...";
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

        private void UpdateStats(int total, int online, int offline, int streaming)
        {
            lblStatTotalVal.Text = total.ToString();
            lblStatOnlineVal.Text = online.ToString();
            lblStatOfflineVal.Text = offline.ToString();

            // Streaming count có thể được thêm vào tooltip hoặc footer
            if (streaming > 0)
            {
                // Update footer để hiển thị streaming count
                var currentStatus = lblFooterStatus.Text;
                if (!currentStatus.Contains("🔵"))
                {
                    lblFooterStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {streaming} thiết bị đang Streaming";
                }
            }
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

        private async void DgvDevices_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Chỉ xử lý khi click vào cột Restart (colRestart)
            if (e.ColumnIndex != colRestart.Index || e.RowIndex < 0) return;

            var row = dgvDevices.Rows[e.RowIndex];
            if (row.Tag is not Device device) return;

            // Xác nhận trước khi restart
            var confirmResult = MessageBox.Show(
                $"Bạn có chắc muốn khởi động lại thiết bị?\n\n" +
                $"Tên: {device.Name}\n" +
                $"IP: {device.IpAddress}:{device.Port}\n" +
                $"Loại: {GetDeviceTypeName(device.Type)}",
                "Xác nhận Khởi Động Lại",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes) return;

            // Disable row và hiển thị trạng thái đang restart
            row.Cells[colRestart.Index] = new DataGridViewTextBoxCell { Value = "⏳..." };
            row.Cells[colRestart.Index].Style.BackColor = Color.FromArgb(230, 230, 230);
            row.Cells[colStatus.Index].Value = "🟡 Đang restart...";
            row.Cells[colStatus.Index].Style.ForeColor = Color.FromArgb(200, 140, 0);
            SetStatus($"Đang khởi động lại thiết bị: {device.Name}...", true);

            try
            {
                var adapter = _adapterFactory.GetAdapter(device);
                var success = await adapter.RestartAsync(device);

                if (success)
                {
                    row.Cells[colStatus.Index].Value = "🟢 Đã restart";
                    row.Cells[colStatus.Index].Style.ForeColor = Color.FromArgb(40, 140, 70);
                    row.Cells[colDetails.Index].Value = "Restart thành công";

                    // Sync status lên MongoDB
                    var pingResult = new DevicePingResult
                    {
                        Device = device,
                        IsSuccess = true,
                        LatencyMs = 0,
                        Details = "Restart thành công",
                        CheckedAt = DateTime.Now
                    };
                    await _healthService.SyncStatusToDbAsync(pingResult);

                    SetStatus($"Khởi động lại thành công: {device.Name}", false);
                }
                else
                {
                    row.Cells[colStatus.Index].Value = "🔴 Restart thất bại";
                    row.Cells[colStatus.Index].Style.ForeColor = Color.FromArgb(200, 40, 40);
                    row.Cells[colDetails.Index].Value = "Không thể kết nối sau khi restart";

                    SetStatus($"Khởi động lại thất bại: {device.Name}", false);
                }
            }
            catch (Exception ex)
            {
                row.Cells[colStatus.Index].Value = "🔴 Lỗi restart";
                row.Cells[colStatus.Index].Style.ForeColor = Color.FromArgb(200, 40, 40);
                row.Cells[colDetails.Index].Value = $"Lỗi: {ex.Message}";
                AppLogger.Error(ex, $"Lỗi restart thiết bị {device.Name}");
                SetStatus($"Lỗi restart: {device.Name} - {ex.Message}", false);
            }
            finally
            {
                // Khôi phục button
                var btnCell = new DataGridViewButtonCell
                {
                    Value = "🔄 Restart",
                    UseColumnTextForButtonValue = true
                };
                row.Cells[colRestart.Index] = btnCell;
            }
        }
    }
}
