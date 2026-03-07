import type { UserRole } from '../types/auth';

export const canViewEmployees = (role: UserRole) =>
  ['SuperAdmin', 'OrgAdmin', 'Manager'].includes(role);

export const canManageUsers = (role: UserRole) =>
  ['SuperAdmin', 'OrgAdmin'].includes(role);

export const canViewOrgSettings = (role: UserRole) =>
  ['SuperAdmin', 'OrgAdmin'].includes(role);

export const isAdmin = (role: UserRole) =>
  ['SuperAdmin', 'OrgAdmin'].includes(role);
