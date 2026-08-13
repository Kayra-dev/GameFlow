import * as AvatarPrimitive from '@radix-ui/react-avatar';
import type { ComponentProps } from 'react';

import { cn, getAvatarColor, getInitials } from '@/lib/utils';

const sizeClasses = {
  xs: 'size-6 text-[10px]',
  sm: 'size-8 text-xs',
  md: 'size-10 text-sm',
  lg: 'size-14 text-base',
  xl: 'size-20 text-xl',
} as const;

type AvatarProps = ComponentProps<typeof AvatarPrimitive.Root> & {
  fullName: string;
  avatarUrl?: string | null;
  size?: keyof typeof sizeClasses;
  /** Sağ altta çevrimiçi göstergesi. */
  isOnline?: boolean;
};

/**
 * Kullanıcı avatarı. Resim yoksa ad soyaddan üretilen baş harfler ve
 * kullanıcıya özgü sabit bir renk gösterilir.
 */
export function Avatar({
  fullName,
  avatarUrl,
  size = 'md',
  isOnline,
  className,
  ...props
}: AvatarProps) {
  return (
    <div className="relative shrink-0">
      <AvatarPrimitive.Root
        className={cn(
          'flex items-center justify-center overflow-hidden rounded-full',
          'ring-1 ring-border select-none',
          sizeClasses[size],
          className,
        )}
        {...props}
      >
        {avatarUrl ? (
          <AvatarPrimitive.Image
            src={avatarUrl}
            alt={fullName}
            className="size-full object-cover"
          />
        ) : null}
        <AvatarPrimitive.Fallback
          className="flex size-full items-center justify-center font-semibold text-white"
          style={{ backgroundColor: getAvatarColor(fullName) }}
          delayMs={avatarUrl ? 400 : 0}
        >
          {getInitials(fullName)}
        </AvatarPrimitive.Fallback>
      </AvatarPrimitive.Root>

      {isOnline === undefined ? null : (
        <span
          aria-label={isOnline ? 'Çevrimiçi' : 'Çevrimdışı'}
          className={cn(
            'absolute -right-0.5 -bottom-0.5 rounded-full ring-2 ring-background',
            size === 'xs' || size === 'sm' ? 'size-2' : 'size-2.5',
            isOnline ? 'bg-success' : 'bg-subtle-foreground',
          )}
        />
      )}
    </div>
  );
}
