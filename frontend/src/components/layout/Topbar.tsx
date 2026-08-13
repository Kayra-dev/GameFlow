import { Bell, LogOut, Menu, Search, User as UserIcon } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Tooltip } from '@/components/ui/tooltip';
import { useLogout } from '@/features/auth/use-auth';
import { useAuthStore } from '@/stores/auth-store';
import { systemRoleLabels } from '@/types/enums';

import { ThemeToggle } from './ThemeToggle';

type TopbarProps = {
  onOpenSidebar: () => void;
  onOpenSearch: () => void;
  unreadNotificationCount: number;
};

export function Topbar({ onOpenSidebar, onOpenSearch, unreadNotificationCount }: TopbarProps) {
  const user = useAuthStore((state) => state.user);
  const logout = useLogout();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/giris', { replace: true });
  };

  return (
    <header className="sticky top-0 z-30 flex h-16 shrink-0 items-center gap-2 border-b border-border bg-surface-overlay px-4 backdrop-blur-xl">
      <Button
        variant="ghost"
        size="icon"
        onClick={onOpenSidebar}
        aria-label="Menüyü aç"
        className="lg:hidden"
      >
        <Menu aria-hidden="true" />
      </Button>

      {/* Arama: masaüstünde geniş bir düğme, mobilde ikon */}
      <button
        type="button"
        onClick={onOpenSearch}
        className="hidden h-9 max-w-sm flex-1 items-center gap-2 rounded-lg border border-border bg-background px-3 text-sm text-subtle-foreground transition-colors hover:border-border-strong hover:text-muted-foreground sm:flex"
      >
        <Search className="size-4" aria-hidden="true" />
        <span>Görev, proje, kişi ara…</span>
        <kbd className="ml-auto rounded border border-border px-1.5 py-0.5 font-mono text-[10px] text-subtle-foreground">
          ⌘K
        </kbd>
      </button>

      <Button
        variant="ghost"
        size="icon"
        onClick={onOpenSearch}
        aria-label="Ara"
        className="sm:hidden"
      >
        <Search aria-hidden="true" />
      </Button>

      <div className="ml-auto flex items-center gap-1">
        <Tooltip content="Bildirimler">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => navigate('/bildirimler')}
            aria-label={
              unreadNotificationCount > 0
                ? `Bildirimler, ${unreadNotificationCount} okunmamış`
                : 'Bildirimler'
            }
            className="relative"
          >
            <Bell aria-hidden="true" />
            {unreadNotificationCount > 0 ? (
              <span className="absolute top-1.5 right-1.5 grid min-w-4 place-items-center rounded-full bg-danger px-1 text-[10px] font-semibold text-white">
                {unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}
              </span>
            ) : null}
          </Button>
        </Tooltip>

        <ThemeToggle />

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              aria-label="Hesap menüsü"
              className="ml-1 rounded-full outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <Avatar
                fullName={user?.fullName ?? '?'}
                avatarUrl={user?.avatarUrl}
                size="sm"
              />
            </button>
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end">
            <DropdownMenuLabel>
              <span className="block truncate text-sm font-medium text-foreground">
                {user?.fullName}
              </span>
              <span className="block truncate text-xs text-muted-foreground">{user?.email}</span>
              {user ? (
                <span className="mt-1 block text-xs text-primary">
                  {systemRoleLabels[user.role]}
                </span>
              ) : null}
            </DropdownMenuLabel>

            <DropdownMenuSeparator />

            <DropdownMenuItem onSelect={() => navigate('/profil')}>
              <UserIcon aria-hidden="true" />
              Profilim
            </DropdownMenuItem>

            <DropdownMenuSeparator />

            <DropdownMenuItem variant="danger" onSelect={handleLogout}>
              <LogOut aria-hidden="true" />
              Çıkış yap
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
