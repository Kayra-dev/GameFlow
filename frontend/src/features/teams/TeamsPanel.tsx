import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Crown, Loader2, MoreHorizontal, Pencil, Trash2, UserPlus, Users } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
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
import { Input, Textarea } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { usersApi } from '@/features/users/api/users-api';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import type { TeamSummary } from '@/types/api';
import { TeamCategory, teamCategoryLabels } from '@/types/enums';

import { teamsApi } from './api/teams-api';

/** Takım rengi seçenekleri; elle hex girmek yerine tutarlı bir palet sunulur. */
const teamColors = [
  '#6366F1',
  '#8B5CF6',
  '#EC4899',
  '#EF4444',
  '#F97316',
  '#F59E0B',
  '#22C55E',
  '#06B6D4',
  '#3B82F6',
  '#64748B',
];

const teamSchema = z.object({
  name: z
    .string()
    .min(2, 'Takım adı en az 2 karakter olmalıdır.')
    .max(96, 'Takım adı en fazla 96 karakter olabilir.'),
  description: z.string().max(1024, 'Açıklama en fazla 1024 karakter olabilir.').optional(),
  category: z.number().int(),
  colorHex: z.string().regex(/^#[0-9a-fA-F]{6}$/, 'Renk seçin.'),
  leaderId: z.string().optional(),
});

type TeamFormValues = z.infer<typeof teamSchema>;

export function TeamsPanel() {
  const [formOpen, setFormOpen] = useState(false);
  const [editingTeam, setEditingTeam] = useState<TeamSummary | null>(null);
  const [deletingTeam, setDeletingTeam] = useState<TeamSummary | null>(null);
  const [membersTeam, setMembersTeam] = useState<TeamSummary | null>(null);

  const { data: teams, isLoading, isError } = useQuery({
    queryKey: queryKeys.teams.list({}),
    queryFn: () => teamsApi.list(),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">
          {teams?.length ?? 0} takım · stüdyo genelinde tanımlıdır
        </p>
        <Button
          onClick={() => {
            setEditingTeam(null);
            setFormOpen(true);
          }}
        >
          <Users aria-hidden="true" />
          Yeni takım
        </Button>
      </div>

      {isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-40 rounded-card" />
          ))}
        </div>
      ) : isError ? (
        <Card>
          <EmptyState
            icon={Users}
            title="Takımlar yüklenemedi"
            description="Sunucuya ulaşılamıyor. Sayfayı yenileyip tekrar deneyin."
          />
        </Card>
      ) : teams && teams.length === 0 ? (
        <Card>
          <EmptyState
            icon={Users}
            title="Henüz takım yok"
            description="Yazılım, Tasarım, Ses gibi departmanlarınızı takım olarak tanımlayın."
            action={
              <Button
                variant="secondary"
                onClick={() => {
                  setEditingTeam(null);
                  setFormOpen(true);
                }}
              >
                <Users aria-hidden="true" />
                İlk takımı oluştur
              </Button>
            }
          />
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {teams?.map((team) => (
            <TeamCard
              key={team.id}
              team={team}
              onEdit={() => {
                setEditingTeam(team);
                setFormOpen(true);
              }}
              onDelete={() => setDeletingTeam(team)}
              onManageMembers={() => setMembersTeam(team)}
            />
          ))}
        </div>
      )}

      <TeamFormDialog open={formOpen} onOpenChange={setFormOpen} team={editingTeam} />
      <DeleteTeamDialog team={deletingTeam} onClose={() => setDeletingTeam(null)} />
      <ManageMembersDialog team={membersTeam} onClose={() => setMembersTeam(null)} />
    </div>
  );
}

function TeamCard({
  team,
  onEdit,
  onDelete,
  onManageMembers,
}: {
  team: TeamSummary;
  onEdit: () => void;
  onDelete: () => void;
  onManageMembers: () => void;
}) {
  // Kart üzerindeki ilerleme ve üye listesi ayrıntı sorgusundan gelir.
  const { data: detail } = useQuery({
    queryKey: queryKeys.teams.detail(team.id),
    queryFn: () => teamsApi.detail(team.id),
  });

  return (
    <Card className="flex h-full flex-col">
      <CardContent className="flex flex-1 flex-col gap-3 pt-5">
        <div className="flex items-start gap-3">
          <div
            className="mt-0.5 size-9 shrink-0 rounded-xl"
            style={{ backgroundColor: `${team.colorHex}22`, border: `1px solid ${team.colorHex}55` }}
            aria-hidden="true"
          >
            <div
              className="m-2 size-5 rounded-md"
              style={{ backgroundColor: team.colorHex }}
            />
          </div>

          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold">{team.name}</p>
            <p className="text-xs text-muted-foreground">
              {teamCategoryLabels[team.category]} · {team.memberCount} üye
            </p>
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon-sm" aria-label={`${team.name} işlemleri`}>
                <MoreHorizontal aria-hidden="true" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onSelect={onEdit}>
                <Pencil aria-hidden="true" />
                Düzenle
              </DropdownMenuItem>
              <DropdownMenuItem onSelect={onManageMembers}>
                <UserPlus aria-hidden="true" />
                Üyeleri yönet
              </DropdownMenuItem>
              <DropdownMenuItem variant="danger" onSelect={onDelete}>
                <Trash2 aria-hidden="true" />
                Sil
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        {team.leader ? (
          <div className="flex items-center gap-2 rounded-lg bg-surface-raised px-2.5 py-2">
            <Crown className="size-3.5 shrink-0 text-warning" aria-hidden="true" />
            <Avatar fullName={team.leader.fullName} avatarUrl={team.leader.avatarUrl} size="xs" />
            <span className="truncate text-xs">{team.leader.fullName}</span>
          </div>
        ) : (
          <p className="rounded-lg border border-dashed border-border px-2.5 py-2 text-xs text-subtle-foreground">
            Lider atanmamış
          </p>
        )}

        <div className="mt-auto space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="text-muted-foreground">İlerleme</span>
            <span className="tabular-nums">%{detail?.progressPercent ?? 0}</span>
          </div>
          <Progress value={detail?.progressPercent ?? 0} color={team.colorHex} />

          <div className="flex gap-3 text-xs text-muted-foreground">
            <span>{detail?.activeTaskCount ?? 0} aktif</span>
            {detail && detail.overdueTaskCount > 0 ? (
              <span className="text-danger">{detail.overdueTaskCount} geciken</span>
            ) : null}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function TeamFormDialog({
  open,
  onOpenChange,
  team,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  team: TeamSummary | null;
}) {
  const queryClient = useQueryClient();
  const isEditMode = Boolean(team);

  const { data: users = [] } = useQuery({
    queryKey: queryKeys.users.assignable,
    queryFn: usersApi.assignable,
    enabled: open,
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<TeamFormValues>({
    resolver: zodResolver(teamSchema),
    // Anahtar (key) prop'u ile bileşen yeniden kurulduğu için varsayılanlar
    // her açılışta doğru değerlerle başlar.
    defaultValues: {
      name: team?.name ?? '',
      description: '',
      category: team?.category ?? TeamCategory.Software,
      colorHex: team?.colorHex ?? teamColors[0],
      leaderId: team?.leader?.id ?? 'none',
    },
  });

  const save = useMutation({
    mutationFn: async (values: TeamFormValues) => {
      const payload = {
        name: values.name,
        description: values.description || null,
        category: values.category as TeamCategory,
        colorHex: values.colorHex,
        iconKey: null,
      };

      if (isEditMode && team) {
        return teamsApi.update(team.id, payload);
      }

      const created = await teamsApi.create({ ...payload, memberIds: [] });

      // Lider ataması ayrı bir uç noktadır; oluşturmadan sonra uygulanır.
      if (values.leaderId && values.leaderId !== 'none') {
        return teamsApi.assignLeader(created.id, values.leaderId);
      }

      return created;
    },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.teams.all });
      // Lider ataması kullanıcının sistem rolünü yükseltebilir.
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.all });

      toast.success(isEditMode ? `${saved.name} güncellendi.` : `${saved.name} oluşturuldu.`);
      reset();
      onOpenChange(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const selectedColor = watch('colorHex');

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent key={team?.id ?? 'new'}>
        <DialogHeader>
          <DialogTitle>{isEditMode ? 'Takımı düzenle' : 'Yeni takım'}</DialogTitle>
          <DialogDescription>
            Takımlar stüdyo genelindedir ve birden fazla projede görev alabilir.
            Her takım için otomatik olarak bir sohbet odası açılır.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => save.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <div className="space-y-1.5">
            <Label htmlFor="team-name">
              Takım adı<span className="ml-0.5 text-danger">*</span>
            </Label>
            <Input
              id="team-name"
              autoFocus
              placeholder="Yazılım"
              aria-invalid={Boolean(errors.name)}
              {...register('name')}
            />
            {errors.name ? (
              <p role="alert" className="text-xs text-danger">
                {errors.name.message}
              </p>
            ) : null}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="team-category">Departman</Label>
            <Select
              value={String(watch('category'))}
              onValueChange={(value) => setValue('category', Number(value))}
            >
              <SelectTrigger id="team-category">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(teamCategoryLabels).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>Renk</Label>
            <div className="flex flex-wrap gap-2">
              {teamColors.map((color) => (
                <button
                  key={color}
                  type="button"
                  onClick={() => setValue('colorHex', color)}
                  aria-label={`Renk ${color}`}
                  aria-pressed={selectedColor === color}
                  className="size-7 rounded-lg outline-none transition-transform hover:scale-110 focus-visible:ring-2 focus-visible:ring-ring"
                  style={{
                    backgroundColor: color,
                    boxShadow:
                      selectedColor === color ? '0 0 0 2px var(--background), 0 0 0 4px ' + color : undefined,
                  }}
                />
              ))}
            </div>
          </div>

          {!isEditMode ? (
            <div className="space-y-1.5">
              <Label htmlFor="team-leader">Takım lideri</Label>
              <Select
                value={watch('leaderId') ?? 'none'}
                onValueChange={(value) => setValue('leaderId', value)}
              >
                <SelectTrigger id="team-leader">
                  <SelectValue placeholder="Seçilmedi" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Sonra atanacak</SelectItem>
                  {users.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-subtle-foreground">
                Lider yapılan kullanıcının sistem rolü otomatik olarak “Takım Lideri”ne yükseltilir.
              </p>
            </div>
          ) : null}

          <div className="space-y-1.5">
            <Label htmlFor="team-description">Açıklama</Label>
            <Textarea
              id="team-description"
              rows={2}
              placeholder="Motor, oynanış ve araç geliştirme."
              {...register('description')}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              {isEditMode ? 'Kaydet' : 'Takımı oluştur'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ManageMembersDialog({
  team,
  onClose,
}: {
  team: TeamSummary | null;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const { data: detail } = useQuery({
    queryKey: queryKeys.teams.detail(team?.id ?? ''),
    queryFn: () => teamsApi.detail(team!.id),
    enabled: Boolean(team),
  });

  const { data: users = [] } = useQuery({
    queryKey: queryKeys.users.assignable,
    queryFn: usersApi.assignable,
    enabled: Boolean(team),
  });

  const memberIds = new Set(detail?.members.map((member) => member.user.id) ?? []);
  const candidates = users.filter((user) => !memberIds.has(user.id));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.teams.all });
    void queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
  };

  const addMembers = useMutation({
    mutationFn: () => teamsApi.addMembers(team!.id, selectedIds),
    onSuccess: () => {
      invalidate();
      toast.success(`${selectedIds.length} üye eklendi.`);
      setSelectedIds([]);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const removeMember = useMutation({
    mutationFn: (userId: string) => teamsApi.removeMember(team!.id, userId),
    onSuccess: () => {
      invalidate();
      toast.success('Üye çıkarıldı.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const assignLeader = useMutation({
    mutationFn: (userId: string) => teamsApi.assignLeader(team!.id, userId),
    onSuccess: () => {
      invalidate();
      toast.success('Takım lideri güncellendi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog
      open={Boolean(team)}
      onOpenChange={(open) => {
        if (!open) {
          setSelectedIds([]);
          onClose();
        }
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{team?.name} · üyeler</DialogTitle>
          <DialogDescription>
            Üye ekleyip çıkarabilir, takım liderini değiştirebilirsiniz.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-5">
          <div className="space-y-2">
            <Label>Mevcut üyeler ({detail?.members.length ?? 0})</Label>

            {detail?.members.length ? (
              <ul className="max-h-48 divide-y divide-border overflow-y-auto rounded-lg border border-border">
                {detail.members.map((member) => (
                  <li key={member.id} className="flex items-center gap-2.5 px-3 py-2">
                    <Avatar
                      fullName={member.user.fullName}
                      avatarUrl={member.user.avatarUrl}
                      size="xs"
                    />
                    <span className="min-w-0 flex-1 truncate text-sm">
                      {member.user.fullName}
                    </span>

                    {member.teamRole === 1 ? (
                      <Badge variant="warning">
                        <Crown aria-hidden="true" />
                        Lider
                      </Badge>
                    ) : (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => assignLeader.mutate(member.user.id)}
                        disabled={assignLeader.isPending}
                      >
                        Lider yap
                      </Button>
                    )}

                    <Button
                      variant="ghost"
                      size="icon-sm"
                      aria-label={`${member.user.fullName} çıkar`}
                      onClick={() => removeMember.mutate(member.user.id)}
                      disabled={removeMember.isPending}
                    >
                      <Trash2 className="text-danger" aria-hidden="true" />
                    </Button>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="rounded-lg border border-dashed border-border px-3 py-4 text-center text-sm text-muted-foreground">
                Bu takımda henüz üye yok.
              </p>
            )}
          </div>

          {candidates.length > 0 ? (
            <div className="space-y-2">
              <Label>Üye ekle</Label>
              <div className="max-h-40 space-y-2 overflow-y-auto rounded-lg border border-border p-3">
                {candidates.map((user) => (
                  <label key={user.id} className="flex cursor-pointer items-center gap-2.5 text-sm">
                    <Checkbox
                      checked={selectedIds.includes(user.id)}
                      onCheckedChange={(checked) =>
                        setSelectedIds((previous) =>
                          checked === true
                            ? [...previous, user.id]
                            : previous.filter((id) => id !== user.id),
                        )
                      }
                    />
                    <Avatar fullName={user.fullName} avatarUrl={user.avatarUrl} size="xs" />
                    <span className="truncate">{user.fullName}</span>
                  </label>
                ))}
              </div>

              <Button
                size="sm"
                variant="secondary"
                onClick={() => addMembers.mutate()}
                disabled={selectedIds.length === 0 || addMembers.isPending}
              >
                <UserPlus aria-hidden="true" />
                Seçilenleri ekle ({selectedIds.length})
              </Button>
            </div>
          ) : null}
        </div>

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Kapat
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function DeleteTeamDialog({ team, onClose }: { team: TeamSummary | null; onClose: () => void }) {
  const queryClient = useQueryClient();

  const remove = useMutation({
    mutationFn: () => teamsApi.remove(team!.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.teams.all });
      toast.success(`${team?.name} silindi.`);
      onClose();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog open={Boolean(team)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Takımı sil</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{team?.name}</strong> silinecek. Görevler ve
            sprintler kaybolmaz, yalnızca takım bağı kopar. Sohbet geçmişi korunur.
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
