import { apiClient } from '@/lib/api-client';
import type { TeamDetail, TeamSummary } from '@/types/api';
import type { TeamCategory } from '@/types/enums';

export interface TeamListParams {
  search?: string;
  category?: TeamCategory;
  onlyMine?: boolean;
}

export interface CreateTeamRequest {
  name: string;
  description?: string | null;
  category: TeamCategory;
  colorHex: string;
  iconKey?: string | null;
  leaderId?: string | null;
  memberIds: string[];
}

export interface UpdateTeamRequest {
  name: string;
  description?: string | null;
  category: TeamCategory;
  colorHex: string;
  iconKey?: string | null;
}

export const teamsApi = {
  async list(params: TeamListParams = {}): Promise<TeamSummary[]> {
    const { data } = await apiClient.get<TeamSummary[]>('/teams', { params });
    return data;
  },

  async detail(id: string): Promise<TeamDetail> {
    const { data } = await apiClient.get<TeamDetail>(`/teams/${id}`);
    return data;
  },

  async create(request: CreateTeamRequest): Promise<TeamDetail> {
    const { data } = await apiClient.post<TeamDetail>('/teams', request);
    return data;
  },

  async update(id: string, request: UpdateTeamRequest): Promise<TeamDetail> {
    const { data } = await apiClient.put<TeamDetail>(`/teams/${id}`, request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/teams/${id}`);
  },

  /** userId null gönderilirse takımın liderliği boşaltılır. */
  async assignLeader(id: string, userId: string | null): Promise<TeamDetail> {
    const { data } = await apiClient.put<TeamDetail>(`/teams/${id}/leader`, { userId });
    return data;
  },

  async addMembers(id: string, userIds: string[]): Promise<TeamDetail> {
    const { data } = await apiClient.post<TeamDetail>(`/teams/${id}/members`, { userIds });
    return data;
  },

  async removeMember(id: string, userId: string): Promise<void> {
    await apiClient.delete(`/teams/${id}/members/${userId}`);
  },
};
