import { apiClient } from '@/lib/api-client';
import type { SprintDetail, SprintReportDto, SprintSummary } from '@/types/api';
import type { SprintStatus } from '@/types/enums';

export interface SprintListParams {
  projectId?: string;
  teamId?: string;
  status?: SprintStatus;
}

export interface SprintRequest {
  projectId?: string;
  teamId?: string | null;
  name: string;
  goal?: string | null;
  startDate: string;
  endDate: string;
}

export const sprintsApi = {
  async list(params: SprintListParams = {}): Promise<SprintSummary[]> {
    const { data } = await apiClient.get<SprintSummary[]>('/sprints', { params });
    return data;
  },

  async detail(id: string): Promise<SprintDetail> {
    const { data } = await apiClient.get<SprintDetail>(`/sprints/${id}`);
    return data;
  },

  async report(id: string): Promise<SprintReportDto> {
    const { data } = await apiClient.get<SprintReportDto>(`/sprints/${id}/report`);
    return data;
  },

  async create(request: SprintRequest): Promise<SprintDetail> {
    const { data } = await apiClient.post<SprintDetail>('/sprints', request);
    return data;
  },

  async start(id: string): Promise<SprintDetail> {
    const { data } = await apiClient.post<SprintDetail>(`/sprints/${id}/start`);
    return data;
  },

  async complete(
    id: string,
    retrospectiveNotes?: string,
    moveUnfinishedToSprintId?: string,
  ): Promise<SprintReportDto> {
    const { data } = await apiClient.post<SprintReportDto>(`/sprints/${id}/complete`, {
      retrospectiveNotes: retrospectiveNotes ?? null,
      moveUnfinishedToSprintId: moveUnfinishedToSprintId ?? null,
    });
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/sprints/${id}`);
  },
};
