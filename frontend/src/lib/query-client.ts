import { QueryClient } from '@tanstack/react-query';
import axios from 'axios';

/**
 * TanStack Query yapılandırması.
 * Sunucu verisi tek doğruluk kaynağıdır; istemcide kopyalanmaz.
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Veri 30 saniye tazedir; sekme değiştirmede gereksiz istek atılmaz.
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Yetki ve doğrulama hatalarını yeniden denemek anlamsızdır.
          if (axios.isAxiosError(error)) {
            const status = error.response?.status ?? 0;
            if (status >= 400 && status < 500) return false;
          }

          return failureCount < 2;
        },
      },
      mutations: {
        retry: false,
      },
    },
  });
}

/**
 * Sorgu anahtarları. Tek yerde toplanır ki geçersiz kılma (invalidation)
 * sırasında yazım hatası yüzünden önbellek bayat kalmasın.
 */
export const queryKeys = {
  currentUser: ['current-user'] as const,

  dashboard: (projectId?: string, onlyMyTasks?: boolean) =>
    ['dashboard', { projectId, onlyMyTasks }] as const,

  users: {
    all: ['users'] as const,
    list: (params: unknown) => ['users', 'list', params] as const,
    detail: (id: string) => ['users', 'detail', id] as const,
    assignable: ['users', 'assignable'] as const,
  },

  teams: {
    all: ['teams'] as const,
    list: (params: unknown) => ['teams', 'list', params] as const,
    detail: (id: string) => ['teams', 'detail', id] as const,
  },

  projects: {
    all: ['projects'] as const,
    list: (params: unknown) => ['projects', 'list', params] as const,
    detail: (id: string) => ['projects', 'detail', id] as const,
    labels: (id: string) => ['projects', id, 'labels'] as const,
  },

  workItems: {
    all: ['work-items'] as const,
    list: (params: unknown) => ['work-items', 'list', params] as const,
    detail: (id: string) => ['work-items', 'detail', id] as const,
    byKey: (key: string) => ['work-items', 'by-key', key] as const,
    board: (params: unknown) => ['work-items', 'board', params] as const,
    deadlines: (params: unknown) => ['work-items', 'deadlines', params] as const,
  },

  sprints: {
    all: ['sprints'] as const,
    list: (params: unknown) => ['sprints', 'list', params] as const,
    detail: (id: string) => ['sprints', 'detail', id] as const,
    report: (id: string) => ['sprints', 'report', id] as const,
  },

  chat: {
    rooms: ['chat', 'rooms'] as const,
    messages: (roomId: string) => ['chat', 'messages', roomId] as const,
  },

  notifications: {
    all: ['notifications'] as const,
    unreadCount: ['notifications', 'unread-count'] as const,
  },

  calendar: (params: unknown) => ['calendar', params] as const,
  meetings: (params: unknown) => ['meetings', params] as const,
  announcements: (params: unknown) => ['announcements', params] as const,
  reports: (params: unknown) => ['reports', params] as const,
  search: (query: string) => ['search', query] as const,
  roles: ['roles'] as const,
};
