export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  isActive: boolean;
  organizationId: string;
  orgName?: string;
  lastLoginAt?: string;
  createdAt: string;
}
