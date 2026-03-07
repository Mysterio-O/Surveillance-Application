import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../hooks/useAuth';
import { getOrgPolicy, updateOrgPolicy } from '../../api/reports';
import toast from 'react-hot-toast';

export default function OrgSettingsPage() {
  const { user } = useAuth();
  const qc = useQueryClient();
  const [tab, setTab] = useState<'monitoring' | 'shift' | 'ai' | 'retention'>('monitoring');

  const { data: policy } = useQuery({
    queryKey: ['org-policy', user?.orgId],
    queryFn: () => getOrgPolicy(user!.orgId).then(r => r.data),
    enabled: !!user?.orgId,
  });

  const mutation = useMutation({
    mutationFn: (data: Record<string, unknown>) => updateOrgPolicy(user!.orgId, data).then(r => r.data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['org-policy'] }); toast.success('Settings saved'); },
    onError: () => toast.error('Failed to save'),
  });

  const tabs = [
    { id: 'monitoring', label: 'Monitoring' },
    { id: 'shift', label: 'Shift Policy' },
    { id: 'ai', label: 'AI Summaries' },
    { id: 'retention', label: 'Retention' },
  ] as const;

  const InputRow = ({ label, field, type = 'text' }: { label: string; field: string; type?: string }) => (
    <div className="flex items-center justify-between py-3 border-b border-surface0/50">
      <label className="text-sm text-text">{label}</label>
      <input type={type} defaultValue={policy?.[field] as string}
        onBlur={e => mutation.mutate({ [field]: type === 'number' ? +e.target.value : e.target.value })}
        className="bg-surface0 border border-surface1 text-text rounded-lg px-3 py-1.5 text-sm w-40 focus:outline-none focus:border-blue" />
    </div>
  );

  const ToggleRow = ({ label, field }: { label: string; field: string }) => (
    <div className="flex items-center justify-between py-3 border-b border-surface0/50">
      <label className="text-sm text-text">{label}</label>
      <button onClick={() => mutation.mutate({ [field]: !policy?.[field] })}
        className={`w-10 h-5 rounded-full transition-colors ${policy?.[field] ? 'bg-blue' : 'bg-surface1'}`}>
        <div className={`w-4 h-4 bg-white rounded-full transition-transform mx-0.5 ${policy?.[field] ? 'translate-x-5' : 'translate-x-0'}`} />
      </button>
    </div>
  );

  return (
    <div className="space-y-6 max-w-2xl">
      <h2 className="text-xl font-semibold text-text">Organization Settings</h2>

      <div className="flex gap-2 border-b border-surface0">
        {tabs.map(t => (
          <button key={t.id} onClick={() => setTab(t.id)}
            className={`px-4 py-2 text-sm transition-colors border-b-2 -mb-px ${tab === t.id ? 'border-blue text-blue' : 'border-transparent text-subtext0 hover:text-text'}`}>
            {t.label}
          </button>
        ))}
      </div>

      {policy && (
        <div className="bg-mantle border border-surface0 rounded-xl p-5">
          {tab === 'monitoring' && (
            <>
              <InputRow label="Capture FPS" field="captureFps" type="number" />
              <ToggleRow label="Enable OCR" field="enableOcr" />
              <ToggleRow label="Enable Screenshots" field="enableScreenshots" />
              <InputRow label="Screenshot Interval (min)" field="screenshotIntervalMinutes" type="number" />
            </>
          )}
          {tab === 'shift' && (
            <>
              <InputRow label="Expected Shift Hours" field="expectedShiftHours" type="number" />
              <InputRow label="Auto-close after (hours)" field="autoCloseShiftAfterHours" type="number" />
            </>
          )}
          {tab === 'ai' && (
            <>
              <ToggleRow label="Enable AI Summaries" field="enableAiSummaries" />
              <InputRow label="AI Provider" field="aiProvider" />
            </>
          )}
          {tab === 'retention' && (
            <>
              <InputRow label="Screenshot Retention (days)" field="screenshotRetentionDays" type="number" />
              <InputRow label="Summary Retention (days)" field="summaryRetentionDays" type="number" />
            </>
          )}
        </div>
      )}
    </div>
  );
}
