import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { Avatar } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input, Textarea } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { usersApi } from '@/features/users/api/users-api';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import type { ProjectSummary } from '@/types/api';
import { ProjectStatus, projectStatusLabels } from '@/types/enums';

import { projectsApi } from '../api/projects-api';

const projectColors = [
  '#8B5CF6',
  '#6366F1',
  '#3B82F6',
  '#06B6D4',
  '#22C55E',
  '#F59E0B',
  '#F97316',
  '#EF4444',
  '#EC4899',
  '#64748B',
];

/** Sunucudaki doğrulama kurallarıyla birebir aynı tutulur. */
const projectSchema = z
  .object({
    name: z
      .string()
      .min(2, 'Proje adı en az 2 karakter olmalıdır.')
      .max(128, 'Proje adı en fazla 128 karakter olabilir.'),
    key: z
      .string()
      .regex(
        /^[A-Za-z][A-Za-z0-9]{1,9}$/,
        'Anahtar harfle başlamalı, 2-10 karakter olmalı ve yalnızca harf/rakam içermelidir.',
      ),
    description: z.string().max(4000, 'Açıklama en fazla 4000 karakter olabilir.').optional(),
    status: z.number().int(),
    colorHex: z.string().regex(/^#[0-9a-fA-F]{6}$/, 'Renk seçin.'),
    genre: z.string().max(64, 'Tür en fazla 64 karakter olabilir.').optional(),
    platforms: z.string().max(256, 'Platformlar en fazla 256 karakter olabilir.').optional(),
    startDate: z.string().optional(),
    targetReleaseDate: z.string().optional(),
    memberIds: z.array(z.string()),
  })
  .refine(
    (values) =>
      !values.startDate ||
      !values.targetReleaseDate ||
      values.targetReleaseDate >= values.startDate,
    {
      message: 'Hedef çıkış tarihi başlangıç tarihinden önce olamaz.',
      path: ['targetReleaseDate'],
    },
  );

type ProjectFormValues = z.infer<typeof projectSchema>;

type ProjectFormDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  project?: ProjectSummary | null;
};

/** Tarih girdisi (yyyy-MM-dd) → sunucunun beklediği ISO zaman damgası. */
function toIsoDate(value: string | undefined): string | null {
  return value ? new Date(`${value}T00:00:00Z`).toISOString() : null;
}

/** ISO zaman damgası → tarih girdisi biçimi. */
function toDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : '';
}

export function ProjectFormDialog({ open, onOpenChange, project }: ProjectFormDialogProps) {
  const queryClient = useQueryClient();
  const isEditMode = Boolean(project);

  const { data: users = [] } = useQuery({
    queryKey: queryKeys.users.assignable,
    queryFn: usersApi.assignable,
    enabled: open,
  });

  // Düzenleme modunda listede olmayan alanlar (tür, platform, tarihler) için ayrıntı gerekir.
  const { data: detail } = useQuery({
    queryKey: queryKeys.projects.detail(project?.id ?? ''),
    queryFn: () => projectsApi.detail(project!.id),
    enabled: open && isEditMode,
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<ProjectFormValues>({
    resolver: zodResolver(projectSchema),
    defaultValues: {
      name: '',
      key: '',
      description: '',
      status: ProjectStatus.Planning,
      colorHex: projectColors[0],
      genre: '',
      platforms: '',
      startDate: '',
      targetReleaseDate: '',
      memberIds: [],
    },
  });

  // Ayrıntı geldiğinde form gerçek değerlerle yeniden doldurulur.
  useEffect(() => {
    if (detail) {
      reset({
        name: detail.name,
        key: detail.key,
        description: detail.description ?? '',
        status: detail.status,
        colorHex: detail.colorHex,
        genre: detail.genre ?? '',
        platforms: detail.platforms ?? '',
        startDate: toDateInput(detail.startDate),
        targetReleaseDate: toDateInput(detail.targetReleaseDate),
        memberIds: [],
      });
    }
  }, [detail, reset]);

  const save = useMutation({
    mutationFn: (values: ProjectFormValues) => {
      const payload = {
        name: values.name,
        description: values.description || null,
        status: values.status as ProjectStatus,
        colorHex: values.colorHex,
        genre: values.genre || null,
        platforms: values.platforms || null,
        startDate: toIsoDate(values.startDate),
        targetReleaseDate: toIsoDate(values.targetReleaseDate),
      };

      return isEditMode && project
        ? projectsApi.update(project.id, payload)
        : projectsApi.create({ ...payload, key: values.key, memberIds: values.memberIds });
    },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects.all });

      toast.success(
        isEditMode ? `${saved.name} güncellendi.` : `${saved.name} (${saved.key}) oluşturuldu.`,
      );
      reset();
      onOpenChange(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const name = watch('name');
  const selectedColor = watch('colorHex');
  const selectedMemberIds = watch('memberIds');

  /**
   * Proje adından anahtar önerir: baş harfler, yoksa ilk üç harf.
   * Kullanıcı elle değiştirdiyse üzerine yazılmaz.
   */
  const suggestKey = () => {
    if (isEditMode || watch('key')) return;

    const words = name.trim().split(/\s+/).filter(Boolean);

    const suggestion =
      words.length > 1
        ? words
            .map((word) => word[0])
            .join('')
            .slice(0, 5)
        : words[0]?.slice(0, 4) ?? '';

    // Türkçe karakterler anahtarda kullanılamaz; ASCII karşılıklarına indirilir.
    const ascii = suggestion
      .replace(/[çÇ]/g, 'C')
      .replace(/[ğĞ]/g, 'G')
      .replace(/[ıİ]/g, 'I')
      .replace(/[öÖ]/g, 'O')
      .replace(/[şŞ]/g, 'S')
      .replace(/[üÜ]/g, 'U')
      .replace(/[^A-Za-z0-9]/g, '')
      .toUpperCase();

    if (ascii.length >= 2) {
      setValue('key', ascii);
    }
  };

  const toggleMember = (userId: string, checked: boolean) => {
    setValue(
      'memberIds',
      checked
        ? [...selectedMemberIds, userId]
        : selectedMemberIds.filter((id) => id !== userId),
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent key={project?.id ?? 'new'} className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEditMode ? 'Projeyi düzenle' : 'Yeni proje'}</DialogTitle>
          <DialogDescription>
            Her projenin kendi görevleri, sprintleri, takvimi ve sohbet odası olur.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => save.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <div className="grid gap-4 sm:grid-cols-[1fr_9rem]">
            <div className="space-y-1.5">
              <Label htmlFor="project-name">
                Proje adı<span className="ml-0.5 text-danger">*</span>
              </Label>
              <Input
                id="project-name"
                autoFocus
                placeholder="Odyssey"
                aria-invalid={Boolean(errors.name)}
                {...register('name', { onBlur: suggestKey })}
              />
              {errors.name ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.name.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="project-key">
                Anahtar<span className="ml-0.5 text-danger">*</span>
              </Label>
              <Input
                id="project-key"
                placeholder="ODY"
                disabled={isEditMode}
                aria-invalid={Boolean(errors.key)}
                className="font-mono uppercase"
                {...register('key')}
              />
            </div>
          </div>

          {errors.key ? (
            <p role="alert" className="-mt-2 text-xs text-danger">
              {errors.key.message}
            </p>
          ) : (
            <p className="-mt-2 text-xs text-subtle-foreground">
              {isEditMode
                ? 'Anahtar, görev numaralarına gömülü olduğu için değiştirilemez.'
                : 'Görevler bu anahtarla numaralanır: ODY-1, ODY-2 …'}
            </p>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="project-status">Durum</Label>
              <Select
                value={String(watch('status'))}
                onValueChange={(value) => setValue('status', Number(value))}
              >
                <SelectTrigger id="project-status">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(projectStatusLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="project-genre">Oyun türü</Label>
              <Input id="project-genre" placeholder="Roguelike" {...register('genre')} />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="project-platforms">Platformlar</Label>
            <Input id="project-platforms" placeholder="PC, PS5, Xbox" {...register('platforms')} />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="project-start">Başlangıç tarihi</Label>
              <Input id="project-start" type="date" {...register('startDate')} />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="project-release">Hedef çıkış</Label>
              <Input
                id="project-release"
                type="date"
                aria-invalid={Boolean(errors.targetReleaseDate)}
                {...register('targetReleaseDate')}
              />
              {errors.targetReleaseDate ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.targetReleaseDate.message}
                </p>
              ) : null}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>Renk</Label>
            <div className="flex flex-wrap gap-2">
              {projectColors.map((color) => (
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
                      selectedColor === color
                        ? `0 0 0 2px var(--background), 0 0 0 4px ${color}`
                        : undefined,
                  }}
                />
              ))}
            </div>
          </div>

          {!isEditMode && users.length > 0 ? (
            <div className="space-y-1.5">
              <Label>Proje üyeleri</Label>
              <div className="max-h-36 space-y-2 overflow-y-auto rounded-lg border border-border p-3">
                {users.map((user) => (
                  <label
                    key={user.id}
                    className="flex cursor-pointer items-center gap-2.5 text-sm"
                  >
                    <Checkbox
                      checked={selectedMemberIds.includes(user.id)}
                      onCheckedChange={(checked) => toggleMember(user.id, checked === true)}
                    />
                    <Avatar fullName={user.fullName} avatarUrl={user.avatarUrl} size="xs" />
                    <span className="truncate">{user.fullName}</span>
                  </label>
                ))}
              </div>
              <p className="text-xs text-subtle-foreground">
                Görevler yalnızca proje üyelerine atanabilir. Siz otomatik olarak eklenirsiniz.
              </p>
            </div>
          ) : null}

          <div className="space-y-1.5">
            <Label htmlFor="project-description">Açıklama</Label>
            <Textarea
              id="project-description"
              rows={2}
              placeholder="Prosedürel seviyelerle ilerleyen roguelike aksiyon oyunu."
              {...register('description')}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              {isEditMode ? 'Kaydet' : 'Projeyi oluştur'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
