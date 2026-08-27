import React, { useState, useEffect } from 'react';
import { licenseService, type LicenseStatus } from '../services/licenseService';
import { notify } from '../lib/notify';
import {
  ShieldCheck,
  ShieldX,
  KeyRound,
  Upload,
  RefreshCw,
  Building2,
  Calendar,
  AlertTriangle,
  Clock,
  CheckCircle2,
} from 'lucide-react';

export const LicensePage: React.FC = () => {
  const [status, setStatus] = useState<LicenseStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [activating, setActivating] = useState(false);
  const [licenseKeyInput, setLicenseKeyInput] = useState('');

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
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-100 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="p-2.5 bg-indigo-50 rounded-xl">
            <ShieldCheck className="w-6 h-6 text-indigo-600" />
          </div>
          <div>
            <h1 className="text-lg font-bold text-slate-800">Quản Lý Bản Quyền Phần Mềm</h1>
            <p className="text-xs text-slate-500 mt-0.5">Kiểm soát thời hạn sử dụng và nạp key RSA 3072-bit</p>
          </div>
        </div>
        <button
          onClick={fetchStatus}
          disabled={loading}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-xl transition cursor-pointer shrink-0"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          Làm Mới
        </button>
      </div>

      {/* Alert Banners */}
      {isDanger && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-2xl flex items-start gap-3 text-red-800">
          <ShieldX className="w-5 h-5 text-red-600 shrink-0 mt-0.5" />
          <div className="text-sm">
            <p className="font-bold">Bản quyền chưa kích hoạt hoặc đã hết hạn!</p>
            <p className="mt-0.5 text-red-700/80">Vui lòng nạp License Key mới ở phần bên phải.</p>
          </div>
        </div>
      )}
      {isWarning && (
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-2xl flex items-start gap-3 text-amber-800">
          <AlertTriangle className="w-5 h-5 text-amber-500 shrink-0 mt-0.5" />
          <div className="text-sm">
            <p className="font-bold">Bản quyền sắp hết hạn!</p>
            <p className="mt-0.5 text-amber-700/80">Còn lại <b>{status?.daysRemaining} ngày</b> — hết hạn vào {status?.expiryDate ? new Date(status.expiryDate).toLocaleDateString('vi-VN') : '--'}.</p>
          </div>
        </div>
      )}

      {/* Main Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">

        {/* Card trái: Trạng thái bản quyền */}
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm p-6 flex flex-col gap-5">
          <div className="flex items-center justify-between pb-4 border-b border-slate-100">
            <h2 className="text-base font-bold text-slate-700 flex items-center gap-2">
              <KeyRound className="w-4 h-4 text-indigo-500" />
              Giấy Phép Sử Dụng
            </h2>
            {status?.isValid ? (
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-700">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                {status.isPermanent ? 'VĨNH VIỄN' : `HỢP LỆ · ${status.daysRemaining} NGÀY`}
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-red-100 text-red-600">
                <span className="w-1.5 h-1.5 rounded-full bg-red-500" />
                CHƯA KÍCH HOẠT
              </span>
            )}
          </div>

          <div className="space-y-3">
            <div className="flex items-center gap-3 p-3.5 rounded-xl bg-slate-50 border border-slate-100">
              <div className="p-2 bg-indigo-50 rounded-lg shrink-0">
                <Building2 className="w-4 h-4 text-indigo-500" />
              </div>
              <div className="min-w-0">
                <p className="text-[11px] font-medium text-slate-400 uppercase tracking-wide">Đơn Vị Được Cấp Quyền</p>
                <p className="text-sm font-bold text-slate-800 truncate">{status?.customerName || '—'}</p>
              </div>
            </div>

            <div className="flex items-center gap-3 p-3.5 rounded-xl bg-slate-50 border border-slate-100">
              <div className="p-2 bg-indigo-50 rounded-lg shrink-0">
                <Calendar className="w-4 h-4 text-indigo-500" />
              </div>
              <div className="min-w-0">
                <p className="text-[11px] font-medium text-slate-400 uppercase tracking-wide">Thời Điểm Hết Hạn</p>
                <p className="text-sm font-bold text-slate-800">
                  {status?.isPermanent
                    ? 'Vĩnh Viễn (Không giới hạn)'
                    : status?.expiryDate
                    ? new Date(status.expiryDate).toLocaleString('vi-VN')
                    : '—'}
                </p>
              </div>
            </div>

            {status?.isValid && !status?.isPermanent && (
              <div className="flex items-start gap-3 p-3.5 rounded-xl bg-slate-50 border border-slate-100">
                <div className="p-2 bg-indigo-50 rounded-lg shrink-0">
                  <Clock className="w-4 h-4 text-indigo-500" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-[11px] font-medium text-slate-400 uppercase tracking-wide mb-2">Thời Hạn Còn Lại</p>
                  <div className="w-full h-1.5 bg-slate-200 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all ${
                        (status?.daysRemaining ?? 0) <= 15 ? 'bg-amber-500' : 'bg-emerald-500'
                      }`}
                      style={{ width: `${Math.min(100, ((status?.daysRemaining ?? 0) / 365) * 100)}%` }}
                    />
                  </div>
                  <p className="text-xs font-semibold text-slate-600 mt-1.5">{status?.daysRemaining} ngày còn lại</p>
                </div>
              </div>
            )}
          </div>

          {status?.isValid && (
            <div className="mt-auto flex items-center gap-2 text-xs text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-xl px-3 py-2.5">
              <CheckCircle2 className="w-4 h-4 shrink-0" />
              <span>Chữ ký số RSA 3072-bit hợp lệ · Máy tính đã được xác thực</span>
            </div>
          )}
        </div>

        {/* Card phải: Kích hoạt */}
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm p-6 flex flex-col gap-5">
          <div className="pb-4 border-b border-slate-100">
            <h2 className="text-base font-bold text-slate-700 flex items-center gap-2">
              <Upload className="w-4 h-4 text-indigo-500" />
              Nạp License Key Mới
            </h2>
            <p className="text-xs text-slate-400 mt-1">Nhập chuỗi ký tự hoặc tải lên file .lic do nhà cung cấp cấp</p>
          </div>

          <form onSubmit={handleActivate} className="space-y-3">
            <label className="block text-xs font-semibold text-slate-600">Dán Chuỗi License Key:</label>
            <textarea
              rows={5}
              value={licenseKeyInput}
              onChange={(e) => setLicenseKeyInput(e.target.value)}
              placeholder="Dán chuỗi PX-LIC-... vào đây..."
              className="w-full px-3.5 py-2.5 font-mono text-xs text-slate-800 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-400 resize-none transition"
            />
            <button
              type="submit"
              disabled={activating || !licenseKeyInput.trim()}
              className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold text-sm rounded-xl shadow-sm transition flex items-center justify-center gap-2 cursor-pointer"
            >
              {activating ? <RefreshCw className="w-4 h-4 animate-spin" /> : <ShieldCheck className="w-4 h-4" />}
              {activating ? 'Đang xác thực...' : 'Kích Hoạt Bản Quyền'}
            </button>
          </form>

          <div className="relative flex items-center">
            <div className="flex-grow border-t border-slate-200" />
            <span className="flex-shrink mx-3 text-[11px] text-slate-400 uppercase font-semibold tracking-wider">Hoặc</span>
            <div className="flex-grow border-t border-slate-200" />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-2">Nạp Từ File Bản Quyền (.lic):</label>
            <label className="flex flex-col items-center justify-center w-full h-20 border-2 border-dashed border-slate-200 hover:border-indigo-400 rounded-xl cursor-pointer bg-slate-50 hover:bg-indigo-50/40 transition group">
              <Upload className="w-5 h-5 text-slate-400 group-hover:text-indigo-500 transition mb-1" />
              <span className="text-xs font-medium text-slate-500 group-hover:text-indigo-600 transition">Bấm để chọn file .lic</span>
              <input type="file" accept=".lic,.txt" onChange={handleFileUpload} disabled={activating} className="hidden" />
            </label>
          </div>

          <div className="mt-auto p-3 bg-slate-50 rounded-xl border border-slate-200 text-xs text-slate-500 space-y-0.5">
            <p className="font-semibold text-slate-600">🔒 Bảo Mật Chữ Ký Số RSA 3072-bit</p>
            <p>Phần mềm tự động xác thực tính toàn vẹn và mã máy. Mọi thay đổi trái phép sẽ bị từ chối.</p>
          </div>
        </div>

      </div>
    </div>
  );
};
