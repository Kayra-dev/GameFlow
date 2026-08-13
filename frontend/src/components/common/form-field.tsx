import type { ReactNode } from 'react';

import { Label } from '@/components/ui/label';

/**
 * `datetime-local` girdisi yerel saat bekler ve saat dilimi taşımaz.
 * `toISOString()` doğrudan kullanılamaz; UTC'ye kaydırıp saati bozar.
 */
export function toLocalInputValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');

  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}`
  );
}

/** Boş girdiyi null'a çevirir; dolu girdiyi UTC ISO'ya. */
export function toIsoOrNull(value: string): string | null {
  return value ? new Date(value).toISOString() : null;
}

/** Etiket + alan + hata/ipucu üçlüsü. */
export function Field({
  label,
  htmlFor,
  error,
  hint,
  required,
  children,
}: {
  label: string;
  htmlFor?: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={htmlFor}>
        {label}
        {required ? <span className="ml-0.5 text-danger">*</span> : null}
      </Label>
      {children}
      {error ? (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      ) : hint ? (
        <p className="text-xs text-subtle-foreground">{hint}</p>
      ) : null}
    </div>
  );
}
