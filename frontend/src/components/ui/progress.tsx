import { cn } from '@/lib/utils';

type ProgressProps = {
  /** 0-100 arası yüzde. */
  value: number;
  className?: string;
  /** Çubuk rengini dışarıdan verir (örn. takım rengi). */
  color?: string;
  label?: string;
};

export function Progress({ value, className, color, label }: ProgressProps) {
  const clamped = Math.min(100, Math.max(0, Math.round(value)));

  return (
    <div
      role="progressbar"
      aria-valuenow={clamped}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={label ?? `İlerleme: %${clamped}`}
      className={cn('h-1.5 w-full overflow-hidden rounded-full bg-surface-raised', className)}
    >
      <div
        className="h-full rounded-full transition-[width] duration-500 ease-out-quint"
        style={{
          width: `${clamped}%`,
          backgroundColor: color ?? 'var(--primary)',
        }}
      />
    </div>
  );
}
