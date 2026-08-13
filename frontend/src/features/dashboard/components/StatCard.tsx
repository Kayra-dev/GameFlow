import type { LucideIcon } from 'lucide-react';

import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';

type Tone = 'primary' | 'success' | 'warning' | 'danger';

const toneClasses: Record<Tone, { icon: string; ring: string }> = {
  primary: { icon: 'text-primary', ring: 'bg-primary/12' },
  success: { icon: 'text-success', ring: 'bg-success/12' },
  warning: { icon: 'text-warning', ring: 'bg-warning/12' },
  danger: { icon: 'text-danger', ring: 'bg-danger/12' },
};

type StatCardProps = {
  icon: LucideIcon;
  label: string;
  value: number | string;
  tone?: Tone;
  hint?: string;
};

/** Dashboard'daki sayısal özet kartı. */
export function StatCard({ icon: Icon, label, value, tone = 'primary', hint }: StatCardProps) {
  const classes = toneClasses[tone];

  return (
    <Card className="h-full">
      <CardContent className="flex items-center gap-4 pt-5">
        <div className={cn('grid size-11 shrink-0 place-items-center rounded-xl', classes.ring)}>
          <Icon className={cn('size-5', classes.icon)} aria-hidden="true" />
        </div>
        <div className="min-w-0">
          <p className="truncate text-xs font-medium tracking-wide text-muted-foreground uppercase">
            {label}
          </p>
          <p className="mt-0.5 text-2xl font-semibold tabular-nums">{value}</p>
          {hint ? <p className="text-xs text-subtle-foreground">{hint}</p> : null}
        </div>
      </CardContent>
    </Card>
  );
}
