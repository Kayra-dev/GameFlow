import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';

import { EmptyState } from '@/components/ui/empty-state';
import { ShieldAlert } from 'lucide-react';
import { useAuthStore } from '@/stores/auth-store';
import type { SystemRole } from '@/types/enums';

type ProtectedRouteProps = {
  children: ReactNode;
  /** Boşsa yalnızca oturum açmış olmak yeterli. */
  allowedRoles?: SystemRole[];
};

/**
 * Oturum ve rol denetimi. Bu yalnızca arayüz katmanıdır; her uç nokta
 * sunucuda da ayrıca yetkilendirilir.
 */
export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const location = useLocation();
  const { accessToken, user } = useAuthStore();

  if (!accessToken || !user) {
    // Girişten sonra kullanıcı istediği sayfaya geri döndürülür.
    return <Navigate to="/giris" replace state={{ from: location.pathname }} />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return (
      <EmptyState
        icon={ShieldAlert}
        title="Bu sayfaya erişim yetkiniz yok"
        description="Sayfayı görüntülemek için gereken role sahip değilsiniz. Yöneticinizle görüşün."
      />
    );
  }

  return children;
}
