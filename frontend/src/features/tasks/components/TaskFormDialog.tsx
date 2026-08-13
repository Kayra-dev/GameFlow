import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Plus, X } from 'lucide-react';
import { useState } from 'react';
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
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { projectsApi } from '@/features/projects/api/projects-api';
import { teamsApi } from '@/features/teams/api/teams-api';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import {
  WorkItemPriority,
  WorkItemStatus,
  WorkItemType,
  workItemPriorityLabels,
  workItemStatusLabels,
  workItemTypeLabels,
} from '@/types/enums';

import { workItemsApi } from '../api/work-items-api';

const taskSchema = z
  .object({
    title: z
      .string()
      .min(3, 'Görev başlığı en az 3 karakter olmalıdır.')
      .max(256, 'Görev başlığı en fazla 256 karakter olabilir.'),
    description: z.string().max(8000, 'Açıklama en fazla 8000 karakter olabilir.').optional(),
    status: z.number().int(),
    priority: z.number().int(),
    type: z.number().int(),
    assigneeId: z.string().optional(),
    teamId: z.string().optional(),
    startDate: z.string().optional(),
    dueDate: z.string().optional(),
    estimatedHours: z.string().optional(),
    storyPoints: z.string().optional(),
  })
  .refine((values) => !values.startDate || !values.dueDate || values.dueDate >= values.startDate, {
    message: 'Son teslim tarihi başlangıç tarihinden önce olamaz.',
    path: ['dueDate'],
  });

type TaskFormValues = z.infer<typeof taskSchema>;

type TaskFormDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectId: string;
  /** Formun açıldığı kolon; yeni görev bu durumla oluşturulur. */
  initialStatus?: WorkItemStatus;
};

function toIsoDate(value: string | undefined): string | null {
  return value ? new Date(`${value}T00:00:00Z`).toISOString() : null;
}

/** Boş dizeyi null'a, sayısal metni sayıya çevirir. */
function toNumberOrNull(value: string | undefined): number | null {
  if (!value?.trim()) return null;

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

export function TaskFormDialog({
  open,
  onOpenChange,
  projectId,
  initialStatus = WorkItemStatus.Pending,
}: TaskFormDialogProps) {
  const queryClient = useQueryClient();
  const [checklist, setChecklist] = useState<string[]>([]);
  const [checklistDraft, setChecklistDraft] = useState('');

  // Görevler yalnızca proje üyelerine atanabildiği için atanacak kişiler
  // proje ayrıntısından okunur, tüm kullanıcı listesinden değil.
  const { data: project } = useQuery({
    queryKey: queryKeys.projects.detail(projectId),
    queryFn: () => projectsApi.detail(projectId),
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
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskSchema),
    defaultValues: {
      title: '',
      description: '',
      status: initialStatus,
      priority: WorkItemPriority.Medium,
      type: WorkItemType.Task,
      assigneeId: 'none',
      teamId: 'none',
      startDate: '',
      dueDate: '',
      estimatedHours: '',
      storyPoints: '',
    },
  });

  const create = useMutation({
    mutationFn: (values: TaskFormValues) =>
      workItemsApi.create({
        projectId,
        title: values.title,
        description: values.description || null,
        status: values.status as WorkItemStatus,
        priority: values.priority as WorkItemPriority,
        type: values.type as WorkItemType,
        assigneeId: values.assigneeId === 'none' ? null : values.assigneeId,
        teamId: values.teamId === 'none' ? null : values.teamId,
        sprintId: null,
        parentId: null,
        startDate: toIsoDate(values.startDate),
        dueDate: toIsoDate(values.dueDate),
        estimatedHours: toNumberOrNull(values.estimatedHours),
        storyPoints: toNumberOrNull(values.storyPoints),
        labelIds: [],
        checklistItems: checklist,
      }),
    onSuccess: (created) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects.all });
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });

      toast.success(`${created.key} oluşturuldu.`);
      reset();
      setChecklist([]);
      setChecklistDraft('');
      onOpenChange(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const addChecklistItem = () => {
    const text = checklistDraft.trim();

    if (text) {
      setChecklist((previous) => [...previous, text]);
      setChecklistDraft('');
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent key={initialStatus} className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Yeni görev</DialogTitle>
          <DialogDescription>
            Görev, {project?.name ?? 'proje'} altında oluşturulur ve{' '}
            {project?.key ?? '—'} anahtarıyla numaralanır.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => create.mutate(values))}
          noValidate
          className="space-y-4"
        >
          <div className="space-y-1.5">
            <Label htmlFor="task-title">
              Başlık<span className="ml-0.5 text-danger">*</span>
            </Label>
            <Input
              id="task-title"
              autoFocus
              placeholder="Zıplama mekaniğindeki çift zıplama hatası"
              aria-invalid={Boolean(errors.title)}
              {...register('title')}
            />
            {errors.title ? (
              <p role="alert" className="text-xs text-danger">
                {errors.title.message}
              </p>
            ) : null}
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-1.5">
              <Label htmlFor="task-type">Tür</Label>
              <Select
                value={String(watch('type'))}
                onValueChange={(value) => setValue('type', Number(value))}
              >
                <SelectTrigger id="task-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(workItemTypeLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-priority">Öncelik</Label>
              <Select
                value={String(watch('priority'))}
                onValueChange={(value) => setValue('priority', Number(value))}
              >
                <SelectTrigger id="task-priority">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(workItemPriorityLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-status">Durum</Label>
              <Select
                value={String(watch('status'))}
                onValueChange={(value) => setValue('status', Number(value))}
              >
                <SelectTrigger id="task-status">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(workItemStatusLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="task-assignee">Atanan kişi</Label>
              <Select
                value={watch('assigneeId') ?? 'none'}
                onValueChange={(value) => setValue('assigneeId', value)}
              >
                <SelectTrigger id="task-assignee">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Atanmadı</SelectItem>
                  {project?.members.map((member) => (
                    <SelectItem key={member.user.id} value={member.user.id}>
                      {member.user.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-subtle-foreground">
                Yalnızca proje üyeleri listelenir.
              </p>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-team">Takım</Label>
              <Select
                value={watch('teamId') ?? 'none'}
                onValueChange={(value) => setValue('teamId', value)}
              >
                <SelectTrigger id="task-team">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Takım yok</SelectItem>
                  {teams.map((team) => (
                    <SelectItem key={team.id} value={team.id}>
                      {team.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="task-start">Başlangıç</Label>
              <Input id="task-start" type="date" {...register('startDate')} />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-due">Son teslim</Label>
              <Input
                id="task-due"
                type="date"
                aria-invalid={Boolean(errors.dueDate)}
                {...register('dueDate')}
              />
              {errors.dueDate ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.dueDate.message}
                </p>
              ) : null}
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="task-hours">Tahmini süre (saat)</Label>
              <Input
                id="task-hours"
                type="number"
                min={0}
                max={9999}
                step="0.5"
                placeholder="6.5"
                {...register('estimatedHours')}
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-points">Puan</Label>
              <Input
                id="task-points"
                type="number"
                min={0}
                max={1000}
                placeholder="5"
                {...register('storyPoints')}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="task-checklist">Kontrol listesi</Label>

            {checklist.length > 0 ? (
              <ul className="space-y-1.5 rounded-lg border border-border p-2">
                {checklist.map((item, index) => (
                  <li key={`${item}-${index}`} className="flex items-center gap-2 text-sm">
                    <span className="min-w-0 flex-1 truncate">{item}</span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      aria-label={`"${item}" maddesini kaldır`}
                      onClick={() =>
                        setChecklist((previous) => previous.filter((_, i) => i !== index))
                      }
                    >
                      <X aria-hidden="true" />
                    </Button>
                  </li>
                ))}
              </ul>
            ) : null}

            <div className="flex gap-2">
              <Input
                id="task-checklist"
                value={checklistDraft}
                onChange={(event) => setChecklistDraft(event.target.value)}
                onKeyDown={(event) => {
                  // Enter formu göndermek yerine madde ekler.
                  if (event.key === 'Enter') {
                    event.preventDefault();
                    addChecklistItem();
                  }
                }}
                placeholder="Madde ekle ve Enter'a bas"
              />
              <Button
                type="button"
                variant="secondary"
                size="icon"
                onClick={addChecklistItem}
                aria-label="Kontrol listesine ekle"
              >
                <Plus aria-hidden="true" />
              </Button>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="task-description">Açıklama</Label>
            <Textarea
              id="task-description"
              rows={3}
              placeholder="Hata, oyuncu havada ikinci kez zıpladığında ivmenin sıfırlanmamasından kaynaklanıyor."
              {...register('description')}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={create.isPending}>
              {create.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Görevi oluştur
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
