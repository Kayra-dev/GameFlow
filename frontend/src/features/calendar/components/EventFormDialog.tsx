import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
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
import { Switch } from '@/components/ui/switch';
import { projectsApi } from '@/features/projects/api/projects-api';
import { teamsApi } from '@/features/teams/api/teams-api';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import { CalendarEventType, calendarEventTypeLabels } from '@/types/enums';

import { calendarApi } from '../api/calendar-api';
import { Field, toIsoOrNull, toLocalInputValue } from '@/components/common/form-field';

/** Etkinlik rengi seçenekleri; kolon/rapor paletiyle aynı tonlar. */
const colorOptions = [
  '#3B82F6',
  '#8B5CF6',
  '#22C55E',
  '#F59E0B',
  '#EF4444',
  '#06B6D4',
  '#EC4899',
  '#64748B',
];

/**
 * Kullanıcı tarafından oluşturulan takvim etkinlikleri. Görev son tarihleri ve
 * sprint tarihleri buradan gelmez; onlar kaynak kayıtlarından türetilir.
 */
const schema = z
  .object({
    title: z
      .string()
      .min(1, 'Etkinlik başlığı zorunludur.')
      .max(192, 'Başlık en fazla 192 karakter olabilir.'),
    description: z.string().max(2000, 'Açıklama en fazla 2000 karakter olabilir.').optional(),
    type: z.number().int(),
    startsAt: z.string().min(1, 'Başlangıç zamanı zorunludur.'),
    endsAt: z.string(),
    isAllDay: z.boolean(),
    colorHex: z.string(),
    scope: z.string(),
  })
  .refine(
    (values) => !values.endsAt || new Date(values.endsAt) >= new Date(values.startsAt),
    { path: ['endsAt'], message: 'Bitiş zamanı başlangıçtan önce olamaz.' },
  );

type FormValues = z.infer<typeof schema>;

type EventFormDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Izgarada tıklanan gün; form bu günle açılır. */
  defaultDate?: Date;
};

export function EventFormDialog({ open, onOpenChange, defaultDate }: EventFormDialogProps) {
  const queryClient = useQueryClient();

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
      type: CalendarEventType.Custom,
      startsAt: '',
      endsAt: '',
      isAllDay: false,
      colorHex: colorOptions[0],
      scope: 'none',
    },
  });

  // Dialog her açılışta seçili günün sabah 10'uyla başlatılır; kullanıcı
  // tarihi baştan yazmak zorunda kalmaz.
  useEffect(() => {
    if (!open) return;

    const start = new Date(defaultDate ?? new Date());
    start.setHours(10, 0, 0, 0);

    const end = new Date(start);
    end.setHours(11, 0, 0, 0);

    reset({
      title: '',
      description: '',
      type: CalendarEventType.Custom,
      startsAt: toLocalInputValue(start),
      endsAt: toLocalInputValue(end),
      isAllDay: false,
      colorHex: colorOptions[0],
      scope: 'none',
    });
  }, [open, defaultDate, reset]);

  const create = useMutation({
    mutationFn: (values: FormValues) => {
      const [scopeKind, scopeId] = values.scope.split(':');

      return calendarApi.createEvent({
        title: values.title,
        description: values.description || null,
        type: values.type as CalendarEventType,
        startsAt: new Date(values.startsAt).toISOString(),
        endsAt: values.isAllDay ? null : toIsoOrNull(values.endsAt),
        isAllDay: values.isAllDay,
        colorHex: values.colorHex,
        projectId: scopeKind === 'project' ? (scopeId ?? null) : null,
        teamId: scopeKind === 'team' ? (scopeId ?? null) : null,
      });
    },
    onSuccess: (created) => {
      void queryClient.invalidateQueries({ queryKey: ['calendar'] });
      toast.success(`“${created.title}” takvime eklendi.`);
      onOpenChange(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const isAllDay = watch('isAllDay');
  const colorHex = watch('colorHex');

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Yeni etkinlik</DialogTitle>
          <DialogDescription>
            Sürüm, kilometre taşı, oynanış testi gibi takvim kayıtları. Görev son
            tarihleri görevin kendisinden gelir, buradan girilmez.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => create.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <Field label="Başlık" error={errors.title?.message} htmlFor="event-title" required>
            <Input
              id="event-title"
              autoFocus
              placeholder="Dikey dilim sunumu"
              aria-invalid={Boolean(errors.title)}
              {...register('title')}
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Tür" htmlFor="event-type" required>
              <Select
                value={String(watch('type'))}
                onValueChange={(value) => setValue('type', Number(value))}
              >
                <SelectTrigger id="event-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.values(CalendarEventType).map((type) => (
                    <SelectItem key={type} value={String(type)}>
                      {calendarEventTypeLabels[type]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            <Field label="Kapsam" htmlFor="event-scope" hint="Boş bırakılırsa kişisel etkinliktir.">
              <Select
                value={watch('scope')}
                onValueChange={(value) => setValue('scope', value)}
              >
                <SelectTrigger id="event-scope">
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
          </div>

          <label className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border border-border p-3">
            <span className="text-sm">
              Tüm gün
              <span className="block text-xs text-muted-foreground">
                Saat yerine yalnızca gün gösterilir.
              </span>
            </span>
            <Switch
              checked={isAllDay}
              onCheckedChange={(checked) => setValue('isAllDay', checked)}
            />
          </label>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label={isAllDay ? 'Gün' : 'Başlangıç'}
              error={errors.startsAt?.message}
              htmlFor="event-start"
              required
            >
              <Input
                id="event-start"
                type="datetime-local"
                aria-invalid={Boolean(errors.startsAt)}
                {...register('startsAt')}
              />
            </Field>

            {!isAllDay ? (
              <Field label="Bitiş" error={errors.endsAt?.message} htmlFor="event-end">
                <Input
                  id="event-end"
                  type="datetime-local"
                  aria-invalid={Boolean(errors.endsAt)}
                  {...register('endsAt')}
                />
              </Field>
            ) : null}
          </div>

          <Field label="Renk">
            <div className="flex flex-wrap gap-2">
              {colorOptions.map((color) => (
                <button
                  key={color}
                  type="button"
                  onClick={() => setValue('colorHex', color)}
                  aria-label={`Renk ${color}`}
                  aria-pressed={colorHex === color}
                  className={cn(
                    'size-7 rounded-full outline-none transition-transform',
                    'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
                    'focus-visible:ring-offset-background',
                    colorHex === color
                      ? 'scale-110 ring-2 ring-foreground ring-offset-2 ring-offset-surface'
                      : 'hover:scale-105',
                  )}
                  style={{ backgroundColor: color }}
                />
              ))}
            </div>
          </Field>

          <Field label="Açıklama" error={errors.description?.message} htmlFor="event-description">
            <Textarea id="event-description" rows={3} {...register('description')} />
          </Field>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={create.isPending}>
              {create.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Etkinliği ekle
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
