import api from './client';
import type { Shift } from '../types/shift';

export const getMyShifts = (from?: string, to?: string) =>
  api.get<Shift[]>('/api/shifts/my', { params: { from, to } });
export const getOrgShifts = (from?: string, to?: string) =>
  api.get<Shift[]>('/api/shifts', { params: { from, to } });
