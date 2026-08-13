import { useQuery } from '@tanstack/react-query';
import { Suspense, useState } from 'react';
import { Outlet } from 'react-router-dom';

import { PageLoader } from '@/components/ui/spinner';
import { notificationsApi } from '@/features/notifications/api/notifications-api';
import { GlobalSearch } from '@/features/search/GlobalSearch';
import { useRealtimeConnection } from '@/hooks/use-realtime';
import { queryKeys } from '@/lib/query-client';

import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';

/**
 * Oturum açmış kullanıcının gördüğü uygulama kabuğu.
 * SignalR bağlantısı burada bir kez kurulur ve tüm alt sayfalar için açık kalır.
 */
export function AppShell() {
  const [isSidebarOpen, setSidebarOpen] = useState(false);
  const [isSearchOpen, setSearchOpen] = useState(false);

  useRealtimeConnection();

  const { data: unreadCount = 0 } = useQuery({
    queryKey: queryKeys.notifications.unreadCount,
    queryFn: notificationsApi.unreadCount,
    // Anlık güncelleme SignalR ile gelir; bu sorgu yalnızca ilk değeri sağlar.
    staleTime: 60_000,
  });

  return (
    <div className="flex min-h-dvh">
      <Sidebar isOpen={isSidebarOpen} onClose={() => setSidebarOpen(false)} />

      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar
          onOpenSidebar={() => setSidebarOpen(true)}
          onOpenSearch={() => setSearchOpen(true)}
          unreadNotificationCount={unreadCount}
        />

        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <Suspense fallback={<PageLoader />}>
            <Outlet />
          </Suspense>
        </main>
      </div>

      <GlobalSearch open={isSearchOpen} onOpenChange={setSearchOpen} />
    </div>
  );
}
