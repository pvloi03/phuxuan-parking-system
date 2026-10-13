using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using HPParkingSystem.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingSystem.Services.Devices.Config
{
    /// <summary>
    /// Triển khai IDeviceConfigService - Quản lý cấu hình thiết bị với Cache + Reload động
    /// </summary>
    public class DeviceConfigService(IRepository<Lane> laneRepo, IRepository<Device> deviceRepo) : IDeviceConfigService, IDisposable
    {
        private readonly IRepository<Lane> _laneRepo = laneRepo ?? throw new ArgumentNullException(nameof(laneRepo));
        private readonly IRepository<Device> _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));

        private DeviceConfigResult _currentConfig = new();
        private string _lastConfigHash = string.Empty;
        private Timer? _monitorTimer;
        private bool _isMonitoring;
        private readonly object _lock = new();

        public event EventHandler<ConfigChangeEventArgs>? OnConfigChanged;

        public DeviceConfigResult CurrentConfig => _currentConfig;

        /// <summary>
        /// Nạp cấu hình từ MongoDB với logging chi tiết
        /// </summary>
        public async Task<DeviceConfigResult> LoadConfigAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var result = new DeviceConfigResult();

            AppLogger.Information("[DeviceConfig] Bắt đầu nạp cấu hình từ MongoDB...");

            try
            {
                // 1. Query Lanes và Devices song song để tăng tốc (Lấy các bản ghi chưa bị xóa)
                var lanesTask = _laneRepo.FindAsync(l => !l.IsDeleted && l.IsActive, cancellationToken);
                var devicesTask = _deviceRepo.FindAsync(d => !d.IsDeleted, cancellationToken);

                await Task.WhenAll(lanesTask, devicesTask);

                var lanes = await lanesTask;
                var devices = await devicesTask;

                result.LoadTime = sw.Elapsed;
                AppLogger.Information($"[DeviceConfig] Query MongoDB hoàn tất: {lanes?.Count ?? 0} Lanes, {devices?.Count ?? 0} Devices trong {sw.ElapsedMilliseconds}ms");

                if (devices == null || devices.Count == 0)
                {
                    result.Warnings.Add("Không tìm thấy thiết bị nào trong CSDL MongoDB");
                    AppLogger.Warning("[DeviceConfig] Không tìm thấy thiết bị nào");
                    return result;
                }

                // 2. Populate Navigation Properties
                foreach (var lane in lanes ?? Enumerable.Empty<Lane>())
                {
                    lane.PlateCamera = devices.FirstOrDefault(d => d.Id == lane.PlateCameraDeviceId);
                    lane.OverviewCamera = devices.FirstOrDefault(d => d.Id == lane.OverviewCameraDeviceId);
                    lane.Controller = devices.FirstOrDefault(d => d.Id == lane.ControllerDeviceId);

                    LogDeviceMapping(lane.Code, lane.Direction, lane.PlateCamera, lane.OverviewCamera, lane.Controller);
                }

                // 3. Áp dụng vào result theo TriggerAuxPort (1 = Cột Trái / Slot 1, 2 = Cột Phải / Slot 2)
                var activeLanes = (lanes ?? Enumerable.Empty<Lane>()).Where(l => !l.IsDeleted && l.IsActive).ToList();
                Lane? lane1 = null;
                Lane? lane2 = null;

                if (activeLanes.Count == 1)
                {
                    // Hệ thống chỉ có 1 làn duy nhất: Không bao giờ nhân đôi sang cả 2 cột!
                    var single = activeLanes[0];
                    if (single.TriggerAuxPort == 2)
                    {
                        lane2 = single;
                        lane1 = null;
                    }
                    else
                    {
                        lane1 = single;
                        lane2 = null;
                    }
                }
                else if (activeLanes.Count > 1)
                {
                    // Hệ thống có từ 2 làn trở lên:
                    lane1 = activeLanes.FirstOrDefault(l => l.TriggerAuxPort == 1);
                    lane2 = activeLanes.FirstOrDefault(l => l.TriggerAuxPort == 2 && l != lane1);

                    // Nếu các làn chưa cấu hình rõ TriggerAuxPort (hoặc cả 2 đều port 1 / port 2):
                    if (lane1 == null && lane2 == null)
                    {
                        lane1 = activeLanes[0];
                        lane2 = activeLanes[1];
                    }
                    else if (lane1 == null)
                    {
                        lane1 = activeLanes.FirstOrDefault(l => l != lane2);
                    }
                    else if (lane2 == null)
                    {
                        lane2 = activeLanes.FirstOrDefault(l => l != lane1);
                    }
                }

                if (lane1 != null)
                {
                    result.Lane1 = lane1;
                    result.Lane1PlateCamera = lane1.PlateCamera;
                    result.Lane1OverviewCamera = lane1.OverviewCamera;
                    if (result.Controller == null && lane1.Controller != null)
                    {
                        result.Controller = lane1.Controller;
                        result.ControllerIp = lane1.Controller.IpAddress;
                        result.ControllerPort = lane1.Controller.Port > 0 ? lane1.Controller.Port : 4370;
                    }
                }

                if (lane2 != null)
                {
                    result.Lane2 = lane2;
                    result.Lane2PlateCamera = lane2.PlateCamera;
                    result.Lane2OverviewCamera = lane2.OverviewCamera;
                    if (result.Controller == null && lane2.Controller != null)
                    {
                        result.Controller = lane2.Controller;
                        result.ControllerIp = lane2.Controller.IpAddress;
                        result.ControllerPort = lane2.Controller.Port > 0 ? lane2.Controller.Port : 4370;
                    }
                }

                // Fallback nếu cả 2 làn chưa gán Controller trực tiếp: tìm Controller đầu tiên trong danh mục Devices
                if (result.Controller == null)
                {
                    var fallbackController = devices.FirstOrDefault(d => d.Type == DeviceType.Controller && d.IsActive)
                                           ?? devices.FirstOrDefault(d => d.Type == DeviceType.Controller);
                    if (fallbackController != null)
                    {
                        result.Controller = fallbackController;
                        result.ControllerIp = fallbackController.IpAddress;
                        result.ControllerPort = fallbackController.Port > 0 ? fallbackController.Port : 4370;
                        AppLogger.Information($"[DeviceConfig] Sử dụng Controller mặc định từ danh mục Devices: {fallbackController.Name} ({fallbackController.IpAddress}:{result.ControllerPort})");
                    }
                }

                // Ánh xạ tương thích ngược theo Direction
                result.InLane = lane1?.Direction == LaneDirection.In ? lane1 : (lane2?.Direction == LaneDirection.In ? lane2 : null);
                result.OutLane = lane2?.Direction == LaneDirection.Out ? lane2 : (lane1?.Direction == LaneDirection.Out ? lane1 : null);
                result.InPlateCamera = result.InLane?.PlateCamera;
                result.InOverviewCamera = result.InLane?.OverviewCamera;
                result.OutPlateCamera = result.OutLane?.PlateCamera;
                result.OutOverviewCamera = result.OutLane?.OverviewCamera;

                // 4. Kiểm tra thiếu cấu hình
                CheckMissingConfigs(result, lane1, lane2);

                // 5. Tính hash để detect thay đổi
                result.Success = true;
                _lastConfigHash = ComputeConfigHash(result);

                AppLogger.Information(
                    $"[DeviceConfig] Nạp thành công - " +
                    $"Làn 1 ({result.Lane1?.Name ?? "N/A"}, Aux={result.Lane1?.TriggerAuxPort}, Dir={result.Lane1?.Direction}): " +
                    $"Plate={result.Lane1PlateCamera?.IpAddress ?? "N/A"}, Ovw={result.Lane1OverviewCamera?.IpAddress ?? "N/A"} | " +
                    $"Làn 2 ({result.Lane2?.Name ?? "N/A"}, Aux={result.Lane2?.TriggerAuxPort}, Dir={result.Lane2?.Direction}): " +
                    $"Plate={result.Lane2PlateCamera?.IpAddress ?? "N/A"}, Ovw={result.Lane2OverviewCamera?.IpAddress ?? "N/A"} | " +
                    $"Ctrl: {result.ControllerIp ?? "N/A"}:{result.ControllerPort}");

                _currentConfig = result;
                return result;

            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Warnings.Add($"Lỗi nạp cấu hình: {ex.Message}");
                AppLogger.Error(ex, $"[DeviceConfig] Lỗi nạp cấu hình: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Kiểm tra và reload nếu có thay đổi
        /// </summary>
        public async Task<(bool hasChanged, DeviceConfigResult? newConfig)> CheckAndReloadIfChangedAsync(CancellationToken cancellationToken = default)
        {
            var oldHash = _lastConfigHash;
            var oldConfig = _currentConfig;

            var newConfig = await LoadConfigAsync(cancellationToken);
            var newHash = newConfig.Success ? ComputeConfigHash(newConfig) : oldHash;

            if (newConfig.Success && newHash != oldHash)
            {
                var changedDevices = DetectChanges(oldConfig, newConfig);

                AppLogger.Warning($"[DeviceConfig] Phát hiện thay đổi cấu hình: {string.Join(", ", changedDevices)}");

                var eventArgs = new ConfigChangeEventArgs
                {
                    OldConfig = oldConfig,
                    NewConfig = newConfig,
                    ChangedDevices = changedDevices
                };

                OnConfigChanged?.Invoke(this, eventArgs);

                return (true, newConfig);
            }

            return (false, null);
        }

        /// <summary>
        /// Bắt đầu giám sát thay đổi định kỳ
        /// </summary>
        public void StartMonitoring(TimeSpan interval)
        {
            lock (_lock)
            {
                if (_isMonitoring) return;

                _monitorTimer = new Timer(
                    async _ => await CheckAndReloadIfChangedAsync(),
                    null,
                    interval,
                    interval
                );
                _isMonitoring = true;

                AppLogger.Information($"[DeviceConfig] Bắt đầu giám sát thay đổi cấu hình mỗi {interval.TotalSeconds}s");
            }
        }

        /// <summary>
        /// Dừng giám sát
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lock)
            {
                _monitorTimer?.Dispose();
                _monitorTimer = null;
                _isMonitoring = false;

                AppLogger.Information("[DeviceConfig] Dừng giám sát thay đổi cấu hình");
            }
        }

        private void LogDeviceMapping(string laneCode, LaneDirection direction, Device? plate, Device? overview, Device? controller)
        {
            var dir = direction == LaneDirection.In ? "Vào" : "Ra";
            var plateStatus = plate != null ? $"{plate.IpAddress}" : "⚠️ CHƯA GÁN";
            var overviewStatus = overview != null ? $"{overview.IpAddress}" : "⚠️ CHƯA GÁN";
            var ctrlStatus = controller != null ? $"{controller.IpAddress}:{controller.Port}" : "⚠️ CHƯA GÁN";

            AppLogger.Information($"[DeviceConfig] Làn {laneCode} ({dir}): Plate={plateStatus}, Ovw={overviewStatus}, Ctrl={ctrlStatus}");
        }

        private void CheckMissingConfigs(DeviceConfigResult result, Lane? lane1, Lane? lane2)
        {
            if (lane1 == null && lane2 == null)
            {
                result.Warnings.Add("Không tìm thấy làn xe nào đang hoạt động (IsActive=true)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Không tìm thấy làn xe nào đang hoạt động");
                return;
            }

            if (lane1 == null)
            {
                result.Warnings.Add("Chưa cấu hình Làn 1 (TriggerAuxPort = 1)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Chưa cấu hình Làn 1 (TriggerAuxPort = 1)");
            }
            else
            {
                if (lane1.PlateCamera == null)
                {
                    result.Warnings.Add($"Làn 1 ({lane1.Name}) chưa gán Camera Biển Số");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 1 ({lane1.Name}) chưa gán Camera Biển Số");
                }
                else if (!lane1.PlateCamera.IsActive)
                {
                    result.Warnings.Add($"Làn 1 ({lane1.Name}): Camera Biển Số ({lane1.PlateCamera.Name}) đang ngừng hoạt động (Inactive)");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 1 ({lane1.Name}): Camera Biển Số ({lane1.PlateCamera.Name}) đang Inactive");
                }

                if (lane1.OverviewCamera == null)
                {
                    result.Warnings.Add($"Làn 1 ({lane1.Name}) chưa gán Camera Toàn Cảnh");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 1 ({lane1.Name}) chưa gán Camera Toàn Cảnh");
                }
                else if (!lane1.OverviewCamera.IsActive)
                {
                    result.Warnings.Add($"Làn 1 ({lane1.Name}): Camera Toàn Cảnh ({lane1.OverviewCamera.Name}) đang ngừng hoạt động (Inactive)");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 1 ({lane1.Name}): Camera Toàn Cảnh ({lane1.OverviewCamera.Name}) đang Inactive");
                }
            }

            if (lane2 == null)
            {
                result.Warnings.Add("Chưa cấu hình Làn 2 (TriggerAuxPort = 2)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Chưa cấu hình Làn 2 (TriggerAuxPort = 2)");
            }
            else
            {
                if (lane2.PlateCamera == null)
                {
                    result.Warnings.Add($"Làn 2 ({lane2.Name}) chưa gán Camera Biển Số");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 2 ({lane2.Name}) chưa gán Camera Biển Số");
                }
                else if (!lane2.PlateCamera.IsActive)
                {
                    result.Warnings.Add($"Làn 2 ({lane2.Name}): Camera Biển Số ({lane2.PlateCamera.Name}) đang ngừng hoạt động (Inactive)");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 2 ({lane2.Name}): Camera Biển Số ({lane2.PlateCamera.Name}) đang Inactive");
                }

                if (lane2.OverviewCamera == null)
                {
                    result.Warnings.Add($"Làn 2 ({lane2.Name}) chưa gán Camera Toàn Cảnh");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 2 ({lane2.Name}) chưa gán Camera Toàn Cảnh");
                }
                else if (!lane2.OverviewCamera.IsActive)
                {
                    result.Warnings.Add($"Làn 2 ({lane2.Name}): Camera Toàn Cảnh ({lane2.OverviewCamera.Name}) đang ngừng hoạt động (Inactive)");
                    AppLogger.Warning($"[DeviceConfig] ⚠️ Làn 2 ({lane2.Name}): Camera Toàn Cảnh ({lane2.OverviewCamera.Name}) đang Inactive");
                }
            }

            if (result.Controller == null)
            {
                result.Warnings.Add("Không tìm thấy Controller (Barrier)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Không tìm thấy Controller");
            }
        }

        private string ComputeConfigHash(DeviceConfigResult config)
        {
            var parts = new List<string>
            {
                config.Lane1?.Id ?? "null",
                config.Lane2?.Id ?? "null",
                FormatDeviceHash(config.Lane1PlateCamera),
                FormatDeviceHash(config.Lane1OverviewCamera),
                FormatDeviceHash(config.Lane2PlateCamera),
                FormatDeviceHash(config.Lane2OverviewCamera),
                FormatDeviceHash(config.Controller),
                config.ControllerIp ?? "",
                config.ControllerPort.ToString()
            };

            return string.Join("|", parts).GetHashCode().ToString("X8");
        }

        private static string FormatDeviceHash(Device? dev) =>
            dev == null ? "null" : $"{dev.Id}:{dev.Code}:{dev.Name}:{dev.IpAddress}:{dev.Port}:{dev.UserName}:{dev.Password}:{dev.IsActive}";

        private List<string> DetectChanges(DeviceConfigResult oldConfig, DeviceConfigResult newConfig)
        {
            var changes = new List<string>();

            CheckDeviceDiff(changes, "Camera Biển Số Làn 1", oldConfig.Lane1PlateCamera, newConfig.Lane1PlateCamera);
            CheckDeviceDiff(changes, "Camera Toàn Cảnh Làn 1", oldConfig.Lane1OverviewCamera, newConfig.Lane1OverviewCamera);
            CheckDeviceDiff(changes, "Camera Biển Số Làn 2", oldConfig.Lane2PlateCamera, newConfig.Lane2PlateCamera);
            CheckDeviceDiff(changes, "Camera Toàn Cảnh Làn 2", oldConfig.Lane2OverviewCamera, newConfig.Lane2OverviewCamera);
            CheckDeviceDiff(changes, "Controller", oldConfig.Controller, newConfig.Controller);

            if (oldConfig.ControllerIp != newConfig.ControllerIp || oldConfig.ControllerPort != newConfig.ControllerPort)
                changes.Add($"Controller Config: {oldConfig.ControllerIp}:{oldConfig.ControllerPort} → {newConfig.ControllerIp}:{newConfig.ControllerPort}");

            return changes;
        }

        private static void CheckDeviceDiff(List<string> changes, string label, Device? oldDev, Device? newDev)
        {
            if (oldDev == null && newDev != null)
            {
                changes.Add($"{label}: Đã kích hoạt ({newDev.IpAddress}:{newDev.Port})");
                return;
            }
            if (oldDev != null && newDev == null)
            {
                changes.Add($"{label}: Đã vô hiệu hóa");
                return;
            }
            if (oldDev != null && newDev != null)
            {
                if (oldDev.Id != newDev.Id || oldDev.IpAddress != newDev.IpAddress || oldDev.Port != newDev.Port)
                    changes.Add($"{label} Địa chỉ: {oldDev.IpAddress}:{oldDev.Port} → {newDev.IpAddress}:{newDev.Port}");
                if (oldDev.UserName != newDev.UserName || oldDev.Password != newDev.Password)
                    changes.Add($"{label} Tài khoản đăng nhập thay đổi");
                if (oldDev.Name != newDev.Name)
                    changes.Add($"{label} Tên: {oldDev.Name} → {newDev.Name}");
                if (oldDev.IsActive != newDev.IsActive)
                    changes.Add($"{label} IsActive: {oldDev.IsActive} → {newDev.IsActive}");
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
