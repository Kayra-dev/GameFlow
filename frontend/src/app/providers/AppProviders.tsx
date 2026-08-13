import { QueryClientProvider } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';
import { Toaster } from 'sonner';

import { TooltipProvider } from '@/components/ui/tooltip';
import { createQueryClient } from '@/lib/query-client';
import { useThemeStore } from '@/stores/theme-store';

/** Uygulama genelindeki sağlayıcılar tek yerde toplanır. */
export function AppProviders({ children }: { children: ReactNode }) {
  // QueryClient bir kez oluşturulur; her render'da yenilenirse önbellek sıfırlanırdı.
  const [queryClient] = useState(createQueryClient);
  const theme = useThemeStore((state) => state.theme);

  return (
    <QueryClientProvider client={queryClient}>
      <TooltipProvider delayDuration={300}>
        {children}

        <Toaster
          theme={theme}
          position="bottom-right"
          richColors
          closeButton
          toastOptions={{
            classNames: {
              toast: 'rounded-xl border-border bg-surface text-foreground shadow-float',
            },
          }}
        />
      </TooltipProvider>
    </QueryClientProvider>
  );
}
