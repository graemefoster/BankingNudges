import type { NudgeDto } from '../types';
import { respondToNudge } from '../api/bankApi';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

interface Props {
  nudge: NudgeDto;
  onDismissed: () => void;
}

const urgencyConfig: Record<string, { border: string; bg: string; icon: string }> = {
  HIGH: { border: 'border-accent-coral/40', bg: 'bg-accent-coral/10', icon: '🔴' },
  MEDIUM: { border: 'border-accent-amber/40', bg: 'bg-accent-amber/10', icon: '🟡' },
  LOW: { border: 'border-accent-teal/40', bg: 'bg-accent-teal/10', icon: '🟢' },
};

export default function NudgeCard({ nudge, onDismissed }: Props) {
  const [responding, setResponding] = useState(false);
  const navigate = useNavigate();
  const config = urgencyConfig[nudge.urgency] ?? urgencyConfig.MEDIUM;

  const handleRespond = async (action: string) => {
    setResponding(true);
    try {
      await respondToNudge(nudge.id, action);
      if (action === 'ACCEPTED') {
        navigate(`/nudge/${nudge.id}`);
      } else {
        onDismissed();
      }
    } catch {
      setResponding(false);
    }
  };

  return (
    <div className={`rounded-xl border ${config.border} ${config.bg} p-4`}>
      <div className="flex items-start gap-3 mb-3">
        <span className="text-lg">{config.icon}</span>
        <p className="text-sm text-text-primary leading-relaxed flex-1">
          {nudge.message}
        </p>
      </div>

      <div className="flex gap-2">
        <button
          disabled={responding}
          onClick={() => handleRespond('ACCEPTED')}
          className="flex-1 text-xs font-semibold py-2 rounded-lg bg-accent-teal text-white hover:bg-accent-teal/90 transition-colors disabled:opacity-50"
        >
          {nudge.cta}
        </button>
        <button
          disabled={responding}
          onClick={() => handleRespond('SNOOZED')}
          className="text-xs font-medium py-2 px-3 rounded-lg bg-dark-surface text-text-secondary hover:text-text-primary transition-colors disabled:opacity-50"
        >
          Later
        </button>
        <button
          disabled={responding}
          onClick={() => handleRespond('DISMISSED')}
          className="text-xs font-medium py-2 px-3 rounded-lg bg-dark-surface text-text-secondary hover:text-text-primary transition-colors disabled:opacity-50"
        >
          Dismiss
        </button>
      </div>
    </div>
  );
}
