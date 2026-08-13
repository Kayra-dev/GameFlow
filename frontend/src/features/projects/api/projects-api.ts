import { apiClient } from '@/lib/api-client';
import type { ProjectDetail, ProjectSummary } from '@/types/api';
import type { ProjectStatus } from '@/types/enums';

export interface ProjectListParams {
  search?: string;
  status?: ProjectStatus;
  onlyMine?: boolean;
}

export interface CreateProjectRequest {
  name: string;
  /** Görev anahtarı öneki (örn. "ODY" → ODY-42). Sunucu büyük harfe çevirir. */
  key: string;
  description?: string | null;
  status: ProjectStatus;
  colorHex: string;
  genre?: string | null;
  platforms?: string | null;
  startDate?: string | null;
  targetReleaseDate?: string | null;
  memberIds: string[];
}

export interface UpdateProjectRequest {
  name: string;
  description?: string | null;
  status: ProjectStatus;
  colorHex: string;
  genre?: string | null;
  platforms?: string | null;
  startDate?: string | null;
  targetReleaseDate?: string | null;
}

export const projectsApi = {
  async list(params: ProjectListParams = {}): Promise<ProjectSummary[]> {
    const { data } = await apiClient.get<ProjectSummary[]>('/projects', { params });
    return data;
  },

  async detail(id: string): Promise<ProjectDetail> {
    const { data } = await apiClient.get<ProjectDetail>(`/projects/${id}`);
    return data;
  },

  async create(request: CreateProjectRequest): Promise<ProjectDetail> {
    const { data } = await apiClient.post<ProjectDetail>('/projects', request);
    return data;
  },

  async update(id: string, request: UpdateProjectRequest): Promise<ProjectDetail> {
    const { data } = await apiClient.put<ProjectDetail>(`/projects/${id}`, request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/projects/${id}`);
  },

  async addMembers(id: string, userIds: string[], isManager = false): Promise<ProjectDetail> {
    const { data } = await apiClient.post<ProjectDetail>(`/projects/${id}/members`, {
      userIds,
      isManager,
    });
    return data;
  },

  async removeMember(id: string, userId: string): Promise<void> {
    await apiClient.delete(`/projects/${id}/members/${userId}`);
  },

  async setManager(id: string, userId: string, isManager: boolean): Promise<void> {
    await apiClient.put(`/projects/${id}/members/${userId}/manager`, null, {
      params: { isManager },
    });
  },
};
