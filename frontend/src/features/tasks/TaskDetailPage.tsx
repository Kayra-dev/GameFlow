import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  CalendarClock,
  Clock,
  History,
  Rocket,
  Target,
  TriangleAlert,
  Trash2,
  Users,
} from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';

import { Avatar } from '@/components/ui/avatar';
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
import { Label } from '@/components/ui/label';
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
import { deadlineToneClasses, formatDate, formatDueDate, formatRelative, getDeadlineTone } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import { isLeader, useAuthStore } from '@/stores/auth-store';
import {
  WorkItemPriority,
  WorkItemStatus,
  workItemPriorityLabels,
  workItemStatusLabels,
  workItemTypeLabels,
} from '@/types/enums';

import { workItemsApi } from './api/work-items-api';
import { TaskAttachments } from './components/TaskAttachments';
import { TaskChecklist } from './components/TaskChecklist';
import { TaskComments } from './components/TaskComments';
import { useState } from 'react';

const priorityVariant: Record<WorkItemPriority, 'neutral' | 'info' | 'warning' | 'danger'> = {
  [WorkItemPriority.Lowest]: 'neutral',
  [WorkItemPriority.Low]: 'info',
  [WorkItemPriority.Medium]: 'warning',
  [WorkItemPriority.High]: 'warning',
  [WorkItemPriority.Critical]: 'danger',
};

/** Görev detayı. Adres görev anahtarını taşır (örn. /gorevler/ODY-42). */
export function TaskDetailPage() {
  const { key = '' } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const { data: task, isLoading, isError } = useQuery({
    queryKey: queryKeys.workItems.byKey(key),
    queryFn: () => workItemsApi.byKey(key),
    enabled: Boolean(key),
  });

  // Atama listesi proje üyeleriyle sınırlı.
  const { data: project } = useQuery({
    queryKey: queryKeys.projects.detail(task?.projectId ?? ''),
    queryFn: () => projectsApi.detail(task!.projectId),
    enabled: Boolean(task?.projectId),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const changeStatus = useMutation({
    mutationFn: (status: WorkItemStatus) => workItemsApi.changeStatus(task!.id, status),
    onSuccess: (_data, status) => {
      invalidate();
      toast.success(`Durum “${workItemStatusLabels[status]}” olarak güncellendi.`);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const assign = useMutation({
    mutationFn: (assigneeId: string | null) => workItemsApi.assign(task!.id, assigneeId),
    onSuccess: () => {
      invalidate();
      toast.success('Atama güncellendi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: () => workItemsApi.remove(task!.id),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects.all });
      toast.success(`${task?.key} silindi.`);
      navigate(`/projeler/${task?.projectId}`);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-6xl space-y-4">
        <Skeleton className="h-24 rounded-card" />
        <div className="grid gap-4 lg:grid-cols-[1fr_20rem]">
          <Skeleton className="h-96 rounded-card" />
          <Skeleton className="h-72 rounded-card" />
        </div>
      </div>
    );
  }

  if (isError || !task) {
    return (
      <EmptyState
        icon={TriangleAlert}
        title="Görev bulunamadı"
        description={`“${key}” anahtarlı görev silinmiş olabilir veya erişim yetkiniz yok.`}
        action={
          <Button asChild variant="secondary">
            <Link to="/projeler">Projelere dön</Link>
          </Button>
        }
      />
    );
  }

  // Düzenleme yetkisi sunucuda da denetlenir; burada yalnızca arayüzü uyarlar.
  const isAssignee = task.assignee?.id === user?.id;
  const isReporter = task.reporter?.id === user?.id;
  const canEdit = isLeader(user) || isAssignee || isReporter;
  const canDelete = isLeader(user);

  const tone = getDeadlineTone(task.dueDate, task.status === WorkItemStatus.Done);

  return (
    <div className="mx-auto w-full max-w-6xl space-y-4">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to={`/projeler/${task.projectId}`}>
          <ArrowLeft aria-hidden="true" />
          {task.projectName}
        </Link>
      </Button>

      {/* Başlık kartı */}
      <Card>
        <CardContent className="space-y-3 pt-5">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-sm text-subtle-foreground">{task.key}</span>
            <Badge variant="neutral">{workItemTypeLabels[task.type]}</Badge>
            <Badge variant={priorityVariant[task.priority]}>
              {workItemPriorityLabels[task.priority]}
            </Badge>

            {task.isOverdue ? (
              <Badge variant="danger">
                <TriangleAlert aria-hidden="true" />
                Gecikmiş
              </Badge>
            ) : null}

            {canDelete ? (
              <Button
                variant="ghost"
                size="icon-sm"
                onClick={() => setDeleteOpen(true)}
                aria-label="Görevi sil"
                className="ml-auto"
              >
                <Trash2 className="text-danger" aria-hidden="true" />
              </Button>
            ) : null}
          </div>

          <h1 className="text-xl font-semibold tracking-tight">{task.title}</h1>

          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-muted-foreground">
            <span>
              {task.reporter ? `${task.reporter.fullName} oluşturdu` : 'Oluşturan bilinmiyor'} ·{' '}
              {formatRelative(task.createdAt)}
            </span>
            {task.updatedAt ? <span>Güncellendi {formatRelative(task.updatedAt)}</span> : null}
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-[1fr_20rem]">
        {/* Sol kolon: içerik */}
        <div className="space-y-4">
          <Card>
            <CardContent className="space-y-5 pt-5">
              <section className="space-y-2">
                <h2 className="text-sm font-semibold">Açıklama</h2>
                {task.description ? (
                  <p className="text-sm leading-relaxed whitespace-pre-wrap text-foreground">
                    {task.description}
                  </p>
                ) : (
                  <p className="text-sm text-muted-foreground">Açıklama girilmemiş.</p>
                )}
              </section>

              <hr className="border-border" />

              <TaskChecklist
                workItemId={task.id}
                items={task.checklistItems}
                canEdit={canEdit}
              />

              <hr className="border-border" />

              <TaskAttachments
                workItemId={task.id}
                attachments={task.attachments}
                canEdit={canEdit}
              />
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-5">
              <TaskComments
                workItemId={task.id}
                comments={task.comments}
                canModerate={isLeader(user)}
              />
            </CardContent>
          </Card>
        </div>

        {/* Sağ kolon: üst veriler ve geçmiş */}
        <div className="space-y-4">
          <Card>
            <CardContent className="space-y-4 pt-5">
              <div className="space-y-1.5">
                <Label htmlFor="detail-status">Durum</Label>
                <Select
                  value={String(task.status)}
                  onValueChange={(value) => changeStatus.mutate(Number(value) as WorkItemStatus)}
                  disabled={!canEdit || changeStatus.isPending}
                >
                  <SelectTrigger id="detail-status">
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

              <div className="space-y-1.5">
                <Label htmlFor="detail-assignee">Atanan kişi</Label>
                <Select
                  value={task.assignee?.id ?? 'none'}
                  onValueChange={(value) => assign.mutate(value === 'none' ? null : value)}
                  disabled={!canEdit || assign.isPending}
                >
                  <SelectTrigger id="detail-assignee">
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
              </div>

              <hr className="border-border" />

              <dl className="space-y-3 text-sm">
                <MetaRow icon={CalendarClock} label="Son teslim">
                  {task.dueDate ? (
                    <span className={cn('font-medium', deadlineToneClasses[tone])}>
                      {formatDate(task.dueDate)} · {formatDueDate(task.dueDate)}
                    </span>
                  ) : (
                    <span className="text-muted-foreground">Yok</span>
                  )}
                </MetaRow>

                {task.startDate ? (
                  <MetaRow icon={CalendarClock} label="Başlangıç">
                    {formatDate(task.startDate)}
                  </MetaRow>
                ) : null}

                {task.teamName ? (
                  <MetaRow icon={Users} label="Takım">
                    {task.teamName}
                  </MetaRow>
                ) : null}

                {task.sprintName ? (
                  <MetaRow icon={Rocket} label="Sprint">
                    {task.sprintName}
                  </MetaRow>
                ) : null}

                {task.storyPoints !== null ? (
                  <MetaRow icon={Target} label="Puan">
                    {task.storyPoints}
                  </MetaRow>
                ) : null}

                {task.estimatedHours !== null || task.loggedHours !== null ? (
                  <MetaRow icon={Clock} label="Süre">
                    {task.loggedHours ?? 0} / {task.estimatedHours ?? '—'} saat
                  </MetaRow>
                ) : null}
              </dl>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="space-y-3 pt-5">
              <h2 className="flex items-center gap-2 text-sm font-semibold">
                <History className="size-4 text-subtle-foreground" aria-hidden="true" />
                Aktivite geçmişi
              </h2>

              {task.activities.length === 0 ? (
                <p className="text-sm text-muted-foreground">Kayıt yok.</p>
              ) : (
                <ol className="space-y-3">
                  {task.activities.map((activity) => (
                    <li key={activity.id} className="flex gap-2.5">
                      <Avatar
                        fullName={activity.actor?.fullName ?? 'Sistem'}
                        avatarUrl={activity.actor?.avatarUrl}
                        size="xs"
                      />
                      <div className="min-w-0 flex-1">
                        <p className="text-xs leading-relaxed">{activity.description}</p>
                        <p className="text-[11px] text-subtle-foreground">
                          {formatRelative(activity.createdAt)}
                        </p>
                      </div>
                    </li>
                  ))}
                </ol>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Görevi sil</DialogTitle>
            <DialogDescription>
              <strong className="text-foreground">{task.key}</strong> silinecek. Alt görevleri
              varsa onlar da silinir. Yorumlar ve aktivite geçmişi korunur.
            </DialogDescription>
          </DialogHeader>

          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteOpen(false)}>
              Vazgeç
            </Button>
            <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
              <Trash2 aria-hidden="true" />
              Sil
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

/** Sağ kolondaki ikon + etiket + değer satırı. */
function MetaRow({
  icon: Icon,
  label,
  children,
}: {
  icon: typeof CalendarClock;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex items-start gap-2.5">
      <Icon className="mt-0.5 size-3.5 shrink-0 text-subtle-foreground" aria-hidden="true" />
      <dt className="min-w-20 text-xs text-muted-foreground">{label}</dt>
      <dd className="min-w-0 flex-1 text-right text-xs">{children}</dd>
    </div>
  );
}
