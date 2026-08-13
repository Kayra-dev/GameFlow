import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef } from 'react';
import { toast } from 'sonner';

import { API_BASE_URL } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { authStore } from '@/stores/auth-store';
import type { NotificationDto } from '@/types/api';

/**
 * Bildirim ve çevrimiçi durum hub'ına bağlanır.
 *
 * Bağlantı uygulama kabuğunda bir kez kurulur. Kopma durumunda SignalR
 * kendiliğinden yeniden bağlanır; token query string ile taşınır çünkü
 * tarayıcı WebSocket el sıkışmasında Authorization başlığı gönderemez.
 */
export function useRealtimeConnection(): void {
  const queryClient = useQueryClient();
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const { accessToken } = authStore.getState();

    if (!accessToken) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/presence`, {
        // Token her yeniden bağlanmada tazelenir; süresi dolmuşsa yenilenmiş
        // hâli kullanılır.
        accessTokenFactory: () => authStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('NotificationReceived', (notification: NotificationDto) => {
      // Bildirim listesi ve sayaç tazelenir.
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all });

      toast(notification.title, {
        description: notification.message,
      });
    });

    connection.on('UnreadCountChanged', (unreadCount: number) => {
      queryClient.setQueryData(queryKeys.notifications.unreadCount, unreadCount);
    });

    // Çevrimiçi durum değişince dashboard'daki "çevrimiçi kullanıcılar" tazelenir.
    const invalidateDashboard = () => {
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    };

    connection.on('UserOnline', invalidateDashboard);
    connection.on('UserOffline', invalidateDashboard);

    connection.on('WorkItemChanged', () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });
    });

    // React StrictMode geliştirmede efektleri iki kez çalıştırır. Temizlik,
    // start() daha el sıkışmasını bitirmeden stop() çağırırsa bağlantı
    // "negotiation sırasında durduruldu" hatasıyla düşer. Bu yüzden başlatma
    // sözü saklanır ve durdurma her zaman onun ardına zincirlenir.
    const startPromise = connection.start().catch(() => {
      // Bağlantı kurulamazsa uygulama REST üzerinden çalışmaya devam eder.
      // Otomatik yeniden bağlanma devrede olduğu için kullanıcı rahatsız edilmez.
    });

    return () => {
      connectionRef.current = null;

      void startPromise.finally(() => {
        if (connection.state !== HubConnectionState.Disconnected) {
          void connection.stop();
        }
      });
    };
  }, [queryClient]);
}

/**
 * Bir sohbet odasına bağlanır ve odaya ait olayları dinler.
 * Oda değiştiğinde eski gruptan çıkılır, yenisine katılınır.
 */
export function useChatConnection(
  roomId: string | undefined,
  handlers: {
    onMessageReceived?: (message: unknown) => void;
    onMessageEdited?: (message: unknown) => void;
    onMessageDeleted?: (roomId: string, messageId: string) => void;
    onUserTyping?: (roomId: string, userId: string, isTyping: boolean) => void;
  },
): { sendTyping: (isTyping: boolean) => void } {
  const connectionRef = useRef<HubConnection | null>(null);
  // Olay işleyicileri her render'da değişebilir; bağlantıyı yeniden kurmamak
  // için referans üzerinden okunur.
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  useEffect(() => {
    const { accessToken } = authStore.getState();

    if (!roomId || !accessToken) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/chat`, {
        accessTokenFactory: () => authStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10_000])
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('MessageReceived', (message) => handlersRef.current.onMessageReceived?.(message));
    connection.on('MessageEdited', (message) => handlersRef.current.onMessageEdited?.(message));
    connection.on('MessageDeleted', (room: string, messageId: string) =>
      handlersRef.current.onMessageDeleted?.(room, messageId),
    );
    connection.on('UserTyping', (room: string, userId: string, isTyping: boolean) =>
      handlersRef.current.onUserTyping?.(room, userId, isTyping),
    );

    // Yeniden bağlanıldığında odaya tekrar katılmak gerekir; gruplar
    // sunucu tarafında bağlantıya bağlıdır.
    const joinRoom = () => connection.invoke('JoinRoom', roomId).catch(() => undefined);

    connection.onreconnected(joinRoom);

    // Bkz. useRealtimeConnection: temizlik, başlatma tamamlanmadan
    // stop() çağırmamalı (StrictMode çift render'ı).
    const startPromise = connection
      .start()
      .then(joinRoom)
      .catch(() => undefined);

    return () => {
      connectionRef.current = null;

      void startPromise.finally(async () => {
        if (connection.state === HubConnectionState.Connected) {
          await connection.invoke('LeaveRoom', roomId).catch(() => undefined);
        }

        if (connection.state !== HubConnectionState.Disconnected) {
          await connection.stop().catch(() => undefined);
        }
      });
    };
  }, [roomId]);

  const sendTyping = (isTyping: boolean) => {
    const connection = connectionRef.current;

    if (roomId && connection?.state === HubConnectionState.Connected) {
      void connection.invoke('NotifyTyping', roomId, isTyping).catch(() => undefined);
    }
  };

  return { sendTyping };
}
