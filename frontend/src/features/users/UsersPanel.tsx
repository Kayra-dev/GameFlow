import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  KeyRound,
  MoreHorizontal,
  Pencil,
  Search,
  Trash2,
  UserPlus,
  Users as UsersIcon,
} from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { EmptyState } from '@/components/ui/empty-state';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { useDebouncedValue } from '@/hooks/use-debounced-value';
import { getErrorMessage } from '@/lib/api-client';
import { formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import type { UserSummary } from '@/types/api';
import { SystemRole, systemRoleLabels } from '@/types/enums';

import { usersApi } from './api/users-api';
import { UserFormDialog } from './components/UserFormDialog';

const roleVariant = {
  [SystemRole.Admin]: 'primary',
  [SystemRole.TeamLeader]: 'info',
  [SystemRole.TeamMember]: 'neutral',
} as const;

const PAGE_SIZE = 20;

/** Yönetim panelindeki kullanıcı yönetimi sekmesi. */
export function UsersPanel() {
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserSummary | null>(null);
  const [passwordUser, setPasswordUser] = useState<UserSummary | null>(null);
  const [deletingUser, setDeletingUser] = useState<UserSummary | null>(null);

  const debouncedSearch = useDebouncedValue(search, 300);
  const currentUserId = useAuthStore((state) => state.user?.id);

  const params = {
    page,
    pageSize: PAGE_SIZE,
    search: debouncedSearch || undefined,
    role: roleFilter === 'all' ? undefined : (Number(roleFilter) as SystemRole),
  };

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.users.list(params),
    queryFn: () => usersApi.list(params),
  });

  const openCreate = () => {
    setEditingUser(null);
    setFormOpen(true);
  };

  const openEdit = (user: UserSummary) => {
    setEditingUser(user);
    setFormOpen(true);
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1 sm:max-w-xs">
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-subtle-foreground"
            aria-hidden="true"
          />
          <Input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              // Filtre değişince ilk sayfaya dönülür, aksi halde boş sayfa görünür.
              setPage(1);
            }}
            placeholder="Ad veya e-posta ara…"
            aria-label="Kullanıcı ara"
            className="pl-9"
          />
        </div>

        <Select
          value={roleFilter}
          onValueChange={(value) => {
            setRoleFilter(value);
            setPage(1);
          }}
        >
          <SelectTrigger className="sm:w-44" aria-label="Role göre filtrele">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm roller</SelectItem>
            {Object.entries(systemRoleLabels).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Button onClick={openCreate} className="sm:ml-auto">
          <UserPlus aria-hidden="true" />
          Yeni kullanıcı
        </Button>
      </div>

      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="space-y-3 p-5">
            {Array.from({ length: 5 }, (_, index) => (
              <Skeleton key={index} className="h-12" />
            ))}
          </div>
        ) : isError ? (
          <EmptyState
            icon={UsersIcon}
            title="Kullanıcılar yüklenemedi"
            description="Sunucuya ulaşılamıyor. Sayfayı yenileyip tekrar deneyin."
          />
        ) : data && data.items.length === 0 ? (
          <EmptyState
            icon={UsersIcon}
            title={debouncedSearch ? 'Sonuç bulunamadı' : 'Henüz kullanıcı yok'}
            description={
              debouncedSearch
                ? `“${debouncedSearch}” aramasıyla eşleşen kullanıcı yok.`
                : 'İlk kullanıcıyı oluşturmak için sağ üstteki düğmeyi kullanın.'
            }
            action={
              debouncedSearch ? null : (
                <Button onClick={openCreate} variant="secondary">
                  <UserPlus aria-hidden="true" />
                  Yeni kullanıcı
                </Button>
              )
            }
          />
        ) : (
          <ul className="divide-y divide-border">
            {data?.items.map((user) => (
              <li
                key={user.id}
                className="flex items-center gap-3 px-4 py-3 transition-colors hover:bg-surface-raised/40"
              >
                <Avatar
                  fullName={user.fullName}
                  avatarUrl={user.avatarUrl}
                  size="sm"
                  isOnline={user.isOnline}
                />

                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <p className="truncate text-sm font-medium">{user.fullName}</p>
                    {user.id === currentUserId ? (
                      <Badge variant="neutral">Siz</Badge>
                    ) : null}
                  </div>
                  <p className="truncate text-xs text-muted-foreground">{user.email}</p>
                </div>

                <div className="hidden min-w-0 flex-1 sm:block">
                  <p className="truncate text-xs text-muted-foreground">
                    {user.jobTitle ?? '—'}
                  </p>
                  <p className="truncate text-[11px] text-subtle-foreground">
                    {user.isOnline ? 'Çevrimiçi' : `Son görülme ${formatRelative(user.lastSeenAt)}`}
                  </p>
                </div>

                <Badge variant={roleVariant[user.role]}>{systemRoleLabels[user.role]}</Badge>

                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      aria-label={`${user.fullName} için işlemler`}
                    >
                      <MoreHorizontal aria-hidden="true" />
                    </Button>
                  </DropdownMenuTrigger>

                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onSelect={() => openEdit(user)}>
                      <Pencil aria-hidden="true" />
                      Düzenle
                    </DropdownMenuItem>
                    <DropdownMenuItem onSelect={() => setPasswordUser(user)}>
                      <KeyRound aria-hidden="true" />
                      Şifre sıfırla
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      variant="danger"
                      // Kendi hesabını silme girişimi sunucuda da engellenir.
                      disabled={user.id === currentUserId}
                      onSelect={() => setDeletingUser(user)}
                    >
                      <Trash2 aria-hidden="true" />
                      Sil
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {data && data.totalPages > 1 ? (
        <div className="flex items-center justify-between gap-3">
          <p className="text-xs text-muted-foreground">
            {data.totalCount} kullanıcı · sayfa {data.page}/{data.totalPages}
          </p>
          <div className="flex gap-2">
            <Button
              variant="secondary"
              size="sm"
              disabled={!data.hasPrevious}
              onClick={() => setPage((previous) => previous - 1)}
            >
              Önceki
            </Button>
            <Button
              variant="secondary"
              size="sm"
              disabled={!data.hasNext}
              onClick={() => setPage((previous) => previous + 1)}
            >
              Sonraki
            </Button>
          </div>
        </div>
      ) : null}

      <UserFormDialog open={formOpen} onOpenChange={setFormOpen} user={editingUser} />

      <ResetPasswordDialog
        user={passwordUser}
        onClose={() => setPasswordUser(null)}
      />

      <DeleteUserDialog user={deletingUser} onClose={() => setDeletingUser(null)} />
    </div>
  );
}

function ResetPasswordDialog({
  user,
  onClose,
}: {
  user: UserSummary | null;
  onClose: () => void;
}) {
  const [newPassword, setNewPassword] = useState('');
  const [mustChange, setMustChange] = useState(true);

  const reset = useMutation({
    mutationFn: () =>
      usersApi.resetPassword(user!.id, { newPassword, mustChangePassword: mustChange }),
    onSuccess: () => {
      toast.success(`${user?.fullName} kullanıcısının şifresi sıfırlandı.`);
      setNewPassword('');
      onClose();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog
      open={Boolean(user)}
      onOpenChange={(open) => {
        if (!open) {
          setNewPassword('');
          onClose();
        }
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Şifre sıfırla</DialogTitle>
          <DialogDescription>
            {user?.fullName} için yeni bir geçici şifre belirleyin. Kullanıcının açık
            oturumları kapatılacak.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="new-password">Yeni şifre</Label>
            <Input
              id="new-password"
              type="text"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              placeholder="Gelistirici1"
              autoComplete="new-password"
            />
            <p className="text-xs text-subtle-foreground">
              En az 8 karakter, bir büyük harf, bir küçük harf ve bir rakam.
            </p>
          </div>

          <label className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border border-border p-3">
            <span className="text-sm">İlk girişte şifre değiştirsin</span>
            <Switch checked={mustChange} onCheckedChange={setMustChange} />
          </label>
        </div>

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Vazgeç
          </Button>
          <Button
            onClick={() => reset.mutate()}
            disabled={reset.isPending || newPassword.length < 8}
          >
            Şifreyi sıfırla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function DeleteUserDialog({ user, onClose }: { user: UserSummary | null; onClose: () => void }) {
  const queryClient = useQueryClient();

  const remove = useMutation({
    mutationFn: () => usersApi.remove(user!.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.teams.all });

      toast.success(`${user?.fullName} silindi.`);
      onClose();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog open={Boolean(user)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Kullanıcıyı sil</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{user?.fullName}</strong> silinecek. Kullanıcı
            giriş yapamayacak ve takım/proje üyelikleri kaldırılacak. Yorumları, görev geçmişi
            ve denetim kayıtları korunur.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Vazgeç
          </Button>
          <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
            <Trash2 aria-hidden="true" />
            Sil
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
