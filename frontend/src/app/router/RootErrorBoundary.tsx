import { TriangleAlert } from 'lucide-react';
import { isRouteErrorResponse, useRouteError } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';

/**
 * Rota seviyesinde yakalanan hatalar. Beyaz ekran yerine kullanıcıya
 * anlaşılır bir mesaj ve toparlanma yolu sunar.
 */
export function RootErrorBoundary() {
  const error = useRouteError();

  const description = isRouteErrorResponse(error)
    ? `Sunucu ${error.status} yanıtı döndü.`
    : error instanceof Error
      ? error.message
      : 'Beklenmeyen bir hata oluştu.';

  return (
    <div className="grid min-h-dvh place-items-center p-6">
      <EmptyState
        icon={TriangleAlert}
        title="Bir şeyler ters gitti"
        description={description}
        action={
          <Button variant="secondary" onClick={() => window.location.reload()}>
            Sayfayı yenile
          </Button>
        }
      />
    </div>
  );
}
