export interface Shift {
  id: string;
  employeeId: string;
  employeeName?: string;
  date: string;
  startedAt: string;
  endedAt?: string;
  expectedHours: number;
  actualHours?: number;
  status: 'Active' | 'Completed' | 'AutoClosed';
  agentVersion?: string;
}
