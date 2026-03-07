import api from './client';
import type { AuthResponse } from '../types/auth';

export const login = (email: string, password: string) =>
  api.post<AuthResponse>('/api/auth/login', { email, password });

export const acceptInvite = (token: string, password: string, confirmPassword: string) =>
  api.post<AuthResponse>('/api/auth/accept-invite', { token, password, confirmPassword });
