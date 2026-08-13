import type { ComponentProps } from 'react';

import { cn } from '@/lib/utils';

/** Veri yüklenirken düzeni bozmadan yer tutan parıltılı blok. */
export function Skeleton({ className, ...props }: ComponentProps<'div'>) {
  return (
    <div
      className={cn('skeleton-shimmer rounded-md bg-surface-raised', className)}
      aria-hidden="true"
      {...props}
    />
  );
}
