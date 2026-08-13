import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Koşullu Tailwind sınıflarını birleştirir ve çakışan yardımcı sınıfları
 * (örn. iki farklı `p-*`) sonuncusu kazanacak şekilde sadeleştirir.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

/** Dosya boyutunu okunabilir metne çevirir. */
export function formatFileSize(bytes: number): string {
  if (bytes <= 0) return '0 B';

  const units = ['B', 'KB', 'MB', 'GB'];
  const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  const value = bytes / 1024 ** exponent;

  return `${value.toFixed(exponent === 0 ? 0 : 1)} ${units[exponent]}`;
}

/** Ad soyaddan iki harflik avatar kısaltması üretir. */
export function getInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0]!.slice(0, 2).toLocaleUpperCase('tr-TR');

  return (parts[0]![0]! + parts[parts.length - 1]![0]!).toLocaleUpperCase('tr-TR');
}

/**
 * Kullanıcı adından tutarlı bir renk üretir; aynı kullanıcı her yerde
 * aynı avatar rengini alır.
 */
export function getAvatarColor(seed: string): string {
  const palette = [
    'oklch(0.63 0.2 285)',
    'oklch(0.66 0.18 200)',
    'oklch(0.7 0.17 155)',
    'oklch(0.75 0.16 75)',
    'oklch(0.66 0.2 25)',
    'oklch(0.64 0.19 330)',
    'oklch(0.68 0.15 250)',
  ];

  let hash = 0;
  for (let index = 0; index < seed.length; index += 1) {
    hash = (hash * 31 + seed.charCodeAt(index)) % 100_000;
  }

  return palette[hash % palette.length]!;
}
