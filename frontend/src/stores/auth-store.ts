import { create } from 'zustand';
import { persist } from 'zustand/middleware';

import { SystemRole } from '@/types/enums';
import type { AuthResponse, CurrentUser } from '@/types/api';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: CurrentUser | null;
  /** Uygulama açılışında oturum doğrulaması tamamlandı mı. */
  isInitialized: boolean;
}

interface AuthActions {
  setSession: (response: AuthResponse) => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  setUser: (user: CurrentUser) => void;
  setInitialized: (value: boolean) => void;
  clearSession: () => void;
}

const initialState: AuthState = {
  accessToken: null,
  refreshToken: null,
  user: null,
  isInitialized: false,
};

/**
 * Oturum durumu. Uygulama verileri asla istemcide tutulmaz; burada yalnızca
 * kimlik doğrulama tokenları ve oturum sahibinin özeti saklanır.
 */
export const useAuthStore = create<AuthState & AuthActions>()(
  persist(
    (set) => ({
      ...initialState,

      setSession: (response) =>
        set({
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
          user: response.user,
        }),

      setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),

      setUser: (user) => set({ user }),

      setInitialized: (value) => set({ isInitialized: value }),

      clearSession: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: 'gameflow-session',
      // Yalnızca tokenlar ve kullanıcı özeti kalıcı olur; oturum durumu bayrağı olmaz.
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
      }),
    },
  ),
);

/** React bileşeni dışından (örn. axios interceptor) senkron erişim. */
export const authStore = {
  getState: useAuthStore.getState,
  setState: useAuthStore.setState,
};

export function isAdmin(user: CurrentUser | null): boolean {
  return user?.role === SystemRole.Admin;
}

export function isLeader(user: CurrentUser | null): boolean {
  return user?.role === SystemRole.Admin || user?.role === SystemRole.TeamLeader;
}
