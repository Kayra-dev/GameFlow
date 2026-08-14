import axios, {
  AxiosHeaders,
  type AxiosError,
  type AxiosInstance,
  type InternalAxiosRequestConfig,
} from 'axios';

import { authStore } from '@/stores/auth-store';
import type { ApiProblem, AuthResponse } from '@/types/api';

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

/**
 * İstek zaman aşımı.
 *
 * Ücretsiz barındırma planlarında sunucu hareketsizlik sonrası uykuya geçer ve
 * ilk istek konteyner ayağa kalkana kadar bekler — ölçülen süre ~35 saniye.
 * Sınır bunun altında kalırsa uykudan sonraki her ilk istek hatasız bir sunucuda
 * bile "zaman aşımı" ile düşer. Sınır, uyanma süresinin rahatça üstünde tutulur.
 */
const REQUEST_TIMEOUT_MS = 90_000;

/** Sonsuz döngüyü önlemek için bir istek en fazla bir kez yenilenir. */
type RetriableConfig = InternalAxiosRequestConfig & {
  _retried?: boolean;
  _timeoutRetried?: boolean;
};

export const apiClient: AxiosInstance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  timeout: REQUEST_TIMEOUT_MS,
  headers: { 'Content-Type': 'application/json' },
});

/**
 * Sunucuyu uykudan uyandırmaya başlar ve sonucu beklemez.
 *
 * Giriş ekranı açılır açılmaz çağrılır: kullanıcı e-postasını ve şifresini
 * yazarken sunucu arka planda ayağa kalkar, "Giriş yap"a basıldığında bekleme
 * çoğunlukla bitmiş olur. Hata yutulur; bu yalnızca bir hızlandırmadır.
 */
export function wakeApi(): void {
  if (!API_BASE_URL) return;

  void axios
    .get(`${API_BASE_URL}/health`, { timeout: REQUEST_TIMEOUT_MS })
    .catch(() => undefined);
}

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
    // Zaman aşımı diğer isteklerle aynı: uykudaki sunucuda daha kısa bir sınır,
    // oturumu geçerli olan kullanıcıyı boş yere giriş ekranına atıyordu.
    { headers: { 'Content-Type': 'application/json' }, timeout: REQUEST_TIMEOUT_MS },
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

    // Zaman aşımı: sunucu uykudaysa ilk istek yanıtsız kalır ama o istek
    // konteyneri ayağa kaldırır. Tek bir tekrar çoğunlukla başarılı olur.
    //
    // Yalnızca GET tekrarlanır: zaman aşımında isteğin sunucuya ulaşıp
    // ulaşmadığı bilinemez, bu yüzden yazma isteklerini tekrarlamak kaydı
    // ikinci kez oluşturma riski taşır.
    const isTimeout = error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT';
    const isSafeMethod = (config?.method ?? 'get').toLowerCase() === 'get';

    if (isTimeout && config && isSafeMethod && !config._timeoutRetried) {
      config._timeoutRetried = true;
      return apiClient.request(config);
    }

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

    if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') {
      // Sunucu ücretsiz planda uykuya geçtiği için ilk istek uzun sürebilir;
      // kullanıcı "bir şey bozuldu" sanmasın diye sebep açıkça yazılır.
      return 'Sunucu uykudan uyanıyor, bu ilk seferde bir dakikayı bulabilir. Lütfen tekrar deneyin.';
    }

    if (!error.response) {
      return 'Sunucuya ulaşılamıyor. İnternet bağlantınızı kontrol edin.';
    }
  }

  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.';
}
