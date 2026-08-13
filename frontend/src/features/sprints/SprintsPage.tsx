import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CircleCheck, Loader2, Play, Plus, Rocket, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
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
import { projectsApi } from '@/features/projects/api/projects-api';
import { getErrorMessage } from '@/lib/api-client';
import { formatDate } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { isLeader, useAuthStore } from '@/stores/auth-store';
import type { SprintSummary } from '@/types/api';
import { SprintStatus, sprintStatusLabels } from '@/types/enums';

import { sprintsApi } from './api/sprints-api';

const statusVariant: Record<SprintStatus, 'neutral' | 'primary' | 'success' | 'danger'> = {
  [SprintStatus.Planned]: 'neutral',
  [SprintStatus.Active]: 'primary',
  [SprintStatus.Completed]: 'success',
  [SprintStatus.Cancelled]: 'danger',
};

const sprintSchema = z
  .object({
    projectId: z.string().min(1, 'Proje seçilmelidir.'),
    name: z
      .string()
      .min(2, 'Sprint adı en az 2 karakter olmalıdır.')
      .max(128, 'Sprint adı en fazla 128 karakter olabilir.'),
    goal: z.string().max(1024, 'Hedef en fazla 1024 karakter olabilir.').optional(),
    startDate: z.string().min(1, 'Başlangıç tarihi zorunludur.'),
    endDate: z.string().min(1, 'Bitiş tarihi zorunludur.'),
  })
  .refine((values) => values.endDate > values.startDate, {
    message: 'Bitiş tarihi başlangıçtan sonra olmalıdır.',
    path: ['endDate'],
  })
  .refine(
    (values) => {
      const days =
        (new Date(values.endDate).getTime() - new Date(values.startDate).getTime()) / 86_400_000;
      return days >= 1 && days <= 60;
    },
    { message: 'Sprint süresi 1 ile 60 gün arasında olmalıdır.', path: ['endDate'] },
  );

type SprintFormValues = z.infer<typeof sprintSchema>;

export function SprintsPage() {
  const [formOpen, setFormOpen] = useState(false);
  const [completing, setCompleting] = useState<SprintSummary | null>(null);
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const canManage = isLeader(user);

  const { data: projects } = useQuery({
    queryKey: queryKeys.projects.list({}),
    queryFn: () => projectsApi.list(),
  });

  const { data: sprints, isLoading } = useQuery({
    queryKey: queryKeys.sprints.list({}),
    queryFn: () => sprintsApi.list(),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.sprints.all });
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects.all });
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const start = useMutation({
    mutationFn: (id: string) => sprintsApi.start(id),
    onSuccess: () => {
      invalidate();
      toast.success('Sprint başlatıldı.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (id: string) => sprintsApi.remove(id),
    onSuccess: () => {
      invalidate();
      toast.success('Sprint silindi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <div className="mx-auto w-full max-w-5xl space-y-5">
      <header className="flex flex-wrap items-center gap-3">
        <div className="flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">Sprintler</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Bir projede aynı anda yalnızca tek sprint aktif olabilir.
          </p>
        </div>

        {canManage && projects && projects.length > 0 ? (
          <Button onClick={() => setFormOpen(true)}>
            <Plus aria-hidden="true" />
            Yeni sprint
          </Button>
        ) : null}
      </header>

      {isLoading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-32 rounded-card" />
          ))}
        </div>
      ) : sprints?.length === 0 ? (
        <Card>
          <EmptyState
            icon={Rocket}
            title="Sprint yok"
            description={
              projects && projects.length === 0
                ? 'Sprint oluşturmak için önce bir proje gerekiyor.'
                : 'İlk sprintinizi oluşturarak işleri zaman dilimlerine bölün.'
            }
            action={
              canManage && projects && projects.length > 0 ? (
                <Button variant="secondary" onClick={() => setFormOpen(true)}>
                  <Plus aria-hidden="true" />
                  Sprint oluştur
                </Button>
              ) : null
            }
          />
        </Card>
      ) : (
        <ul className="space-y-3">
          {sprints?.map((sprint) => (
            <li key={sprint.id}>
              <Card>
                <CardContent className="space-y-3 pt-5">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-base font-semibold">{sprint.name}</h2>
                    <Badge variant={statusVariant[sprint.status]}>
                      {sprintStatusLabels[sprint.status]}
                    </Badge>

                    {canManage ? (
                      <span className="ml-auto flex items-center gap-1.5">
                        {sprint.status === SprintStatus.Planned ? (
                          <>
                            <Button
                              size="sm"
                              onClick={() => start.mutate(sprint.id)}
                              disabled={start.isPending}
                            >
                              <Play aria-hidden="true" />
                              Başlat
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              onClick={() => remove.mutate(sprint.id)}
                              aria-label="Sprinti sil"
                            >
                              <Trash2 className="text-danger" aria-hidden="true" />
                            </Button>
                          </>
                        ) : sprint.status === SprintStatus.Active ? (
                          <Button size="sm" variant="secondary" onClick={() => setCompleting(sprint)}>
                            <CircleCheck aria-hidden="true" />
                            Tamamla
                          </Button>
                        ) : null}
                      </span>
                    ) : null}
                  </div>

                  <p className="text-xs text-muted-foreground">
                    {formatDate(sprint.startDate)} – {formatDate(sprint.endDate)}
                  </p>

                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-muted-foreground">
                        {sprint.completedTaskCount}/{sprint.taskCount} görev tamamlandı
                      </span>
                      <span className="tabular-nums">%{sprint.progressPercent}</span>
                    </div>
                    <Progress value={sprint.progressPercent} className="h-2" />
                  </div>
                </CardContent>
              </Card>
            </li>
          ))}
        </ul>
      )}

      <SprintFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        projects={projects ?? []}
        onCreated={invalidate}
      />

      <CompleteSprintDialog
        sprint={completing}
        onClose={() => setCompleting(null)}
        onCompleted={invalidate}
      />
    </div>
  );
}

function SprintFormDialog({
  open,
  onOpenChange,
  projects,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projects: { id: string; name: string }[];
  onCreated: () => void;
}) {
  const today = new Date().toISOString().slice(0, 10);
  const twoWeeksLater = new Date(Date.now() + 14 * 86_400_000).toISOString().slice(0, 10);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<SprintFormValues>({
    resolver: zodResolver(sprintSchema),
    defaultValues: {
      projectId: projects[0]?.id ?? '',
      name: '',
      goal: '',
      startDate: today,
      endDate: twoWeeksLater,
    },
  });

  const create = useMutation({
    mutationFn: (values: SprintFormValues) =>
      sprintsApi.create({
        projectId: values.projectId,
        name: values.name,
        goal: values.goal || null,
        startDate: new Date(`${values.startDate}T00:00:00Z`).toISOString(),
        endDate: new Date(`${values.endDate}T00:00:00Z`).toISOString(),
      }),
    onSuccess: (created) => {
      onCreated();
      toast.success(`${created.name} oluşturuldu.`);
      reset();
      onOpenChange(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Yeni sprint</DialogTitle>
          <DialogDescription>
            Sprint planlanmış olarak oluşturulur; hazır olduğunuzda başlatırsınız.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => create.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <div className="space-y-1.5">
            <Label htmlFor="sprint-project">Proje</Label>
            <Select
              value={watch('projectId')}
              onValueChange={(value) => setValue('projectId', value)}
            >
              <SelectTrigger id="sprint-project">
                <SelectValue placeholder="Proje seçin" />
              </SelectTrigger>
              <SelectContent>
                {projects.map((project) => (
                  <SelectItem key={project.id} value={project.id}>
                    {project.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="sprint-name">
              Sprint adı<span className="ml-0.5 text-danger">*</span>
            </Label>
            <Input
              id="sprint-name"
              autoFocus
              placeholder="Sprint 1 · Zıplama ve ses"
              aria-invalid={Boolean(errors.name)}
              {...register('name')}
            />
            {errors.name ? (
              <p role="alert" className="text-xs text-danger">
                {errors.name.message}
              </p>
            ) : null}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="sprint-start">Başlangıç</Label>
              <Input id="sprint-start" type="date" {...register('startDate')} />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="sprint-end">Bitiş</Label>
              <Input
                id="sprint-end"
                type="date"
                aria-invalid={Boolean(errors.endDate)}
                {...register('endDate')}
              />
              {errors.endDate ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.endDate.message}
                </p>
              ) : null}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="sprint-goal">Sprint hedefi</Label>
            <Textarea
              id="sprint-goal"
              rows={2}
              placeholder="Oynanış hatalarını kapatıp ilk ses geçişini tamamlamak."
              {...register('goal')}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={create.isPending}>
              {create.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Sprinti oluştur
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function CompleteSprintDialog({
  sprint,
  onClose,
  onCompleted,
}: {
  sprint: SprintSummary | null;
  onClose: () => void;
  onCompleted: () => void;
}) {
  const [notes, setNotes] = useState('');

  const complete = useMutation({
    mutationFn: () => sprintsApi.complete(sprint!.id, notes || undefined),
    onSuccess: (report) => {
      onCompleted();
      toast.success(
        `${report.sprintName} tamamlandı · %${report.progressPercent} başarı ` +
          `(${report.completedTaskCount}/${report.totalTaskCount} görev)`,
      );
      setNotes('');
      onClose();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remaining = sprint ? sprint.taskCount - sprint.completedTaskCount : 0;

  return (
    <Dialog open={Boolean(sprint)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Sprinti tamamla</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{sprint?.name}</strong> kapatılacak.
            {remaining > 0
              ? ` Tamamlanmayan ${remaining} görev backlog'a döner.`
              : ' Tüm görevler tamamlanmış.'}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-1.5">
          <Label htmlFor="retro-notes">Retrospektif notları</Label>
          <Textarea
            id="retro-notes"
            rows={4}
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            placeholder="Neler iyi gitti, neler sarktı?"
          />
        </div>

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Vazgeç
          </Button>
          <Button onClick={() => complete.mutate()} disabled={complete.isPending}>
            {complete.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
            Tamamla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
