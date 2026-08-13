import { Loader2 } from 'lucide-react';

import { cn } from '@/lib/utils';

export function Spinner({ className }: { className?: string }) {
  return <Loader2 className={cn('size-4 animate-spin', className)} aria-hidden="true" />;
}

/** Sayfa geçişlerinde kullanılan tam alan yükleniyor göstergesi. */
export function PageLoader({ label = 'Yükleniyor…' }: { label?: string }) {
  return (
    <div className="flex min-h-64 flex-1 flex-col items-center justify-center gap-3">
      <Spinner className="size-6 text-primary" />
      <p className="text-sm text-muted-foreground">{label}</p>
    </div>
  );
}
