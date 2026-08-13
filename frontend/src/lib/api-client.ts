import axios, {
  AxiosHeaders,
  type AxiosError,
  type AxiosInstance,
  type InternalAxiosRequestConfig,
} from 'axios';

import { authStore } from '@/stores/auth-store';
import type { ApiProblem, AuthResponse } from '@/types/api';

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

/** Sonsuz döngüyü önlemek için bir istek en fazla bir kez yenilenir. */
type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean };

export const apiClient: AxiosInstance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const { accessToken } = authStore.getState();

  if (accessToken) {
    const headers = AxiosHeaders.from(config.headers);
    headers.set('Authorization', `Bearer ${accessToken}`);
    config.headers = headers;
  }

  return config;
});

/**
 * Aynı anda 401 alan birden fazla isteğin ayrı ayrı yenileme denemesini
 * engellemek için tek bir yenileme sözü paylaşılır.
 */
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const { refreshToken } = authStore.getState();

  if (!refreshToken) {
    throw new Error('Yenileme tokenı bulunmuyor.');
  }

  // Interceptor'a takılmaması için ayrı bir istemci kullanılır.
  const { data } = await axios.post<AuthResponse>(
    `${API_BASE_URL}/api/auth/refresh`,
    { refreshToken },
    { headers: { 'Content-Type': 'application/json' }, timeout: 15_000 },
  );

  authStore.getState().setSession(data);

  return data.accessToken;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiProblem>) => {
    const config = error.config as RetriableConfig | undefined;
    const status = error.response?.status;

    const isRefreshCall = config?.url?.includes('/auth/refresh') ?? false;

    if (status !== 401 || !config || config._retried || isRefreshCall) {
      return Promise.reject(error);
    }

    config._retried = true;

    try {
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null;
      });

      const accessToken = await refreshPromise;

      const headers = AxiosHeaders.from(config.headers);
      headers.set('Authorization', `Bearer ${accessToken}`);
      config.headers = headers;

      return apiClient.request(config);
    } catch {
      // Yenileme de başarısızsa oturum sonlandırılır ve giriş ekranına yönlendirilir.
      authStore.getState().clearSession();

      if (!window.location.pathname.endsWith('/giris')) {
        window.location.assign(`${import.meta.env.BASE_URL}giris`);
      }

      return Promise.reject(error);
    }
  },
);

/** Axios hatasından kullanıcıya gösterilebilir Türkçe mesaj üretir. */
export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiProblem>(error)) {
    const problem = error.response?.data;

    if (problem?.errors) {
      const firstField = Object.values(problem.errors)[0];
      if (firstField?.[0]) return firstField[0];
    }

    if (problem?.title) return problem.title;

    if (error.code === 'ECONNABORTED') {
      return 'İstek zaman aşımına uğradı. Lütfen tekrar deneyin.';
    }

    if (!error.response) {
      return 'Sunucuya ulaşılamıyor. İnternet bağlantınızı kontrol edin.';
    }
  }

  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.';
}
