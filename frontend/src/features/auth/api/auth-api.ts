import { apiClient } from '@/lib/api-client';
import type {
  AuthResponse,
  CurrentUser,
  LoginRequest,
} from '@/types/api';

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UpdateProfileRequest {
  fullName: string;
  jobTitle?: string | null;
  bio?: string | null;
}

export const authApi = {
  async login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/login', request);
    return data;
  },

  async me(): Promise<CurrentUser> {
    const { data } = await apiClient.get<CurrentUser>('/auth/me');
    return data;
  },

  async logout(refreshToken: string): Promise<void> {
    await apiClient.post('/auth/logout', { refreshToken });
  },

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await apiClient.post('/auth/change-password', request);
  },

  async updateProfile(request: UpdateProfileRequest): Promise<CurrentUser> {
    const { data } = await apiClient.put<CurrentUser>('/auth/profile', request);
    return data;
  },
};
