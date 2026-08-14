import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Gamepad2, Loader2, LogIn } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Navigate, useLocation } from 'react-router-dom';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ThemeToggle } from '@/components/layout/ThemeToggle';
import { wakeApi } from '@/lib/api-client';
import { useAuthStore } from '@/stores/auth-store';

import { useLogin } from './use-auth';

const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'E-posta zorunludur.')
    .email('Geçerli bir e-posta adresi girin.'),
  password: z.string().min(1, 'Şifre zorunludur.'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

/**
 * Giriş ekranı. Sistemde kayıt (register) akışı yoktur; hesaplar yalnızca
 * yönetici tarafından oluşturulur, bu yüzden ekranda kayıt bağlantısı bulunmaz.
 */
export function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const login = useLogin();
  const location = useLocation();
  const isAuthenticated = useAuthStore((state) => Boolean(state.accessToken && state.user));

  // Sunucu ücretsiz planda uykuya geçiyor ve uyanması yaklaşık yarım dakika
  // sürüyor. Ekran açılır açılmaz uyandırma başlatılır; kullanıcı bilgilerini
  // yazarken bekleme büyük ölçüde biter.
  useEffect(() => {
    wakeApi();
  }, []);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  if (isAuthenticated) {
    const from = (location.state as { from?: string } | null)?.from;
    return <Navigate to={from ?? '/'} replace />;
  }

  const onSubmit = (values: LoginFormValues) => login.mutate(values);

  return (
    <div className="relative flex min-h-dvh items-center justify-center overflow-hidden px-4 py-10">
      {/* Arka plan: yumuşak ışıma katmanları, içeriğin okunabilirliğini bozmayacak yoğunlukta */}
      <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10">
        <div className="absolute -top-40 -left-32 size-[32rem] rounded-full bg-brand-600/20 blur-[120px]" />
        <div className="absolute -right-32 -bottom-40 size-[32rem] rounded-full bg-info/15 blur-[120px]" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,transparent_0%,var(--background)_75%)]" />
      </div>

      <div className="absolute top-5 right-5">
        <ThemeToggle />
      </div>

      <div className="animate-fade-up w-full max-w-[26rem]">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <div className="grid size-12 place-items-center rounded-2xl bg-primary shadow-float">
            <Gamepad2 className="size-6 text-primary-foreground" aria-hidden="true" />
          </div>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">GameFlow</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Oyun geliştirme ekipleri için proje yönetimi
            </p>
          </div>
        </div>

        <div className="glass rounded-card p-6 shadow-float sm:p-7">
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="email">E-posta</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                autoFocus
                placeholder="ad.soyad@studyo.com"
                aria-invalid={Boolean(errors.email)}
                aria-describedby={errors.email ? 'email-error' : undefined}
                {...register('email')}
              />
              {errors.email ? (
                <p id="email-error" role="alert" className="text-xs text-danger">
                  {errors.email.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Şifre</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  className="pr-11"
                  aria-invalid={Boolean(errors.password)}
                  aria-describedby={errors.password ? 'password-error' : undefined}
                  {...register('password')}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((previous) => !previous)}
                  aria-label={showPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                  className="absolute top-0 right-0 grid h-10 w-11 place-items-center rounded-r-lg text-muted-foreground transition-colors hover:text-foreground"
                >
                  {showPassword ? (
                    <EyeOff className="size-4" aria-hidden="true" />
                  ) : (
                    <Eye className="size-4" aria-hidden="true" />
                  )}
                </button>
              </div>
              {errors.password ? (
                <p id="password-error" role="alert" className="text-xs text-danger">
                  {errors.password.message}
                </p>
              ) : null}
            </div>

            <Button type="submit" size="lg" className="w-full" disabled={login.isPending}>
              {login.isPending ? (
                <>
                  <Loader2 className="animate-spin" aria-hidden="true" />
                  Giriş yapılıyor…
                </>
              ) : (
                <>
                  <LogIn aria-hidden="true" />
                  Giriş yap
                </>
              )}
            </Button>
          </form>

          <p className="mt-5 border-t border-border pt-4 text-center text-xs leading-relaxed text-subtle-foreground">
            Hesaplar yalnızca yönetici tarafından oluşturulur.
            <br />
            Erişim için stüdyo yöneticinizle iletişime geçin.
          </p>
        </div>
      </div>
    </div>
  );
}
