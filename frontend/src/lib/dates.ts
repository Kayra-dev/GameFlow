import { differenceInCalendarDays, format, formatDistanceToNow, isToday, isTomorrow } from 'date-fns';
import { tr } from 'date-fns/locale';

/** Sunucudan gelen ISO tarihini Date'e çevirir. */
export function parseDate(value: string | null | undefined): Date | null {
  if (!value) return null;

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function formatDate(value: string | null | undefined): string {
  const date = parseDate(value);
  return date ? format(date, 'd MMMM yyyy', { locale: tr }) : '—';
}

export function formatDateTime(value: string | null | undefined): string {
  const date = parseDate(value);
  return date ? format(date, 'd MMM yyyy HH:mm', { locale: tr }) : '—';
}

export function formatTime(value: string | null | undefined): string {
  const date = parseDate(value);
  return date ? format(date, 'HH:mm', { locale: tr }) : '—';
}

/** "3 saat önce" biçiminde göreli zaman. */
export function formatRelative(value: string | null | undefined): string {
  const date = parseDate(value);
  return date ? formatDistanceToNow(date, { addSuffix: true, locale: tr }) : '—';
}

/** Son teslim tarihini insan diline çevirir: "Bugün", "Yarın", "3 gün gecikmiş". */
export function formatDueDate(value: string | null | undefined): string {
  const date = parseDate(value);

  if (!date) return 'Tarih yok';
  if (isToday(date)) return 'Bugün';
  if (isTomorrow(date)) return 'Yarın';

  const days = differenceInCalendarDays(date, new Date());

  if (days < 0) {
    return `${Math.abs(days)} gün gecikmiş`;
  }

  if (days <= 7) {
    return `${days} gün kaldı`;
  }

  return format(date, 'd MMM', { locale: tr });
}

export type DeadlineTone = 'overdue' | 'urgent' | 'soon' | 'normal' | 'none';

/**
 * Deadline'ın renk tonunu belirler.
 * Kırmızı: gecikmiş · Turuncu: 2 gün içinde · Yeşil: ileride.
 */
export function getDeadlineTone(
  dueDate: string | null | undefined,
  isCompleted = false,
): DeadlineTone {
  const date = parseDate(dueDate);

  if (!date) return 'none';
  if (isCompleted) return 'normal';

  const days = differenceInCalendarDays(date, new Date());

  if (days < 0) return 'overdue';
  if (days === 0) return 'urgent';
  if (days <= 2) return 'soon';

  return 'normal';
}

export const deadlineToneClasses: Record<DeadlineTone, string> = {
  overdue: 'text-danger',
  urgent: 'text-danger',
  soon: 'text-warning',
  normal: 'text-success',
  none: 'text-subtle-foreground',
};

/** Takvim ay adları (Türkçe). */
export function formatMonthYear(date: Date): string {
  return format(date, 'MMMM yyyy', { locale: tr });
}
