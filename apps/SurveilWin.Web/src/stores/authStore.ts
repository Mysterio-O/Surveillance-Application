import { create } from 'zustand';
import type { AuthUser } from '../types/auth';

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (token: string, refreshToken: string, user: AuthUser) => void;
  logout: () => void;
}

const STORAGE_KEY = 'surveilwin_auth';

const loadFromStorage = (): Partial<AuthState> => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    return JSON.parse(raw);
  } catch { return {}; }
};

export const useAuthStore = create<AuthState>((set) => {
  const saved = loadFromStorage();
  return {
    user: saved.user ?? null,
    accessToken: saved.accessToken ?? null,
    isAuthenticated: !!saved.accessToken && !!saved.user,

    login: (accessToken, refreshToken, user) => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ accessToken, refreshToken, user }));
      set({ user, accessToken, isAuthenticated: true });
    },

    logout: () => {
      localStorage.removeItem(STORAGE_KEY);
      set({ user: null, accessToken: null, isAuthenticated: false });
    },
  };
});
