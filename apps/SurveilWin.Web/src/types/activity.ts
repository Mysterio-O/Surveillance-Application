export type AppCategory =
  | 'coding' | 'browser_work' | 'browser' | 'communication'
  | 'docs' | 'terminal' | 'media' | 'system' | 'idle' | 'other';

export interface AppDwellTime {
  app: string;
  displayName: string;
  category: AppCategory;
  seconds: number;
  percent: number;
}

export interface ActivitySummaryWindow {
  id: string;
  windowStart: string;
  windowEnd: string;
  topApps: AppDwellTime[];
  idleSeconds: number;
  activeSeconds: number;
  productivityScore: number;
}

export interface DailyReport {
  date: string;
  employee: { id: string; fullName: string };
  shift: { startedAt: string; endedAt?: string; actualHours?: number } | null;
  totals: { activeSeconds: number; idleSeconds: number; productivityScore: number };
  appBreakdown?: AppDwellTime[];
  hourlyProductivity?: { hour: number; score: number; dominant: AppCategory }[];
  jiraTickets?: string[];
  topKeywords?: string[];
  aiNarrative?: string;
  aiModelUsed?: string;
}
