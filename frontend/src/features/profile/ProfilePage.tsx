import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { KeyRound, Loader2, Save } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { z } from 'zod';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input, Textarea } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { authApi } from '@/features/auth/api/auth-api';
import { usersApi } from '@/features/users/api/users-api';
import { getErrorMessage } from '@/lib/api-client';
import { formatDate } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import { systemRoleLabels, teamCategoryLabels, teamRoleLabels } from '@/types/enums';

const profileSchema = z.object({
  fullName: z
    .string()
    .min(3, 'Ad soyad en az 3 karakter olmalıdır.')
    .max(128, 'Ad soyad en fazla 128 karakter olabilir.'),
  jobTitle: z.string().max(128).optional(),
  bio: z.string().max(1024).optional(),
});

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Mevcut şifre zorunludur.'),
    newPassword: z
      .string()
      .min(8, 'Şifre en az 8 karakter olmalıdır.')
      .regex(/[A-ZÇĞİÖŞÜ]/, 'Şifre en az bir büyük harf içermelidir.')
      .regex(/[a-zçğıöşü]/, 'Şifre en az bir küçük harf içermelidir.')
      .regex(/[0-9]/, 'Şifre en az bir rakam içermelidir.'),
    confirmPassword: z.string().min(1, 'Şifreyi tekrar girin.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Şifreler birbiriyle eşleşmiyor.',
    path: ['confirmPassword'],
  });

type ProfileValues = z.infer<typeof profileSchema>;
type PasswordValues = z.infer<typeof passwordSchema>;

/**
 * Profil sayfası. Adres kullanıcı kimliği taşıyorsa başka birinin profili
 * (salt okunur), taşımıyorsa oturum sahibinin düzenlenebilir profili gösterilir.
 */
export function ProfilePage() {
  const { userId } = useParams();
  const currentUser = useAuthStore((state) => state.user);
  const isOwnProfile = !userId || userId === currentUser?.id;
  const targetId = userId ?? currentUser?.id ?? '';

  const { data: detail, isLoading } = useQuery({
    queryKey: queryKeys.users.detail(targetId),
    queryFn: () => usersApi.detail(targetId),
    enabled: Boolean(targetId),
  });

  if (isLoading || !detail) {
    return (
      <div className="mx-auto w-full max-w-3xl space-y-4">
        <Skeleton className="h-32 rounded-card" />
        <Skeleton className="h-64 rounded-card" />
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-3xl space-y-4">
      <Card>
        <CardContent className="flex flex-wrap items-center gap-4 pt-5">
          <Avatar
            fullName={detail.fullName}
            avatarUrl={detail.avatarUrl}
            size="xl"
            isOnline={detail.isOnline}
          />

          <div className="min-w-0 flex-1">
            <h1 className="text-xl font-semibold tracking-tight">{detail.fullName}</h1>
            <p className="text-sm text-muted-foreground">{detail.email}</p>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <Badge variant="primary">{systemRoleLabels[detail.role]}</Badge>
              {detail.jobTitle ? <Badge variant="neutral">{detail.jobTitle}</Badge> : null}
              {!detail.isActive ? <Badge variant="danger">Devre dışı</Badge> : null}
            </div>
          </div>

          <div className="flex gap-6 text-center">
            <div>
              <p className="text-2xl font-semibold tabular-nums text-success">
                {detail.completedTaskCount}
              </p>
              <p className="text-xs text-muted-foreground">Tamamlanan</p>
            </div>
            <div>
              <p className="text-2xl font-semibold tabular-nums">{detail.activeTaskCount}</p>
              <p className="text-xs text-muted-foreground">Aktif</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {detail.bio ? (
        <Card>
          <CardContent className="pt-5">
            <p className="text-sm leading-relaxed whitespace-pre-wrap">{detail.bio}</p>
          </CardContent>
        </Card>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Takımlar</CardTitle>
          </CardHeader>
          <CardContent>
            {detail.teams.length === 0 ? (
              <p className="text-sm text-muted-foreground">Takıma dâhil değil.</p>
            ) : (
              <ul className="space-y-2">
                {detail.teams.map((team) => (
                  <li key={team.id}>
                    <Link
                      to={`/takimlar/${team.id}`}
                      className="flex items-center gap-2.5 rounded-lg px-1 py-1 text-sm transition-colors hover:bg-surface-raised"
                    >
                      <span
                        className="size-2 shrink-0 rounded-full"
                        style={{ backgroundColor: team.colorHex }}
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate">{team.name}</span>
                      <span className="shrink-0 text-xs text-muted-foreground">
                        {teamRoleLabels[team.teamRole]} · {teamCategoryLabels[team.category]}
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Projeler</CardTitle>
          </CardHeader>
          <CardContent>
            {detail.projects.length === 0 ? (
              <p className="text-sm text-muted-foreground">Projeye dâhil değil.</p>
            ) : (
              <ul className="space-y-2">
                {detail.projects.map((project) => (
                  <li key={project.id}>
                    <Link
                      to={`/projeler/${project.id}`}
                      className="flex items-center gap-2.5 rounded-lg px-1 py-1 text-sm transition-colors hover:bg-surface-raised"
                    >
                      <span
                        className="size-2 shrink-0 rounded-full"
                        style={{ backgroundColor: project.colorHex }}
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate">{project.name}</span>
                      <span className="shrink-0 font-mono text-xs text-subtle-foreground">
                        {project.key}
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>

      <p className="text-center text-xs text-subtle-foreground">
        Katılım: {formatDate(detail.createdAt)}
      </p>

      {isOwnProfile ? (
        <>
          <ProfileForm
            defaultValues={{
              fullName: detail.fullName,
              jobTitle: detail.jobTitle ?? '',
              bio: detail.bio ?? '',
            }}
            userId={detail.id}
          />
          <PasswordForm />
        </>
      ) : null}
    </div>
  );
}

function ProfileForm({
  defaultValues,
  userId,
}: {
  defaultValues: ProfileValues;
  userId: string;
}) {
  const queryClient = useQueryClient();
  const setUser = useAuthStore((state) => state.setUser);

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<ProfileValues>({ resolver: zodResolver(profileSchema), defaultValues });

  const update = useMutation({
    mutationFn: (values: ProfileValues) =>
      authApi.updateProfile({
        fullName: values.fullName,
        jobTitle: values.jobTitle || null,
        bio: values.bio || null,
      }),
    onSuccess: (updated) => {
      // Kenar çubuğundaki ad ve avatar da güncellenir.
      setUser(updated);
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.detail(userId) });
      toast.success('Profil güncellendi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Profili düzenle</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit((values) => update.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <div className="space-y-1.5">
            <Label htmlFor="profile-name">Ad soyad</Label>
            <Input
              id="profile-name"
              aria-invalid={Boolean(errors.fullName)}
              {...register('fullName')}
            />
            {errors.fullName ? (
              <p role="alert" className="text-xs text-danger">
                {errors.fullName.message}
              </p>
            ) : null}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="profile-title">Unvan</Label>
            <Input id="profile-title" placeholder="Gameplay Programmer" {...register('jobTitle')} />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="profile-bio">Hakkımda</Label>
            <Textarea id="profile-bio" rows={3} {...register('bio')} />
          </div>

          <Button type="submit" disabled={!isDirty || update.isPending}>
            {update.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <Save aria-hidden="true" />
            )}
            Kaydet
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function PasswordForm() {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const change = useMutation({
    mutationFn: (values: PasswordValues) =>
      authApi.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      }),
    onSuccess: () => {
      reset();
      toast.success('Şifreniz değiştirildi. Diğer oturumlarınız kapatıldı.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Şifre değiştir</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit((values) => change.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <div className="space-y-1.5">
            <Label htmlFor="current-password">Mevcut şifre</Label>
            <Input
              id="current-password"
              type="password"
              autoComplete="current-password"
              aria-invalid={Boolean(errors.currentPassword)}
              {...register('currentPassword')}
            />
            {errors.currentPassword ? (
              <p role="alert" className="text-xs text-danger">
                {errors.currentPassword.message}
              </p>
            ) : null}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="new-password">Yeni şifre</Label>
              <Input
                id="new-password"
                type="password"
                autoComplete="new-password"
                aria-invalid={Boolean(errors.newPassword)}
                {...register('newPassword')}
              />
              {errors.newPassword ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.newPassword.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="confirm-password">Yeni şifre (tekrar)</Label>
              <Input
                id="confirm-password"
                type="password"
                autoComplete="new-password"
                aria-invalid={Boolean(errors.confirmPassword)}
                {...register('confirmPassword')}
              />
              {errors.confirmPassword ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.confirmPassword.message}
                </p>
              ) : null}
            </div>
          </div>

          <Button type="submit" variant="secondary" disabled={change.isPending}>
            {change.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <KeyRound aria-hidden="true" />
            )}
            Şifreyi değiştir
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
