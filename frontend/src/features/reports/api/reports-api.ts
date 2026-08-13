import { apiClient } from '@/lib/api-client';
import type { ReportsDto } from '@/types/api';

export const reportsApi = {
  async get(params: { projectId?: string; teamId?: string } = {}): Promise<ReportsDto> {
    const { data } = await apiClient.get<ReportsDto>('/reports', { params });
    return data;
  },
};
