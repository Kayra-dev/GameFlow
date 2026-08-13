import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { projectsApi } from '@/features/projects/api/projects-api';
import { teamsApi } from '@/features/teams/api/teams-api';
import { usersApi } from '@/features/users/api/users-api';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';

import { meetingsApi } from '../api/meetings-api';
import { Field, toLocalInputValue } from '@/components/common/form-field';

/** Toplantı süresi sunucuda da sınırlıdır; kullanıcı hatayı formda görür. */
const MAX_DURATION_HOURS = 24;

const schema = z
  .object({
    title: z
      .string()
      .min(1, 'Toplantı başlığı zorunludur.')
      .max(192, 'Başlık en fazla 192 karakter olabilir.'),
    description: z.string().max(2000, 'Açıklama en fazla 2000 karakter olabilir.').optional(),
    startsAt: z.string().min(1, 'Başlangıç zamanı zorunludur.'),
    endsAt: z.string().min(1, 'Bitiş zamanı zorunludur.'),
    location: z.string().max(192, 'Konum en fazla 192 karakter olabilir.').optional(),
    meetingUrl: z
      .string()
      .max(512)
      .refine((value) => !value || /^https?:\/\/\S+$/i.test(value), {
        message: 'Bağlantı http:// veya https:// ile başlamalıdır.',
      })
      .optional(),
    scope: z.string(),
    attendeeIds: z.array(z.string()).min(1, 'En az bir katılımcı seçin.'),
  })
  .refine((values) => new Date(values.endsAt) > new Date(values.startsAt), {
    path: ['endsAt'],
    message: 'Toplantı bitişi başlangıçtan sonra olmalıdır.',
  })
  .refine(
    (values) => {
      const hours =
        (new Date(values.endsAt).getTime() - new Date(values.startsAt).getTime()) / 3_600_000;

      return !(hours > MAX_DURATION_HOURS);
    },
    { path: ['endsAt'], message: `Toplantı süresi en fazla ${MAX_DURATION_HOURS} saat olabilir.` },
  );

type FormValues = z.infer<typeof schema>;

type MeetingFormDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  defaultDate?: Date;
};

/**
 * Toplantı oluşturma. Sunucu, oluşturan kişinin yönetici ya da ilgili
 * takım/projenin lideri olmasını şart koşar; düğme de aynı kurala göre gizlenir.
 */
export function MeetingFormDialog({ open, onOpenChange, defaultDate }: MeetingFormDialogProps) {
  const queryClient = useQueryClient();
  const [attendeeSearch, setAttendeeSearch] = useState('');

  const { data: users = [] } = useQuery({
    queryKey: [...queryKeys.users.all, 'assignable'],
    queryFn: () => usersApi.assignable(),
    enabled: open,
  });

  const { data: projects = [] } = useQuery({
    queryKey: queryKeys.projects.list({}),
    queryFn: () => projectsApi.list(),
    enabled: open,
  });

  const { data: teams = [] } = useQuery({
    queryKey: queryKeys.teams.list({}),
    queryFn: () => teamsApi.list(),
    enabled: open,
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      startsAt: '',
      endsAt: '',
      location: '',
      meetingUrl: '',
      scope: 'none',
      attendeeIds: [],
    },
  });

  useEffect(() => {
    if (!open) return;

    const start = new Date(defaultDate ?? new Date());
    start.setHours(10, 0, 0, 0);

    const end = new Date(start);
    end.setHours(11, 0, 0, 0);

    setAttendeeSearch('');

    reset({
      title: '',
      description: '',
      startsAt: toLocalInputValue(start),
      endsAt: toLocalInputValue(end),
      location: '',
      meetingUrl: '',
      scope: 'none',
      attendeeIds: [],
    });
  }, [open, defaultDate, reset]);

  const create = useMutation({
    mutationFn: (values: FormValues) => {
      const [scopeKind, scopeId] = values.scope.split(':');

      return meetingsApi.create({
        title: values.title,
        description: values.description || null,
        startsAt: new Date(values.startsAt).toISOString(),
        endsAt: new Date(values.endsAt).toISOString(),
        location: values.location || null,
        meetingUrl: values.meetingUrl || null,
        projectId: scopeKind === 'project' ? (scopeId ?? null) : null,
        teamId: scopeKind === 'team' ? (scopeId ?? null) : null,
        attendeeIds: values.attendeeIds,
      });
    },
    onSuccess: (created) => {
      void queryClient.invalidateQueries({ queryKey: ['calendar'] });
      void queryClient.invalidateQueries({ queryKey: ['meetings'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });

      toast.success(`“${created.title}” toplantısı oluşturuldu.`);
      onOpenChange(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const attendeeIds = watch('attendeeIds');

  const filteredUsers = useMemo(() => {
    const query = attendeeSearch.trim().toLocaleLowerCase('tr');

    if (!query) return users;

    return users.filter(
      (user) =>
        user.fullName.toLocaleLowerCase('tr').includes(query) ||
        user.email.toLocaleLowerCase('tr').includes(query),
    );
  }, [users, attendeeSearch]);

  const toggleAttendee = (userId: string, checked: boolean) => {
    setValue(
      'attendeeIds',
      checked ? [...attendeeIds, userId] : attendeeIds.filter((id) => id !== userId),
      { shouldValidate: true },
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Yeni toplantı</DialogTitle>
          <DialogDescription>
            Katılımcılara bildirim gider ve toplantı herkesin takviminde görünür.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => create.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <Field label="Başlık" error={errors.title?.message} htmlFor="meeting-title" required>
            <Input
              id="meeting-title"
              autoFocus
              placeholder="Sprint planlama"
              aria-invalid={Boolean(errors.title)}
              {...register('title')}
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Başlangıç"
              error={errors.startsAt?.message}
              htmlFor="meeting-start"
              required
            >
              <Input
                id="meeting-start"
                type="datetime-local"
                aria-invalid={Boolean(errors.startsAt)}
                {...register('startsAt')}
              />
            </Field>

            <Field label="Bitiş" error={errors.endsAt?.message} htmlFor="meeting-end" required>
              <Input
                id="meeting-end"
                type="datetime-local"
                aria-invalid={Boolean(errors.endsAt)}
                {...register('endsAt')}
              />
            </Field>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Konum" error={errors.location?.message} htmlFor="meeting-location">
              <Input id="meeting-location" placeholder="Toplantı odası 2" {...register('location')} />
            </Field>

            <Field label="Bağlantı" error={errors.meetingUrl?.message} htmlFor="meeting-url">
              <Input
                id="meeting-url"
                placeholder="https://meet.example.com/abc"
                aria-invalid={Boolean(errors.meetingUrl)}
                {...register('meetingUrl')}
              />
            </Field>
          </div>

          <Field
            label="Kapsam"
            htmlFor="meeting-scope"
            hint="Takım veya proje seçilirse yetki o kapsama göre denetlenir."
          >
            <Select value={watch('scope')} onValueChange={(value) => setValue('scope', value)}>
              <SelectTrigger id="meeting-scope">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">Kapsam yok</SelectItem>
                {projects.map((project) => (
                  <SelectItem key={project.id} value={`project:${project.id}`}>
                    Proje · {project.name}
                  </SelectItem>
                ))}
                {teams.map((team) => (
                  <SelectItem key={team.id} value={`team:${team.id}`}>
                    Takım · {team.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>

          <Field
            label={`Katılımcılar${attendeeIds.length > 0 ? ` (${attendeeIds.length})` : ''}`}
            error={errors.attendeeIds?.message}
            required
          >
            <div className="space-y-2">
              <div className="relative">
                <Search
                  className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-subtle-foreground"
                  aria-hidden="true"
                />
                <Input
                  value={attendeeSearch}
                  onChange={(event) => setAttendeeSearch(event.target.value)}
                  placeholder="Kişi ara…"
                  aria-label="Katılımcı ara"
                  className="h-9 pl-9"
                />
              </div>

              <div className="max-h-44 space-y-1 overflow-y-auto rounded-lg border border-border p-2">
                {filteredUsers.length === 0 ? (
                  <p className="px-1 py-3 text-center text-xs text-subtle-foreground">
                    Eşleşen kişi yok.
                  </p>
                ) : (
                  filteredUsers.map((user) => (
                    <label
                      key={user.id}
                      className="flex cursor-pointer items-center gap-2.5 rounded-md px-1 py-1.5 text-sm hover:bg-surface-raised"
                    >
                      <Checkbox
                        checked={attendeeIds.includes(user.id)}
                        onCheckedChange={(checked) => toggleAttendee(user.id, checked === true)}
                      />
                      <Avatar fullName={user.fullName} avatarUrl={user.avatarUrl} size="xs" />
                      <span className="truncate">{user.fullName}</span>
                      <span className="ml-auto shrink-0 truncate text-xs text-subtle-foreground">
                        {user.jobTitle ?? user.email}
                      </span>
                    </label>
                  ))
                )}
              </div>
            </div>
          </Field>

          <Field
            label="Açıklama"
            error={errors.description?.message}
            htmlFor="meeting-description"
          >
            <Textarea id="meeting-description" rows={3} {...register('description')} />
          </Field>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={create.isPending}>
              {create.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Toplantıyı oluştur
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
