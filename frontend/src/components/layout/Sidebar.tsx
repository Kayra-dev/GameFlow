import { Gamepad2, X } from 'lucide-react';
import { NavLink } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';

import { filterNavigation, navigationGroups } from './navigation';

type SidebarProps = {
  /** Mobilde açılır panel olarak davranır. */
  isOpen: boolean;
  onClose: () => void;
};

export function Sidebar({ isOpen, onClose }: SidebarProps) {
  const role = useAuthStore((state) => state.user?.role);
  const groups = filterNavigation(navigationGroups, role);

  return (
    <>
      {/* Mobil arkaplan örtüsü */}
      {isOpen ? (
        <button
          type="button"
          aria-label="Menüyü kapat"
          onClick={onClose}
          className="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm lg:hidden"
        />
      ) : null}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex w-64 flex-col border-r border-border bg-surface',
          'transition-transform duration-300 ease-out-quint',
          // Masaüstünde her zaman görünür, mobilde kaydırılarak açılır.
          'lg:static lg:z-auto lg:translate-x-0',
          isOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex h-16 shrink-0 items-center justify-between gap-2 border-b border-border px-4">
          <NavLink to="/" className="flex items-center gap-2.5" onClick={onClose}>
            <div className="grid size-8 place-items-center rounded-xl bg-primary">
              <Gamepad2 className="size-4 text-primary-foreground" aria-hidden="true" />
            </div>
            <span className="text-base font-semibold tracking-tight">GameFlow</span>
          </NavLink>

          <Button
            variant="ghost"
            size="icon-sm"
            onClick={onClose}
            aria-label="Menüyü kapat"
            className="lg:hidden"
          >
            <X aria-hidden="true" />
          </Button>
        </div>

        <nav className="flex-1 overflow-y-auto p-3" aria-label="Ana menü">
          {groups.map((group, index) => (
            <div key={group.label ?? `group-${index}`} className={index > 0 ? 'mt-5' : undefined}>
              {group.label ? (
                <p className="px-3 pb-2 text-[11px] font-medium tracking-wider text-subtle-foreground uppercase">
                  {group.label}
                </p>
              ) : null}

              <ul className="space-y-0.5">
                {group.items.map((item) => (
                  <li key={item.to}>
                    <NavLink
                      to={item.to}
                      end={item.to === '/'}
                      onClick={onClose}
                      className={({ isActive }) =>
                        cn(
                          'group flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium',
                          'transition-colors outline-none',
                          'focus-visible:ring-2 focus-visible:ring-ring',
                          isActive
                            ? 'bg-primary/12 text-primary'
                            : 'text-muted-foreground hover:bg-surface-raised hover:text-foreground',
                        )
                      }
                    >
                      {({ isActive }) => (
                        <>
                          <item.icon
                            className={cn(
                              'size-4 shrink-0 transition-colors',
                              isActive ? 'text-primary' : 'text-subtle-foreground',
                              'group-hover:text-foreground',
                            )}
                            aria-hidden="true"
                          />
                          {item.label}
                        </>
                      )}
                    </NavLink>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>

        <div className="shrink-0 border-t border-border p-3">
          <p className="px-3 text-[11px] text-subtle-foreground">GameFlow · sürüm 1.0</p>
        </div>
      </aside>
    </>
  );
}
