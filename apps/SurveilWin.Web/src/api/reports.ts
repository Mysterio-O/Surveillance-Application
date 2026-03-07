import api from './client';
import type { DailyReport } from '../types/activity';

export const getDailyReport = (employeeId: string, date: string) =>
  api.get<DailyReport>(`/api/reports/daily/${employeeId}/${date}`);
export const getTeamReport = (date?: string) =>
  api.get('/api/reports/team', { params: { date } });
export const getOrgPolicy = (orgId: string) =>
  api.get(`/api/organizations/${orgId}/policy`);
export const updateOrgPolicy = (orgId: string, data: Record<string, unknown>) =>
  api.put(`/api/organizations/${orgId}/policy`, data);
