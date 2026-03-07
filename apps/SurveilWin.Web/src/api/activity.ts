import api from './client';
import type { ActivitySummaryWindow } from '../types/activity';

export const getMyActivity = (from?: string, to?: string) =>
  api.get<ActivitySummaryWindow[]>('/api/activity/my', { params: { from, to } });
export const getEmployeeActivity = (id: string, from?: string, to?: string) =>
  api.get<ActivitySummaryWindow[]>(`/api/activity/employee/${id}`, { params: { from, to } });
