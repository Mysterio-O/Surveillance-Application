import api from './client';
import type { UserProfile } from '../types/user';

export const listUsers = () => api.get<UserProfile[]>('/api/users');
export const getUser = (id: string) => api.get<UserProfile>(`/api/users/${id}`);
export const inviteUser = (data: { email: string; role: string; firstName: string; lastName: string; managerId?: string }) =>
  api.post('/api/users/invite', data);
export const updateUserRole = (id: string, role: string) => api.put(`/api/users/${id}/role`, { role });
export const deactivateUser = (id: string) => api.delete(`/api/users/${id}`);
export const getMyTeam = () => api.get<UserProfile[]>('/api/users/my-team');
