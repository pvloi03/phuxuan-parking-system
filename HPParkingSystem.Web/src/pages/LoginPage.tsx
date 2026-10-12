import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Lock, User as UserIcon, Loader2, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuthStore } from '@/stores/useAuthStore'
import { authService } from '@/services/authService'
import logoImg from '@/assets/logo.png'

const loginSchema = z.object({
  username: z.string().min(1, 'Vui lòng nhập tên đăng nhập'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const login = useAuthStore((s) => s.login)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const fromPath = (location.state as { from?: { pathname?: string } })?.from?.pathname || '/'

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
      navigate(fromPath, { replace: true })
    } catch (err: any) {
      setErrorMessage(
        err.response?.data?.message ||
        err.message ||
        'Đăng nhập không thành công. Vui lòng kiểm tra lại tài khoản.'
      )
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen w-full relative flex items-center justify-center p-4 bg-linear-to-br from-slate-50 via-slate-100 to-blue-50/60 text-slate-900 selection:bg-blue-600 selection:text-white overflow-hidden">
      {/* Ambient background glow accents for depth */}
      <div
        className="absolute -top-40 -left-40 w-96 h-96 rounded-full bg-blue-400/15 blur-3xl pointer-events-none"
        aria-hidden="true"
      />
      <div
        className="absolute -bottom-40 -right-40 w-96 h-96 rounded-full bg-indigo-400/15 blur-3xl pointer-events-none"
        aria-hidden="true"
      />

      <div className="w-full max-w-md relative z-10">
        <Card className="border-slate-200/80 bg-white/95 shadow-[0_20px_50px_rgba(15,23,42,0.08)] backdrop-blur-md rounded-2xl transition-none">
          <CardHeader className="space-y-4 text-center pb-6 pt-8">
            <div className="flex items-center justify-center">
              <img
                src={logoImg}
                alt="HPParking Logo"
                className="h-16 sm:h-20 w-auto max-w-70 object-contain"
              />
            </div>
            <div>
              <CardTitle className="text-xl font-bold tracking-tight text-slate-900">
                HỆ THỐNG QUẢN TRỊ HPPARKING
              </CardTitle>
              <CardDescription className="text-xs text-slate-500 mt-1">
                Trung tâm quản trị & giám sát thông minh
              </CardDescription>
            </div>
          </CardHeader>

          <CardContent className="px-6 sm:px-8 pb-8">
            {errorMessage && (
              <div className="mb-5 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs flex items-center gap-2 animate-in fade-in-50">
                <AlertCircle className="h-4 w-4 shrink-0 text-red-600" />
                <span>{errorMessage}</span>
              </div>
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="space-y-1.5">
                <label
                  htmlFor="username"
                  className="text-xs font-semibold text-slate-700 flex items-center gap-1.5"
                >
                  <UserIcon className="h-3.5 w-3.5 text-slate-500" />
                  Tên đăng nhập
                </label>
                <Input
                  id="username"
                  {...register('username')}
                  placeholder="Nhập tên tài khoản (ví dụ: admin)"
                  disabled={isLoading}
                  autoComplete="username"
                  className="shadow-none bg-slate-50/80 border-slate-200 text-slate-900 placeholder:text-slate-400 focus-visible:bg-white focus-visible:border-blue-500 focus-visible:ring-blue-500"
                />
                {errors.username && (
                  <p className="text-[11px] text-red-600 font-medium">
                    {errors.username.message}
                  </p>
                )}
              </div>

              <div className="space-y-1.5">
                <label
                  htmlFor="password"
                  className="text-xs font-semibold text-slate-700 flex items-center gap-1.5"
                >
                  <Lock className="h-3.5 w-3.5 text-slate-500" />
                  Mật khẩu
                </label>
                <Input
                  id="password"
                  type="password"
                  {...register('password')}
                  placeholder="Nhập mật khẩu"
                  disabled={isLoading}
                  autoComplete="current-password"
                  className="shadow-none bg-slate-50/80 border-slate-200 text-slate-900 placeholder:text-slate-400 focus-visible:bg-white focus-visible:border-blue-500 focus-visible:ring-blue-500"
                />
                {errors.password && (
                  <p className="text-[11px] text-red-600 font-medium">
                    {errors.password.message}
                  </p>
                )}
              </div>

              <Button
                type="submit"
                disabled={isLoading}
                className="w-full h-10 mt-2 bg-linear-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white font-semibold shadow-md shadow-blue-500/25 rounded-lg cursor-pointer transition-all active:scale-[0.99]"
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
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default LoginPage
