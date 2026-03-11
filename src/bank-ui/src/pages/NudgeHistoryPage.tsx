import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { NudgeHistoryItem } from '../types';
import { getNudgeHistory } from '../api/bankApi';

function relativeTime(iso: string): string {
  const now = Date.now();
  const then = new Date(iso).getTime();
  const diffMs = now - then;
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'Just now';
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay === 1) return 'Yesterday';
  if (diffDay < 7) return `${diffDay}d ago`;
  return new Date(iso).toLocaleDateString('en-AU', { day: 'numeric', month: 'short' });
}

const categoryEmoji: Record<string, string> = {
  SPENDING: '💳',
  CASHFLOW: '💰',
  UPCOMING_PAYMENT: '📅',
  SAVINGS: '🏦',
};

const urgencyColor: Record<string, string> = {
  HIGH: 'bg-accent-coral',
  MEDIUM: 'bg-accent-amber',
  LOW: 'bg-accent-teal',
};

const statusBadge: Record<string, { cls: string; label: string }> = {
  ACCEPTED: { cls: 'bg-accent-teal/15 text-accent-teal', label: 'Accepted' },
  DISMISSED: { cls: 'bg-dark-surface text-text-muted', label: 'Dismissed' },
  SNOOZED: { cls: 'bg-accent-amber/15 text-accent-amber', label: 'Snoozed' },
  EXPIRED: { cls: 'bg-dark-surface text-text-muted', label: 'Expired' },
  PENDING: { cls: 'bg-accent-cyan/15 text-accent-cyan', label: 'Pending' },
  SENT: { cls: 'bg-accent-cyan/15 text-accent-cyan', label: 'Pending' },
};

function dateGroup(iso: string): string {
  const d = new Date(iso);
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);
  const itemDate = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  if (itemDate.getTime() === today.getTime()) return 'Today';
  if (itemDate.getTime() === yesterday.getTime()) return 'Yesterday';
  return 'Older';
}

export default function NudgeHistoryPage() {
  const navigate = useNavigate();
  const [nudges, setNudges] = useState<NudgeHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const customerId = sessionStorage.getItem('customerId');

  useEffect(() => {
    if (!customerId) {
      navigate('/', { replace: true });
      return;
    }
    loadNudges(customerId);
  }, [customerId, navigate]);

  function loadNudges(cid: string) {
    setLoading(true);
    setError('');
    getNudgeHistory(cid)
      .then(setNudges)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }

  if (loading) {
    return (
      <div className="flex justify-center py-16">
        <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-16">
        <p className="text-accent-coral mb-4">{error}</p>
        <button
          onClick={() => customerId && loadNudges(customerId)}
          className="text-accent-teal hover:underline text-sm font-medium"
        >
          Retry
        </button>
      </div>
    );
  }

  if (nudges.length === 0) {
    return (
      <div className="text-center py-16">
        <div className="text-5xl mb-4">💡</div>
        <p className="text-text-secondary font-medium mb-1">No insights yet</p>
        <p className="text-text-muted text-sm">Check back after your next financial check-in</p>
      </div>
    );
  }

  // Group by date
  const groups: { label: string; items: NudgeHistoryItem[] }[] = [];
  const order = ['Today', 'Yesterday', 'Older'];
  const map = new Map<string, NudgeHistoryItem[]>();
  for (const n of nudges) {
    const g = dateGroup(n.createdAt);
    if (!map.has(g)) map.set(g, []);
    map.get(g)!.push(n);
  }
  for (const label of order) {
    const items = map.get(label);
    if (items && items.length > 0) groups.push({ label, items });
  }

  return (
    <div>
      <h2 className="text-lg font-bold text-text-primary mb-4">Your Insights</h2>

      {groups.map((group) => (
        <div key={group.label}>
          <p className="text-xs font-semibold text-text-secondary uppercase tracking-wide py-2 mt-2">
            {group.label}
          </p>
          <div className="space-y-3">
            {group.items.map((nudge) => {
              const badge = statusBadge[nudge.status] ?? statusBadge.PENDING;
              return (
                <div
                  key={nudge.id}
                  onClick={() => navigate(`/nudge/${nudge.id}`)}
                  className="bg-dark-elevated rounded-xl border border-border p-4 cursor-pointer hover:border-accent-teal/30 transition-colors flex items-start gap-3"
                >
                  {/* Category emoji */}
                  <span className="text-2xl leading-none mt-0.5">
                    {categoryEmoji[nudge.category] ?? '💡'}
                  </span>

                  {/* Center content */}
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-text-primary line-clamp-2">{nudge.message}</p>
                    <div className="flex items-center gap-2 mt-1.5">
                      <span className="text-xs text-text-muted">{relativeTime(nudge.createdAt)}</span>
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${badge.cls}`}>
                        {badge.label}
                      </span>
                    </div>
                  </div>

                  {/* Urgency dot */}
                  <span
                    className={`w-2.5 h-2.5 rounded-full mt-1.5 shrink-0 ${urgencyColor[nudge.urgency] ?? 'bg-accent-teal'}`}
                  />
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
