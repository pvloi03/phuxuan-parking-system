import React, { useState, useEffect } from 'react';
import { licenseService, type LicenseStatus } from '../services/licenseService';
import { notify } from '../lib/notify';
import {
  ShieldCheck,
  ShieldX,
  Copy,
  Check,
  KeyRound,
  Upload,
  Cpu,
  Video,
  Layers,
  Sparkles,
  RefreshCw,
  Building2,
  Calendar,
  AlertTriangle
} from 'lucide-react';

export const LicensePage: React.FC = () => {
  const [status, setStatus] = useState<LicenseStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [activating, setActivating] = useState(false);
  const [licenseKeyInput, setLicenseKeyInput] = useState('');
  const [copied, setCopied] = useState(false);

  const fetchStatus = async () => {
    try {
      setLoading(true);
      const data = await licenseService.getStatus();
      setStatus(data);
    } catch (err: any) {
      notify.error(err.response?.data?.message || 'Không thể tải thông tin bản quyền.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStatus();
  }, []);

  const handleCopyMachineCode = () => {
    if (status?.machineCode) {
      navigator.clipboard.writeText(status.machineCode);
      setCopied(true);
      notify.success('Đã sao chép Mã Máy Tính (Machine Code) vào clipboard!');
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleActivate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!licenseKeyInput.trim()) {
      notify.warning('Vui lòng nhập chuỗi License Key hoặc tải lên file .lic');
      return;
    }

    try {
      setActivating(true);
      const res = await licenseService.activate(licenseKeyInput.trim());
      notify.success(res.message || 'Kích hoạt bản quyền thành công!');
      setLicenseKeyInput('');
      fetchStatus();
    } catch (err: any) {
      notify.error(err.response?.data?.message || 'Kích hoạt thất bại. Vui lòng kiểm tra lại License Key.');
    } finally {
      setActivating(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      setActivating(true);
      const res = await licenseService.uploadFile(file);
      notify.success(res.message || 'Kích hoạt bản quyền từ file thành công!');
      fetchStatus();
    } catch (err: any) {
      notify.error(err.response?.data?.message || 'Lỗi đọc hoặc kích hoạt file bản quyền.');
    } finally {
      setActivating(false);
      e.target.value = '';
    }
  };

  if (loading && !status) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <RefreshCw className="w-8 h-8 animate-spin text-indigo-600" />
      </div>
    );
  }

  const isWarning = status?.isValid && !status?.isPermanent && status.daysRemaining <= 15;
  const isDanger = !status?.isValid || status?.isExpired;

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Header Page */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-100 shadow-sm">
        <div>
          <h1 className="text-2xl font-bold text-slate-800 flex items-center gap-2.5">
            <ShieldCheck className="w-7 h-7 text-indigo-600" />
            Quản Lý Bản Quyền Phần Mềm (License)
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Kiểm soát thời hạn sử dụng, hạn mức làn xe / camera và nạp key bản quyền RSA 3072-bit
          </p>
        </div>
        <button
          onClick={fetchStatus}
          disabled={loading}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-xl transition cursor-pointer"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          Làm Mới Trạng Thái
        </button>
      </div>

      {/* Alert Banner khi sắp hết hạn hoặc hết hạn */}
      {isDanger && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-2xl flex items-start gap-3.5 text-red-800">
          <ShieldX className="w-6 h-6 text-red-600 shrink-0 mt-0.5" />
          <div className="text-sm">
            <h3 className="font-bold text-red-900">Bản quyền phần mềm đã hết hạn hoặc chưa được kích hoạt!</h3>
            <p className="mt-1">
              Vui lòng sao chép <b>Mã Máy Tính (Machine Code)</b> bên dưới và gửi cho nhà cung cấp để được cấp License Key mới.
            </p>
          </div>
        </div>
      )}

      {isWarning && (
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-2xl flex items-start gap-3.5 text-amber-800">
          <AlertTriangle className="w-6 h-6 text-amber-600 shrink-0 mt-0.5" />
          <div className="text-sm">
            <h3 className="font-bold text-amber-900">Cảnh báo: Bản quyền sắp hết hạn sử dụng!</h3>
            <p className="mt-1">
              Hệ thống chỉ còn lại <b>{status?.daysRemaining} ngày</b> sử dụng (hết hạn vào {status?.expiryDate ? new Date(status.expiryDate).toLocaleDateString('vi-VN') : '--'}). Vui lòng liên hệ gia hạn sớm để tránh gián đoạn dịch vụ.
            </p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Card 1: Thông tin bản quyền hiện tại */}
        <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-100 shadow-sm p-6 space-y-6">
          <div className="flex items-center justify-between border-b border-slate-100 pb-4">
            <h2 className="text-lg font-bold text-slate-800 flex items-center gap-2">
              <KeyRound className="w-5 h-5 text-indigo-600" />
              Thông Tin Giấy Phép Sử Dụng
            </h2>
            {status?.isValid ? (
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-700">
                <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                {status.isPermanent ? 'BẢN QUYỀN VĨNH VIỄN' : `HỢP LỆ (CÒN ${status.daysRemaining} NGÀY)`}
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-red-100 text-red-700">
                <span className="w-2 h-2 rounded-full bg-red-500" />
                HẾT HẠN / CHƯA KÍCH HOẠT
              </span>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-4 bg-slate-50 rounded-xl border border-slate-100">
              <div className="text-xs font-medium text-slate-400 flex items-center gap-1.5 mb-1">
                <Building2 className="w-4 h-4 text-slate-500" />
                Đơn Vị Được Cấp Quyền
              </div>
              <div className="text-base font-bold text-slate-800">
                {status?.customerName || 'Chưa kích hoạt'}
              </div>
            </div>

            <div className="p-4 bg-slate-50 rounded-xl border border-slate-100">
              <div className="text-xs font-medium text-slate-400 flex items-center gap-1.5 mb-1">
                <Calendar className="w-4 h-4 text-slate-500" />
                Thời Điểm Hết Hạn
              </div>
              <div className="text-base font-bold text-slate-800">
                {status?.isPermanent
                  ? 'Vĩnh Viễn (Không giới hạn)'
                  : status?.expiryDate
                  ? new Date(status.expiryDate).toLocaleString('vi-VN')
                  : '--'}
              </div>
            </div>
          </div>

          {/* Machine Code Box */}
          <div className="p-4 bg-indigo-50/60 border border-indigo-100 rounded-xl">
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold text-indigo-900 uppercase tracking-wider flex items-center gap-1.5">
                <Cpu className="w-4 h-4 text-indigo-600" />
                Mã Định Danh Máy Tính (Machine Code - Hardware ID)
              </span>
              <button
                type="button"
                onClick={handleCopyMachineCode}
                className="inline-flex items-center gap-1 text-xs font-bold text-indigo-600 hover:text-indigo-700 cursor-pointer"
              >
                {copied ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
                {copied ? 'Đã chép' : 'Sao chép'}
              </button>
            </div>
            <div className="font-mono text-base font-bold text-indigo-950 bg-white px-3.5 py-2.5 rounded-lg border border-indigo-200 select-all flex items-center justify-between">
              <span>{status?.machineCode || 'Đang tải...'}</span>
            </div>
            <p className="text-xs text-indigo-600/80 mt-2">
              * Gửi mã máy này cho nhà cung cấp khi mua bản quyền hoặc yêu cầu gia hạn hệ thống.
            </p>
          </div>

          {/* Quota Limits Progress Bars */}
          <div className="space-y-4 pt-2">
            <h3 className="text-sm font-bold text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
              <Layers className="w-4 h-4 text-slate-500" />
              Hạn Mức Sử Dụng Hệ Thống (Quota Limits)
            </h3>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* Max Lanes */}
              <div className="p-4 bg-slate-50 rounded-xl border border-slate-100 space-y-2">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-slate-500">Số Làn Xe:</span>
                  <span className={`font-bold ${status?.quota.isLanesLimitReached ? 'text-amber-600' : 'text-slate-800'}`}>
                    {status?.quota.currentLanes} / {status?.quota.maxLanes} làn
                  </span>
                </div>
                <div className="w-full h-2 bg-slate-200 rounded-full overflow-hidden">
                  <div
                    className={`h-full transition-all ${
                      status?.quota.isLanesLimitReached ? 'bg-amber-500' : 'bg-indigo-600'
                    }`}
                    style={{
                      width: `${Math.min(
                        100,
                        ((status?.quota.currentLanes || 0) / (status?.quota.maxLanes || 1)) * 100
                      )}%`,
                    }}
                  />
                </div>
                {status?.quota.isLanesLimitReached && (
                  <p className="text-[11px] text-amber-600 font-medium">Đã đạt tối đa số làn</p>
                )}
              </div>

              {/* Max Cameras */}
              <div className="p-4 bg-slate-50 rounded-xl border border-slate-100 space-y-2">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-slate-500 flex items-center gap-1">
                    <Video className="w-3.5 h-3.5" />
                    Số Camera:
                  </span>
                  <span className={`font-bold ${status?.quota.isCamerasLimitReached ? 'text-amber-600' : 'text-slate-800'}`}>
                    {status?.quota.currentCameras} / {status?.quota.maxCameras} cam
                  </span>
                </div>
                <div className="w-full h-2 bg-slate-200 rounded-full overflow-hidden">
                  <div
                    className={`h-full transition-all ${
                      status?.quota.isCamerasLimitReached ? 'bg-amber-500' : 'bg-indigo-600'
                    }`}
                    style={{
                      width: `${Math.min(
                        100,
                        ((status?.quota.currentCameras || 0) / (status?.quota.maxCameras || 1)) * 100
                      )}%`,
                    }}
                  />
                </div>
                {status?.quota.isCamerasLimitReached && (
                  <p className="text-[11px] text-amber-600 font-medium">Đã đạt tối đa Camera</p>
                )}
              </div>

              {/* Max Controllers */}
              <div className="p-4 bg-slate-50 rounded-xl border border-slate-100 space-y-2">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-slate-500 flex items-center gap-1">
                    <Cpu className="w-3.5 h-3.5" />
                    Bộ Điều Khiển:
                  </span>
                  <span className={`font-bold ${status?.quota.isControllersLimitReached ? 'text-amber-600' : 'text-slate-800'}`}>
                    {status?.quota.currentControllers} / {status?.quota.maxControllers} bộ
                  </span>
                </div>
                <div className="w-full h-2 bg-slate-200 rounded-full overflow-hidden">
                  <div
                    className={`h-full transition-all ${
                      status?.quota.isControllersLimitReached ? 'bg-amber-500' : 'bg-indigo-600'
                    }`}
                    style={{
                      width: `${Math.min(
                        100,
                        ((status?.quota.currentControllers || 0) / (status?.quota.maxControllers || 1)) * 100
                      )}%`,
                    }}
                  />
                </div>
                {status?.quota.isControllersLimitReached && (
                  <p className="text-[11px] text-amber-600 font-medium">Đã đạt tối đa Controller</p>
                )}
              </div>
            </div>
          </div>

          {/* Features Tag */}
          <div className="pt-2">
            <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-2 flex items-center gap-1">
              <Sparkles className="w-3.5 h-3.5 text-indigo-500" />
              Tính Năng Đã Kích Hoạt
            </h3>
            <div className="flex flex-wrap gap-2">
              {status?.features && status.features.length > 0 ? (
                status.features.map((f, i) => (
                  <span
                    key={i}
                    className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200"
                  >
                    ✓ {f === 'ANPR_Vietnam' ? 'Nhận diện biển số AI (ANPR VN)' : f === 'AutoBarrier' ? 'Điều khiển Barie tự động' : f === 'DualCameraPerLane' ? '2 Camera / Làn' : f}
                  </span>
                ))
              ) : (
                <span className="text-xs text-slate-400 italic">Gói tiêu chuẩn cơ bản</span>
              )}
            </div>
          </div>
        </div>

        {/* Card 2: Nạp & Kích hoạt bản quyền mới */}
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm p-6 space-y-6 flex flex-col justify-between">
          <div className="space-y-4">
            <div className="border-b border-slate-100 pb-4">
              <h2 className="text-lg font-bold text-slate-800 flex items-center gap-2">
                <Upload className="w-5 h-5 text-indigo-600" />
                Nạp License Key Mới
              </h2>
              <p className="text-xs text-slate-500 mt-1">
                Nhập chuỗi ký tự hoặc tải lên file bản quyền (.lic) do nhà cung cấp cấp
              </p>
            </div>

            {/* Form nhập text */}
            <form onSubmit={handleActivate} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1.5">
                  Dán Chuỗi License Key:
                </label>
                <textarea
                  rows={4}
                  value={licenseKeyInput}
                  onChange={(e) => setLicenseKeyInput(e.target.value)}
                  placeholder="Dán chuỗi PX-LIC-... vào đây..."
                  className="w-full px-3.5 py-2.5 font-mono text-xs text-slate-800 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none transition"
                />
              </div>

              <button
                type="submit"
                disabled={activating || !licenseKeyInput.trim()}
                className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-bold text-sm rounded-xl shadow-sm transition flex items-center justify-center gap-2 cursor-pointer"
              >
                {activating ? (
                  <RefreshCw className="w-4 h-4 animate-spin" />
                ) : (
                  <ShieldCheck className="w-4 h-4" />
                )}
                {activating ? 'Đang xác thực...' : 'Kích Hoạt Bản Quyền'}
              </button>
            </form>

            <div className="relative flex py-1 items-center">
              <div className="flex-grow border-t border-slate-200"></div>
              <span className="flex-shrink mx-3 text-xs text-slate-400 uppercase font-semibold">Hoặc</span>
              <div className="flex-grow border-t border-slate-200"></div>
            </div>

            {/* Nút Upload File .lic */}
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1.5">
                Nạp Từ File Bản Quyền (.lic):
              </label>
              <label className="flex flex-col items-center justify-center w-full h-24 border-2 border-dashed border-slate-200 hover:border-indigo-400 rounded-xl cursor-pointer bg-slate-50 hover:bg-indigo-50/40 transition p-2">
                <Upload className="w-6 h-6 text-slate-400 mb-1" />
                <span className="text-xs font-medium text-slate-600">Bấm để chọn file .lic</span>
                <input
                  type="file"
                  accept=".lic,.txt"
                  onChange={handleFileUpload}
                  disabled={activating}
                  className="hidden"
                />
              </label>
            </div>
          </div>

          <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 text-xs text-slate-500 space-y-1">
            <p className="font-semibold text-slate-700">🔒 Bảo Mật Chữ Ký Số RSA 3072-bit:</p>
            <p>Phần mềm tự động kiểm tra tính toàn vẹn và mã máy cục bộ. Mọi thay đổi trái phép sẽ bị từ chối.</p>
          </div>
        </div>
      </div>
    </div>
  );
};
