import { apiClient } from '@/lib/api-client';
import type { PagedResult, UserDetail, UserSummary } from '@/types/api';
import type { SystemRole } from '@/types/enums';

export interface UserListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: SystemRole;
  teamId?: string;
  isActive?: boolean;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: SystemRole;
  jobTitle?: string | null;
  bio?: string | null;
  mustChangePassword: boolean;
  teamIds: string[];
}

export interface UpdateUserRequest {
  fullName: string;
  role: SystemRole;
  jobTitle?: string | null;
  bio?: string | null;
  isActive: boolean;
}

export interface ResetPasswordRequest {
  newPassword: string;
  mustChangePassword: boolean;
}

export const usersApi = {
  async list(params: UserListParams = {}): Promise<PagedResult<UserSummary>> {
    const { data } = await apiClient.get<PagedResult<UserSummary>>('/users', { params });
    return data;
  },

  async assignable(): Promise<UserSummary[]> {
    const { data } = await apiClient.get<UserSummary[]>('/users/assignable');
    return data;
  },

  async detail(id: string): Promise<UserDetail> {
    const { data } = await apiClient.get<UserDetail>(`/users/${id}`);
    return data;
  },

  async create(request: CreateUserRequest): Promise<UserDetail> {
    const { data } = await apiClient.post<UserDetail>('/users', request);
    return data;
  },

  async update(id: string, request: UpdateUserRequest): Promise<UserDetail> {
    const { data } = await apiClient.put<UserDetail>(`/users/${id}`, request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/users/${id}`);
  },

  async resetPassword(id: string, request: ResetPasswordRequest): Promise<void> {
    await apiClient.post(`/users/${id}/reset-password`, request);
  },
};
