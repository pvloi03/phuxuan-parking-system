import { Link, useNavigate } from 'react-router-dom'
import { Home, ArrowLeft, ShieldX } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  const navigate = useNavigate()

  return (
    <div className="min-h-screen w-full flex items-center justify-center p-4 bg-gradient-to-br from-slate-900 via-slate-950 to-blue-950 text-slate-100">
      <div className="text-center max-w-md">
        {/* 404 Number */}
        <div className="relative mb-6">
          <h1 className="text-[12rem] font-bold leading-none text-transparent bg-clip-text bg-gradient-to-br from-blue-500/30 via-slate-700/50 to-indigo-500/30 select-none">
            404
          </h1>
          <div className="absolute inset-0 flex items-center justify-center">
            <div className="h-20 w-20 rounded-2xl bg-gradient-to-tr from-red-600/20 to-orange-500/20 border border-red-500/30 flex items-center justify-center">
              <ShieldX className="h-10 w-10 text-red-400" />
            </div>
          </div>
        </div>

        {/* Message */}
        <div className="space-y-3 mb-8">
          <h2 className="text-2xl font-bold tracking-tight text-white">
            Trang Không Tìm Thấy
          </h2>
          <p className="text-slate-400 text-sm leading-relaxed">
            Xin lỗi, trang bạn đang tìm kiếm không tồn tại hoặc đã bị di chuyển.
            <br />
            Vui lòng kiểm tra lại đường dẫn hoặc quay về trang chính.
          </p>
        </div>

        {/* Path Display */}
        <div className="mb-8 p-4 rounded-xl bg-slate-900/60 border border-slate-800">
          <p className="text-xs text-slate-500 mb-1">Đường dẫn không hợp lệ:</p>
          <code className="text-sm text-red-400 font-mono">
            {window.location.pathname}
          </code>
        </div>

        {/* Actions */}
        <div className="flex items-center justify-center gap-3">
          <Button
            variant="outline"
            onClick={() => navigate(-1)}
            className="border-slate-700 text-slate-300 hover:bg-slate-800 hover:text-white"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Quay Lại
          </Button>
          <Link to="/">
            <Button className="bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-semibold shadow-md shadow-blue-600/30">
              <Home className="h-4 w-4 mr-2" />
              Trang Chủ
            </Button>
          </Link>
        </div>

        {/* Footer */}
        <p className="mt-10 text-xs text-slate-600">
          Phú Xuân Parking System
        </p>
      </div>
    </div>
  )
}
