import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect } from 'react';
import { toast } from 'sonner';

import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import type { LoginRequest } from '@/types/api';

import { authApi } from './api/auth-api';

/**
 * Oturum yönetimi.
 *
 * Uygulama açılışında saklanan token ile /auth/me çağrılır; token geçersizse
 * axios interceptor'ı yenilemeyi dener, o da başarısız olursa oturum düşürülür.
 * Böylece sayfa yenilendiğinde kullanıcı oturumunu kaybetmez.
 */
export function useAuthBootstrap() {
  const { accessToken, user, isInitialized, setUser, setInitialized, clearSession } =
    useAuthStore();

  const { data, isError, isSuccess, isLoading } = useQuery({
    queryKey: queryKeys.currentUser,
    queryFn: authApi.me,
    // Token yoksa istek atılmaz; doğrudan giriş ekranına düşülür.
    enabled: Boolean(accessToken) && !isInitialized,
    retry: false,
    staleTime: 60_000,
  });

  useEffect(() => {
    if (!accessToken) {
      setInitialized(true);
      return;
    }

    if (isSuccess && data) {
      setUser(data);
      setInitialized(true);
    }

    if (isError) {
      clearSession();
      setInitialized(true);
    }
  }, [accessToken, data, isError, isSuccess, setUser, setInitialized, clearSession]);

  return {
    user,
    isAuthenticated: Boolean(accessToken && user),
    isLoading: isLoading && !isInitialized,
    isInitialized,
  };
}

export function useLogin() {
  const setSession = useAuthStore((state) => state.setSession);
  const setInitialized = useAuthStore((state) => state.setInitialized);
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: LoginRequest) => authApi.login(request),
    onSuccess: (response) => {
      setSession(response);
      setInitialized(true);

      // Önceki oturumdan kalan önbellek temizlenir.
      queryClient.clear();
      queryClient.setQueryData(queryKeys.currentUser, response.user);
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });
}

export function useLogout() {
  const { refreshToken, clearSession } = useAuthStore();
  const queryClient = useQueryClient();

  return useCallback(async () => {
    // Sunucuda refresh token iptal edilir; başarısız olsa bile yerel oturum kapanır.
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        // Ağ hatası çıkışı engellemez.
      }
    }

    clearSession();
    queryClient.clear();
  }, [refreshToken, clearSession, queryClient]);
}
