import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

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
import { Switch } from '@/components/ui/switch';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { teamsApi } from '@/features/teams/api/teams-api';
import type { UserSummary } from '@/types/api';
import { SystemRole, systemRoleLabels, teamCategoryLabels } from '@/types/enums';

import { usersApi } from '../api/users-api';

/**
 * Şifre politikası sunucudakiyle birebir aynı tutulur; kullanıcı formu
 * göndermeden hatayı görür, sunucu yine de kendi doğrulamasını yapar.
 */
const passwordSchema = z
  .string()
  .min(8, 'Şifre en az 8 karakter olmalıdır.')
  .max(128, 'Şifre en fazla 128 karakter olabilir.')
  .regex(/[A-ZÇĞİÖŞÜ]/, 'Şifre en az bir büyük harf içermelidir.')
  .regex(/[a-zçğıöşü]/, 'Şifre en az bir küçük harf içermelidir.')
  .regex(/[0-9]/, 'Şifre en az bir rakam içermelidir.');

const createSchema = z.object({
  fullName: z
    .string()
    .min(3, 'Ad soyad en az 3 karakter olmalıdır.')
    .max(128, 'Ad soyad en fazla 128 karakter olabilir.'),
  email: z.string().min(1, 'E-posta zorunludur.').email('Geçerli bir e-posta adresi girin.'),
  password: passwordSchema,
  role: z.number().int(),
  jobTitle: z.string().max(128, 'Unvan en fazla 128 karakter olabilir.').optional(),
  bio: z.string().max(1024, 'Biyografi en fazla 1024 karakter olabilir.').optional(),
  mustChangePassword: z.boolean(),
  teamIds: z.array(z.string()),
});

const editSchema = createSchema
  .omit({ password: true, mustChangePassword: true, teamIds: true, email: true })
  .extend({ isActive: z.boolean() });

type CreateFormValues = z.infer<typeof createSchema>;
type EditFormValues = z.infer<typeof editSchema>;

type UserFormDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Verilirse düzenleme, verilmezse oluşturma modunda açılır. */
  user?: UserSummary | null;
};

export function UserFormDialog({ open, onOpenChange, user }: UserFormDialogProps) {
  const isEditMode = Boolean(user);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditMode ? 'Kullanıcıyı düzenle' : 'Yeni kullanıcı'}</DialogTitle>
          <DialogDescription>
            {isEditMode
              ? 'Ad, unvan, rol ve hesap durumunu güncelleyebilirsiniz.'
              : 'Kullanıcı hesabı oluşturun. Sistemde kayıt ekranı yoktur; hesaplar yalnızca buradan açılır.'}
          </DialogDescription>
        </DialogHeader>

        {isEditMode && user ? (
          <EditUserForm user={user} onDone={() => onOpenChange(false)} />
        ) : (
          <CreateUserForm onDone={() => onOpenChange(false)} />
        )}
      </DialogContent>
    </Dialog>
  );
}

function CreateUserForm({ onDone }: { onDone: () => void }) {
  const queryClient = useQueryClient();

  const { data: teams = [] } = useQuery({
    queryKey: queryKeys.teams.list({}),
    queryFn: () => teamsApi.list(),
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<CreateFormValues>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      fullName: '',
      email: '',
      password: '',
      role: SystemRole.TeamMember,
      jobTitle: '',
      bio: '',
      mustChangePassword: true,
      teamIds: [],
    },
  });

  const create = useMutation({
    mutationFn: (values: CreateFormValues) =>
      usersApi.create({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        role: values.role as SystemRole,
        jobTitle: values.jobTitle || null,
        bio: values.bio || null,
        mustChangePassword: values.mustChangePassword,
        teamIds: values.teamIds,
      }),
    onSuccess: (created) => {
      // Kullanıcı listesi ve takım üye sayıları tazelenir.
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.teams.all });

      toast.success(`${created.fullName} oluşturuldu.`);
      reset();
      onDone();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const selectedTeamIds = watch('teamIds');
  const role = watch('role');

  const toggleTeam = (teamId: string, checked: boolean) => {
    setValue(
      'teamIds',
      checked ? [...selectedTeamIds, teamId] : selectedTeamIds.filter((id) => id !== teamId),
      { shouldValidate: true },
    );
  };

  return (
    <form onSubmit={handleSubmit((values) => create.mutate(values))} noValidate className="space-y-4">
      <Field label="Ad soyad" error={errors.fullName?.message} htmlFor="fullName" required>
        <Input
          id="fullName"
          autoFocus
          placeholder="Elif Yılmaz"
          aria-invalid={Boolean(errors.fullName)}
          {...register('fullName')}
        />
      </Field>

      <Field label="E-posta" error={errors.email?.message} htmlFor="email" required>
        <Input
          id="email"
          type="email"
          autoComplete="off"
          placeholder="elif@studyo.com"
          aria-invalid={Boolean(errors.email)}
          {...register('email')}
        />
      </Field>

      <Field
        label="Geçici şifre"
        error={errors.password?.message}
        htmlFor="password"
        required
        hint="En az 8 karakter, bir büyük harf, bir küçük harf ve bir rakam."
      >
        <Input
          id="password"
          type="text"
          autoComplete="new-password"
          placeholder="Gelistirici1"
          aria-invalid={Boolean(errors.password)}
          {...register('password')}
        />
      </Field>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="Rol" htmlFor="role" required>
          <Select
            value={String(role)}
            onValueChange={(value) => setValue('role', Number(value))}
          >
            <SelectTrigger id="role">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(systemRoleLabels).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>

        <Field label="Unvan" error={errors.jobTitle?.message} htmlFor="jobTitle">
          <Input id="jobTitle" placeholder="Gameplay Programmer" {...register('jobTitle')} />
        </Field>
      </div>

      {teams.length > 0 ? (
        <Field label="Takımlar" hint="Kullanıcı oluşturulurken doğrudan eklenir.">
          <div className="max-h-36 space-y-2 overflow-y-auto rounded-lg border border-border p-3">
            {teams.map((team) => (
              <label key={team.id} className="flex cursor-pointer items-center gap-2.5 text-sm">
                <Checkbox
                  checked={selectedTeamIds.includes(team.id)}
                  onCheckedChange={(checked) => toggleTeam(team.id, checked === true)}
                />
                <span
                  className="size-2 shrink-0 rounded-full"
                  style={{ backgroundColor: team.colorHex }}
                  aria-hidden="true"
                />
                <span className="truncate">{team.name}</span>
                <span className="ml-auto shrink-0 text-xs text-subtle-foreground">
                  {teamCategoryLabels[team.category]}
                </span>
              </label>
            ))}
          </div>
        </Field>
      ) : null}

      <label className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border border-border p-3">
        <span className="text-sm">
          İlk girişte şifre değiştirmesi zorunlu
          <span className="block text-xs text-muted-foreground">
            Kullanıcı geçici şifreyle girip kendi şifresini belirler.
          </span>
        </span>
        <Switch
          checked={watch('mustChangePassword')}
          onCheckedChange={(checked) => setValue('mustChangePassword', checked)}
        />
      </label>

      <DialogFooter>
        <Button type="button" variant="secondary" onClick={onDone}>
          Vazgeç
        </Button>
        <Button type="submit" disabled={create.isPending}>
          {create.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
          Kullanıcıyı oluştur
        </Button>
      </DialogFooter>
    </form>
  );
}

function EditUserForm({ user, onDone }: { user: UserSummary; onDone: () => void }) {
  const queryClient = useQueryClient();

  // Ayrıntı sorgusu bio ve aktiflik gibi listede olmayan alanları getirir.
  const { data: detail } = useQuery({
    queryKey: queryKeys.users.detail(user.id),
    queryFn: () => usersApi.detail(user.id),
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      fullName: user.fullName,
      role: user.role,
      jobTitle: user.jobTitle ?? '',
      bio: '',
      isActive: true,
    },
  });

  // Ayrıntı geldiğinde form güncel değerlerle yeniden doldurulur.
  useEffect(() => {
    if (detail) {
      reset({
        fullName: detail.fullName,
        role: detail.role,
        jobTitle: detail.jobTitle ?? '',
        bio: detail.bio ?? '',
        isActive: detail.isActive,
      });
    }
  }, [detail, reset]);

  const update = useMutation({
    mutationFn: (values: EditFormValues) =>
      usersApi.update(user.id, {
        fullName: values.fullName,
        role: values.role as SystemRole,
        jobTitle: values.jobTitle || null,
        bio: values.bio || null,
        isActive: values.isActive,
      }),
    onSuccess: (updated) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
      toast.success(`${updated.fullName} güncellendi.`);
      onDone();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <form onSubmit={handleSubmit((values) => update.mutate(values))} noValidate className="space-y-4">
      <Field label="Ad soyad" error={errors.fullName?.message} htmlFor="edit-fullName" required>
        <Input id="edit-fullName" autoFocus {...register('fullName')} />
      </Field>

      <Field label="E-posta" hint="E-posta adresi değiştirilemez.">
        <Input value={user.email} disabled readOnly />
      </Field>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="Rol" htmlFor="edit-role" required>
          <Select
            value={String(watch('role'))}
            onValueChange={(value) => setValue('role', Number(value))}
          >
            <SelectTrigger id="edit-role">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(systemRoleLabels).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>

        <Field label="Unvan" error={errors.jobTitle?.message} htmlFor="edit-jobTitle">
          <Input id="edit-jobTitle" {...register('jobTitle')} />
        </Field>
      </div>

      <Field label="Biyografi" error={errors.bio?.message} htmlFor="edit-bio">
        <Textarea id="edit-bio" rows={3} {...register('bio')} />
      </Field>

      <label className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border border-border p-3">
        <span className="text-sm">
          Hesap aktif
          <span className="block text-xs text-muted-foreground">
            Kapatılırsa kullanıcı giriş yapamaz.
          </span>
        </span>
        <Switch
          checked={watch('isActive')}
          onCheckedChange={(checked) => setValue('isActive', checked)}
        />
      </label>

      <DialogFooter>
        <Button type="button" variant="secondary" onClick={onDone}>
          Vazgeç
        </Button>
        <Button type="submit" disabled={update.isPending}>
          {update.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
          Kaydet
        </Button>
      </DialogFooter>
    </form>
  );
}

/** Etiket + alan + hata mesajı üçlüsünü tek yerde toplar. */
function Field({
  label,
  htmlFor,
  error,
  hint,
  required,
  children,
}: {
  label: string;
  htmlFor?: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={htmlFor}>
        {label}
        {required ? <span className="ml-0.5 text-danger">*</span> : null}
      </Label>
      {children}
      {error ? (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      ) : hint ? (
        <p className="text-xs text-subtle-foreground">{hint}</p>
      ) : null}
    </div>
  );
}
