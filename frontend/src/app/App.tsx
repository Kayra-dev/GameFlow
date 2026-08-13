import { RouterProvider } from 'react-router-dom';

import { PageLoader } from '@/components/ui/spinner';
import { useAuthBootstrap } from '@/features/auth/use-auth';

import { AppProviders } from './providers/AppProviders';
import { router } from './router/routes';

/**
 * Oturum doğrulaması tamamlanana kadar yönlendirme başlatılmaz; aksi halde
 * geçerli oturumu olan kullanıcı bir an giriş ekranını görürdü.
 */
function AppRoutes() {
  const { isInitialized } = useAuthBootstrap();

  if (!isInitialized) {
    return (
      <div className="grid min-h-dvh place-items-center">
        <PageLoader label="Oturum kontrol ediliyor…" />
      </div>
    );
  }

  return <RouterProvider router={router} />;
}

export function App() {
  return (
    <AppProviders>
      <AppRoutes />
    </AppProviders>
  );
}
