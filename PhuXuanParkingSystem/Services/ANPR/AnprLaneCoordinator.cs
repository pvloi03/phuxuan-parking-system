using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PhuXuanParkingSystem.Models.ValueObjects;
using PhuXuanParkingSystem.Services.Logging;

namespace PhuXuanParkingSystem.Services.ANPR
{
    /// <summary>
    /// Bộ điều phối nhận diện biển số đa làn (Multi-Lane ANPR Coordinator):
    /// - Quản lý 2 Instance ANPR độc lập cho Làn Vào và Làn Ra (0 lock chéo).
    /// - Áp dụng Cooldown 2 lớp:
    ///   + Lớp 1: Same-lane Cooldown (~1.8s) chống rung radar / spam cảm biến.
    ///   + Lớp 2: Duplicate-plate Cooldown (~3.0s) chống nhận diện trùng biển số.
    /// </summary>
    public class AnprLaneCoordinator : IDisposable
    {
        private static readonly Lazy<AnprLaneCoordinator> _lazy =
            new Lazy<AnprLaneCoordinator>(() => new AnprLaneCoordinator());

        public static AnprLaneCoordinator Instance => _lazy.Value;

        private readonly IAnprService _inLaneAnpr;
        private readonly IAnprService _outLaneAnpr;

        private readonly ConcurrentDictionary<string, DateTime> _lastLaneTriggerTimes =
            new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, DateTime> _lastPlateProcessedTimes =
            new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public double SameLaneCooldownSeconds { get; set; } = 1.8;
        public double DuplicatePlateCooldownSeconds { get; set; } = 3.0;

        public IAnprService InLaneAnpr => _inLaneAnpr;
        public IAnprService OutLaneAnpr => _outLaneAnpr;

        public AnprLaneCoordinator(IAnprService? inLaneAnpr = null, IAnprService? outLaneAnpr = null)
        {
            _inLaneAnpr = inLaneAnpr ?? CreateAnprServiceSafe("LANE_IN");
            _outLaneAnpr = outLaneAnpr ?? CreateAnprServiceSafe("LANE_OUT");
        }

        private static IAnprService CreateAnprServiceSafe(string laneId)
        {
            try
            {
                return new RapidOcrAnprService(laneId);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"[ANPR {laneId}] Khởi tạo fallback ANPR service: {ex.Message}", "ANPR");
                return new NullAnprService(laneId);
            }
        }

        public bool ShouldProcessLaneTrigger(string laneId)
        {
            var now = DateTime.Now;
            if (_lastLaneTriggerTimes.TryGetValue(laneId, out var lastTime))
            {
                var elapsed = (now - lastTime).TotalSeconds;
                if (elapsed >= 0 && elapsed < SameLaneCooldownSeconds)
                {
                    AppLogger.Debug($"[ANPR] Bỏ qua kích hoạt lặp trên làn {laneId} (Cooldown Lớp 1: {elapsed:F1}s < {SameLaneCooldownSeconds}s)", "ANPR");
                    return false;
                }
            }

            _lastLaneTriggerTimes[laneId] = now;
            return true;
        }

        public bool IsDuplicatePlate(string licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate)) return false;

            var now = DateTime.Now;
            if (_lastPlateProcessedTimes.TryGetValue(licensePlate, out var lastTime))
            {
                var elapsed = (now - lastTime).TotalSeconds;
                if (elapsed >= 0 && elapsed < DuplicatePlateCooldownSeconds)
                {
                    return true;
                }
            }

            _lastPlateProcessedTimes[licensePlate] = now;
            return false;
        }

        public async Task<AnprResult> ProcessLaneInFileAsync(string imageFilePath, CancellationToken ct = default)
        {
            return await _inLaneAnpr.RecognizeFileAsync(imageFilePath, ct);
        }

        public async Task<AnprResult> ProcessLaneOutFileAsync(string imageFilePath, CancellationToken ct = default)
        {
            return await _outLaneAnpr.RecognizeFileAsync(imageFilePath, ct);
        }

        public async Task<AnprResult> ProcessLaneInBytesAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            return await _inLaneAnpr.RecognizeAsync(imageBytes, ct);
        }

        public async Task<AnprResult> ProcessLaneOutBytesAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            return await _outLaneAnpr.RecognizeAsync(imageBytes, ct);
        }

        public void Dispose()
        {
            _inLaneAnpr.Dispose();
            _outLaneAnpr.Dispose();
        }
    }

    /// <summary>
    /// Fallback Dummy Service khi môi trường không có Native OCR Library
    /// </summary>
    public class NullAnprService : IAnprService
    {
        public string LaneId { get; }
        public bool IsReady => false;

        public NullAnprService(string laneId)
        {
            LaneId = laneId;
        }

        public Task<AnprResult> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            return Task.FromResult(AnprResult.Failed("Native OCR không sẵn sàng trên kiến trúc này."));
        }

        public Task<AnprResult> RecognizeFileAsync(string imageFilePath, CancellationToken ct = default)
        {
            return Task.FromResult(AnprResult.Failed("Native OCR không sẵn sàng trên kiến trúc này."));
        }

        public void Dispose() { }
    }
}