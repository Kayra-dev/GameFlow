import { apiClient } from '@/lib/api-client';
import type { DashboardDto } from '@/types/api';

export interface DashboardParams {
  projectId?: string;
  onlyMyTasks?: boolean;
  upcomingDays?: number;
}

export const dashboardApi = {
  async get(params: DashboardParams = {}): Promise<DashboardDto> {
    const { data } = await apiClient.get<DashboardDto>('/dashboard', { params });
    return data;
  },
};
