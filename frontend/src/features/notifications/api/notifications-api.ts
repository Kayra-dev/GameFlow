import { apiClient } from '@/lib/api-client';
import type { NotificationDto, PagedResult } from '@/types/api';

export interface NotificationListParams {
  page?: number;
  pageSize?: number;
  onlyUnread?: boolean;
}

export const notificationsApi = {
  async list(params: NotificationListParams = {}): Promise<PagedResult<NotificationDto>> {
    const { data } = await apiClient.get<PagedResult<NotificationDto>>('/notifications', {
      params,
    });
    return data;
  },

  async unreadCount(): Promise<number> {
    const { data } = await apiClient.get<number>('/notifications/unread-count');
    return data;
  },

  async markAsRead(id: string): Promise<void> {
    await apiClient.put(`/notifications/${id}/read`);
  },

  async markAllAsRead(): Promise<void> {
    await apiClient.put('/notifications/read-all');
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/notifications/${id}`);
  },
};
