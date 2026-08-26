import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { ShieldCheck, Lock, User as UserIcon, Loader2, AlertCircle } from 'lucide-react'
import { useAuthStore } from '@/stores/useAuthStore'
import { authService } from '@/services/authService'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const loginSchema = z.object({
  username: z.string().min(1, 'Vui lòng nhập tên đăng nhập'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      username: 'admin',
      password: '',
    },
  })

  const onSubmit = async (values: LoginFormValues) => {
    setIsLoading(true)
    setErrorMessage(null)
    try {
      const res = await authService.login(values.username, values.password)
      login(res)
      navigate('/')
    } catch (err: any) {
      setErrorMessage(
        err.response?.data?.message || 'Đăng nhập không thành công. Vui lòng kiểm tra lại tài khoản.'
      )
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen w-full flex items-center justify-center p-4 bg-gradient-to-br from-slate-900 via-slate-950 to-blue-950 text-slate-100">
      <div className="w-full max-w-md">
        <Card className="border-slate-800 bg-slate-900/90 shadow-2xl backdrop-blur-xl rounded-2xl">
          <CardHeader className="space-y-3 text-center pb-6">
            <div className="mx-auto h-14 w-14 rounded-2xl bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center text-white shadow-lg shadow-blue-500/30">
              <ShieldCheck className="h-8 w-8" />
            </div>
            <div>
              <CardTitle className="text-xl font-bold tracking-tight text-white">
                HỆ THỐNG QUẢN TRỊ HPPARKING
              </CardTitle>
              <CardDescription className="text-xs text-slate-400 mt-1">
                Smart Parking Management & Access Portal
              </CardDescription>
            </div>
          </CardHeader>

          <CardContent>
            {errorMessage && (
              <div className="mb-5 p-3 rounded-lg bg-red-950/60 border border-red-800/60 text-red-300 text-xs flex items-center gap-2">
                <AlertCircle className="h-4 w-4 shrink-0 text-red-400" />
                <span>{errorMessage}</span>
              </div>
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <UserIcon className="h-3.5 w-3.5 text-slate-400" />
                  Tên đăng nhập
                </label>
                <Input
                  {...register('username')}
                  placeholder="Nhập username (ví dụ: admin)"
                  disabled={isLoading}
                  className="bg-slate-800/80 border-slate-700 text-slate-100 placeholder:text-slate-500 focus-visible:ring-blue-500"
                />
                {errors.username && (
                  <p className="text-[11px] text-red-400 font-medium">{errors.username.message}</p>
                )}
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <Lock className="h-3.5 w-3.5 text-slate-400" />
                  Mật khẩu
                </label>
                <Input
                  type="password"
                  {...register('password')}
                  placeholder="Nhập mật khẩu"
                  disabled={isLoading}
                  className="bg-slate-800/80 border-slate-700 text-slate-100 placeholder:text-slate-500 focus-visible:ring-blue-500"
                />
                {errors.password && (
                  <p className="text-[11px] text-red-400 font-medium">{errors.password.message}</p>
                )}
              </div>

              <Button
                type="submit"
                disabled={isLoading}
                className="w-full h-10 mt-2 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-semibold shadow-md shadow-blue-600/30 rounded-lg cursor-pointer"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    Đang đăng nhập...
                  </>
                ) : (
                  'Đăng Nhập Vào Hệ Thống'
                )}
              </Button>
            </form>

            <div className="mt-6 text-center text-[11px] text-slate-500">
              Phú Xuân Parking System v1.0 • Bảo mật chuẩn JWT
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
