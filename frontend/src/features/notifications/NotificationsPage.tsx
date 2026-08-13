import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, CheckCheck, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

import { Avatar } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { getErrorMessage } from '@/lib/api-client';
import { formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';

import { notificationsApi } from './api/notifications-api';

export function NotificationsPage() {
  const [onlyUnread, setOnlyUnread] = useState(false);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const params = { page: 1, pageSize: 50, onlyUnread };

  const { data, isLoading } = useQuery({
    queryKey: [...queryKeys.notifications.all, params],
    queryFn: () => notificationsApi.list(params),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all });
  };

  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: invalidate,
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const markAllRead = useMutation({
    mutationFn: notificationsApi.markAllAsRead,
    onSuccess: () => {
      invalidate();
      toast.success('Tüm bildirimler okundu işaretlendi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (id: string) => notificationsApi.remove(id),
    onSuccess: invalidate,
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <div className="mx-auto w-full max-w-3xl space-y-5">
      <header className="flex flex-wrap items-center gap-3">
        <div className="flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">Bildirimler</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Görev atamaları, yorumlar, sprint ve toplantı hareketleri.
          </p>
        </div>

        <Button
          variant="secondary"
          size="sm"
          onClick={() => markAllRead.mutate()}
          disabled={markAllRead.isPending}
        >
          <CheckCheck aria-hidden="true" />
          Tümünü okundu yap
        </Button>
      </header>

      <Tabs
        value={onlyUnread ? 'unread' : 'all'}
        onValueChange={(value) => setOnlyUnread(value === 'unread')}
      >
        <TabsList>
          <TabsTrigger value="all">Tümü</TabsTrigger>
          <TabsTrigger value="unread">Okunmamış</TabsTrigger>
        </TabsList>
      </Tabs>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }, (_, index) => (
            <Skeleton key={index} className="h-16 rounded-card" />
          ))}
        </div>
      ) : data && data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={Bell}
            title={onlyUnread ? 'Okunmamış bildirim yok' : 'Bildirim yok'}
            description="Görev atandığında, yorum yapıldığında veya sprint başladığında burada görürsünüz."
          />
        </Card>
      ) : (
        <ul className="space-y-2">
          {data?.items.map((notification) => (
            <li key={notification.id}>
              <Card
                className={cn(
                  'flex items-start gap-3 p-4',
                  // Okunmamışlar sol kenarda renkli çizgiyle ayrılır.
                  !notification.isRead && 'border-l-2 border-l-primary',
                )}
              >
                <Avatar
                  fullName={notification.actor?.fullName ?? 'Sistem'}
                  avatarUrl={notification.actor?.avatarUrl}
                  size="sm"
                />

                <button
                  type="button"
                  onClick={() => {
                    if (!notification.isRead) markRead.mutate(notification.id);
                    if (notification.link) navigate(notification.link);
                  }}
                  className="min-w-0 flex-1 text-left outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <p className="text-sm font-medium">{notification.title}</p>
                  <p className="mt-0.5 text-sm text-muted-foreground">{notification.message}</p>
                  <p className="mt-1 text-[11px] text-subtle-foreground">
                    {formatRelative(notification.createdAt)}
                  </p>
                </button>

                <Button
                  variant="ghost"
                  size="icon-sm"
                  onClick={() => remove.mutate(notification.id)}
                  aria-label="Bildirimi sil"
                >
                  <Trash2 className="text-danger" aria-hidden="true" />
                </Button>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
