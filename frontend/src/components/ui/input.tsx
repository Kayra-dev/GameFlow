import type { ComponentProps } from 'react';

import { cn } from '@/lib/utils';

export function Input({ className, type = 'text', ...props }: ComponentProps<'input'>) {
  return (
    <input
      type={type}
      className={cn(
        'h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-foreground',
        'placeholder:text-subtle-foreground transition-colors outline-none',
        'hover:border-border-strong',
        'focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-ring',
        'disabled:cursor-not-allowed disabled:opacity-50',
        // Doğrulama hatası olan alanlar aria-invalid ile işaretlenir.
        'aria-invalid:border-danger aria-invalid:focus-visible:ring-danger/30',
        className,
      )}
      {...props}
    />
  );
}

export function Textarea({ className, ...props }: ComponentProps<'textarea'>) {
  return (
    <textarea
      className={cn(
        'min-h-24 w-full resize-y rounded-lg border border-border bg-surface px-3 py-2',
        'text-sm text-foreground placeholder:text-subtle-foreground',
        'transition-colors outline-none hover:border-border-strong',
        'focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-ring',
        'disabled:cursor-not-allowed disabled:opacity-50',
        'aria-invalid:border-danger',
        className,
      )}
      {...props}
    />
  );
}
