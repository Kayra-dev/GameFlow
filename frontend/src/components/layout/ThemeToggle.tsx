import { Moon, Sun } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Tooltip } from '@/components/ui/tooltip';
import { useThemeStore } from '@/stores/theme-store';

export function ThemeToggle() {
  const { theme, toggleTheme } = useThemeStore();
  const nextLabel = theme === 'dark' ? 'Açık temaya geç' : 'Koyu temaya geç';

  return (
    <Tooltip content={nextLabel}>
      <Button variant="ghost" size="icon" onClick={toggleTheme} aria-label={nextLabel}>
        {theme === 'dark' ? (
          <Sun className="size-4" aria-hidden="true" />
        ) : (
          <Moon className="size-4" aria-hidden="true" />
        )}
      </Button>
    </Tooltip>
  );
}
