export type UserRole = 'SuperAdmin' | 'OrgAdmin' | 'Manager' | 'Employee';

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  orgId: string;
  orgName?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}
