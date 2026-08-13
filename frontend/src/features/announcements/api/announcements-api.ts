import { apiClient } from '@/lib/api-client';
import type { AnnouncementDto } from '@/types/api';
import type { AnnouncementPriority } from '@/types/enums';

export interface AnnouncementListParams {
  projectId?: string;
  includeExpired?: boolean;
}

export interface AnnouncementRequest {
  title: string;
  content: string;
  priority: AnnouncementPriority;
  isPinned: boolean;
  projectId?: string | null;
  expiresAt?: string | null;
}

export const announcementsApi = {
  async list(params: AnnouncementListParams = {}): Promise<AnnouncementDto[]> {
    const { data } = await apiClient.get<AnnouncementDto[]>('/announcements', { params });
    return data;
  },

  async create(request: AnnouncementRequest): Promise<AnnouncementDto> {
    const { data } = await apiClient.post<AnnouncementDto>('/announcements', request);
    return data;
  },

  async update(id: string, request: AnnouncementRequest): Promise<AnnouncementDto> {
    const { data } = await apiClient.put<AnnouncementDto>(`/announcements/${id}`, request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/announcements/${id}`);
  },
};
