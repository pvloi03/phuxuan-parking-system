using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceConfig
{
    /// <summary>
    /// Triển khai IDeviceConfigService - Quản lý cấu hình thiết bị với Cache + Reload động
    /// </summary>
    public class DeviceConfigService : IDeviceConfigService, IDisposable
    {
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;

        private DeviceConfigResult _currentConfig = new();
        private string _lastConfigHash = string.Empty;
        private Timer? _monitorTimer;
        private bool _isMonitoring;
        private readonly object _lock = new();

        public event EventHandler<ConfigChangeEventArgs>? OnConfigChanged;

        public DeviceConfigResult CurrentConfig => _currentConfig;

        public DeviceConfigService(IRepository<Lane> laneRepo, IRepository<Device> deviceRepo)
        {
            _laneRepo = laneRepo ?? throw new ArgumentNullException(nameof(laneRepo));
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
        }

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
                // 1. Query Lanes và Devices song song để tăng tốc
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

                // 3. Áp dụng vào result theo Direction
                var inLane = lanes?.FirstOrDefault(l => l.Direction == LaneDirection.In);
                if (inLane != null)
                {
                    result.InPlateCamera = inLane.PlateCamera;
                    result.InOverviewCamera = inLane.OverviewCamera;
                    result.Controller = inLane.Controller;
                    result.ControllerIp = inLane.Controller?.IpAddress;
                    result.ControllerPort = inLane.Controller?.Port ?? 4370;
                }

                var outLane = lanes?.FirstOrDefault(l => l.Direction == LaneDirection.Out);
                if (outLane != null)
                {
                    result.OutPlateCamera = outLane.PlateCamera;
                    result.OutOverviewCamera = outLane.OverviewCamera;

                    // Controller: ưu tiên InLane, fallback OutLane
                    if (result.Controller == null && outLane.Controller != null)
                    {
                        result.Controller = outLane.Controller;
                        result.ControllerIp = outLane.Controller.IpAddress;
                        result.ControllerPort = outLane.Controller.Port;
                    }
                }

                // 4. Kiểm tra thiếu cấu hình
                CheckMissingConfigs(result, inLane, outLane);

                // 5. Tính hash để detect thay đổi
                result.Success = true;
                _lastConfigHash = ComputeConfigHash(result);

                AppLogger.Information(
                    $"[DeviceConfig] Nạp thành công - InPlate: {result.InPlateCamera?.IpAddress ?? "N/A"}, " +
                    $"InOvw: {result.InOverviewCamera?.IpAddress ?? "N/A"}, " +
                    $"OutPlate: {result.OutPlateCamera?.IpAddress ?? "N/A"}, " +
                    $"OutOvw: {result.OutOverviewCamera?.IpAddress ?? "N/A"}, " +
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
            var newConfig = await LoadConfigAsync(cancellationToken);
            var newHash = ComputeConfigHash(newConfig);

            if (newHash != _lastConfigHash)
            {
                var oldConfig = _currentConfig;
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

        /// <summary>
        /// Cập nhật trạng thái Active của lane
        /// </summary>
        public void UpdateLaneActiveState(LaneDirection direction, bool isActive)
        {
            AppLogger.Information($"[DeviceConfig] Lane {(direction == LaneDirection.In ? "Vào" : "Ra")} {(isActive ? "bật" : "tắt")} hoạt động");
        }

        private void LogDeviceMapping(string laneCode, LaneDirection direction, Device? plate, Device? overview, Device? controller)
        {
            var dir = direction == LaneDirection.In ? "Vào" : "Ra";
            var plateStatus = plate != null ? $"{plate.IpAddress}" : "⚠️ CHƯA GÁN";
            var overviewStatus = overview != null ? $"{overview.IpAddress}" : "⚠️ CHƯA GÁN";
            var ctrlStatus = controller != null ? $"{controller.IpAddress}:{controller.Port}" : "⚠️ CHƯA GÁN";

            AppLogger.Information($"[DeviceConfig] Làn {laneCode} ({dir}): Plate={plateStatus}, Ovw={overviewStatus}, Ctrl={ctrlStatus}");
        }

        private void CheckMissingConfigs(DeviceConfigResult result, Lane? inLane, Lane? outLane)
        {
            if (inLane == null)
            {
                result.Warnings.Add("Không tìm thấy Làn Vào (Direction=In, IsActive=true)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Không tìm thấy Làn Vào");
            }

            if (outLane == null)
            {
                result.Warnings.Add("Không tìm thấy Làn Ra (Direction=Out, IsActive=true)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Không tìm thấy Làn Ra");
            }

            if (inLane?.PlateCamera == null)
            {
                result.Warnings.Add("Làn Vào chưa gán Camera Biển Số");
                AppLogger.Warning("[DeviceConfig] ⚠️ Làn Vào chưa gán Camera Biển Số");
            }

            if (inLane?.OverviewCamera == null)
            {
                result.Warnings.Add("Làn Vào chưa gán Camera Toàn Cảnh");
                AppLogger.Warning("[DeviceConfig] ⚠️ Làn Vào chưa gán Camera Toàn Cảnh");
            }

            if (result.Controller == null)
            {
                result.Warnings.Add("Không tìm thấy Controller (Barrier)");
                AppLogger.Warning("[DeviceConfig] ⚠️ Không tìm thấy Controller");
            }
        }

        private string ComputeConfigHash(DeviceConfigResult config)
        {
            // Hash dựa trên Device IDs và IPs để detect thay đổi nhanh
            var parts = new List<string>
            {
                config.InPlateCamera?.Id ?? "",
                config.InOverviewCamera?.Id ?? "",
                config.OutPlateCamera?.Id ?? "",
                config.OutOverviewCamera?.Id ?? "",
                config.Controller?.Id ?? "",
                config.ControllerIp ?? "",
                config.ControllerPort.ToString()
            };

            var combined = string.Join("|", parts);
            return combined.GetHashCode().ToString("X8");
        }

        private List<string> DetectChanges(DeviceConfigResult oldConfig, DeviceConfigResult newConfig)
        {
            var changes = new List<string>();

            if (oldConfig.InPlateCamera?.IpAddress != newConfig.InPlateCamera?.IpAddress)
                changes.Add($"InPlateCamera: {oldConfig.InPlateCamera?.IpAddress} → {newConfig.InPlateCamera?.IpAddress}");

            if (oldConfig.InOverviewCamera?.IpAddress != newConfig.InOverviewCamera?.IpAddress)
                changes.Add($"InOverviewCamera: {oldConfig.InOverviewCamera?.IpAddress} → {newConfig.InOverviewCamera?.IpAddress}");

            if (oldConfig.OutPlateCamera?.IpAddress != newConfig.OutPlateCamera?.IpAddress)
                changes.Add($"OutPlateCamera: {oldConfig.OutPlateCamera?.IpAddress} → {newConfig.OutPlateCamera?.IpAddress}");

            if (oldConfig.OutOverviewCamera?.IpAddress != newConfig.OutOverviewCamera?.IpAddress)
                changes.Add($"OutOverviewCamera: {oldConfig.OutOverviewCamera?.IpAddress} → {newConfig.OutOverviewCamera?.IpAddress}");

            if (oldConfig.ControllerIp != newConfig.ControllerIp || oldConfig.ControllerPort != newConfig.ControllerPort)
                changes.Add($"Controller: {oldConfig.ControllerIp}:{oldConfig.ControllerPort} → {newConfig.ControllerIp}:{newConfig.ControllerPort}");

            return changes;
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
