import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import type { ComponentProps } from 'react';

import { cn } from '@/lib/utils';

const buttonVariants = cva(
  // Ortak temel: erişilebilir odak halkası, devre dışı durumu, ikon boyutu
  'inline-flex shrink-0 items-center justify-center gap-2 rounded-lg text-sm font-medium ' +
    'whitespace-nowrap transition-all outline-none select-none ' +
    'disabled:pointer-events-none disabled:opacity-50 ' +
    'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ' +
    'focus-visible:ring-offset-background ' +
    "[&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        primary:
          'bg-primary text-primary-foreground shadow-soft hover:brightness-110 active:brightness-95',
        secondary:
          'bg-surface-raised text-foreground border border-border hover:border-border-strong ' +
          'hover:bg-surface',
        ghost: 'text-muted-foreground hover:bg-surface-raised hover:text-foreground',
        danger: 'bg-danger text-white shadow-soft hover:brightness-110',
        outline:
          'border border-border-strong bg-transparent text-foreground hover:bg-surface-raised',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        sm: 'h-8 px-3 text-xs',
        md: 'h-10 px-4',
        lg: 'h-11 px-6 text-base',
        icon: 'size-10',
        'icon-sm': 'size-8',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  },
);

type ButtonProps = ComponentProps<'button'> &
  VariantProps<typeof buttonVariants> & {
    /** Kendi etiketi yerine çocuğunu render eder (örn. <Link>). */
    asChild?: boolean;
  };

export function Button({ className, variant, size, asChild = false, ...props }: ButtonProps) {
  const Component = asChild ? Slot : 'button';

  return (
    <Component className={cn(buttonVariants({ variant, size }), className)} {...props} />
  );
}

export { buttonVariants };
