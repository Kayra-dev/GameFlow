import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  CalendarDays,
  Gamepad2,
  Monitor,
  Plus,
  SquareKanban,
  TriangleAlert,
  Users,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { Progress } from '@/components/ui/progress';
import { Skeleton } from '@/components/ui/skeleton';
import { Tooltip } from '@/components/ui/tooltip';
import { KanbanBoard } from '@/features/tasks/components/KanbanBoard';
import { TaskFormDialog } from '@/features/tasks/components/TaskFormDialog';
import { formatDate } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { isLeader, useAuthStore } from '@/stores/auth-store';
import type { WorkItemSummary } from '@/types/api';
import { ProjectStatus, WorkItemStatus, projectStatusLabels } from '@/types/enums';

import { projectsApi } from './api/projects-api';

const statusVariant: Record<ProjectStatus, 'neutral' | 'primary' | 'info' | 'success' | 'warning'> =
  {
    [ProjectStatus.Planning]: 'neutral',
    [ProjectStatus.InDevelopment]: 'primary',
    [ProjectStatus.Alpha]: 'info',
    [ProjectStatus.Beta]: 'info',
    [ProjectStatus.Released]: 'success',
    [ProjectStatus.OnHold]: 'warning',
    [ProjectStatus.Archived]: 'neutral',
  };

/** Proje detayı: üst bilgi kartı + kanban panosu. */
export function ProjectDetailPage() {
  const { projectId = '' } = useParams();
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);

  const [taskFormOpen, setTaskFormOpen] = useState(false);
  const [newTaskStatus, setNewTaskStatus] = useState<WorkItemStatus>(WorkItemStatus.Pending);

  const { data: project, isLoading, isError } = useQuery({
    queryKey: queryKeys.projects.detail(projectId),
    queryFn: () => projectsApi.detail(projectId),
    enabled: Boolean(projectId),
  });

  // Görev oluşturma yetkisi sunucuda da denetlenir; burada yalnızca
  // düğmeyi gizlemek için kullanılır.
  const canCreateTask = isLeader(user);

  const openTaskForm = (status: WorkItemStatus) => {
    setNewTaskStatus(status);
    setTaskFormOpen(true);
  };

  const handleOpenTask = (task: WorkItemSummary) => {
    navigate(`/gorevler/${task.key}`);
  };

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-[110rem] space-y-6">
        <Skeleton className="h-32 rounded-card" />
        <Skeleton className="h-96 rounded-card" />
      </div>
    );
  }

  if (isError || !project) {
    return (
      <EmptyState
        icon={TriangleAlert}
        title="Proje bulunamadı"
        description="Proje silinmiş olabilir veya erişim yetkiniz yok."
        action={
          <Button asChild variant="secondary">
            <Link to="/projeler">Projelere dön</Link>
          </Button>
        }
      />
    );
  }

  return (
    <div className="mx-auto w-full max-w-[110rem] space-y-5">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/projeler">
          <ArrowLeft aria-hidden="true" />
          Projeler
        </Link>
      </Button>

      <header className="rounded-card border border-border bg-surface p-5">
        {/*
          Mobilde üye avatarları ve düğme kendi satırına iner; aynı satırda
          kalırsa başlığın üstüne biniyorlardı.
        */}
        <div className="flex flex-wrap items-start gap-4">
          <div
            className="grid size-12 shrink-0 place-items-center rounded-xl font-mono text-sm font-semibold text-white"
            style={{ backgroundColor: project.colorHex }}
            aria-hidden="true"
          >
            {project.key.slice(0, 3)}
          </div>

          <div className="min-w-0 flex-1 basis-[min(100%,16rem)]">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-xl font-semibold tracking-tight">{project.name}</h1>
              <Badge variant={statusVariant[project.status]}>
                {projectStatusLabels[project.status]}
              </Badge>
            </div>

            {project.description ? (
              <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
                {project.description}
              </p>
            ) : null}

            <div className="mt-3 flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-muted-foreground [&>span]:whitespace-nowrap">
              {project.genre ? (
                <span className="flex items-center gap-1.5">
                  <Gamepad2 className="size-3.5" aria-hidden="true" />
                  {project.genre}
                </span>
              ) : null}

              {project.platforms ? (
                <span className="flex items-center gap-1.5">
                  <Monitor className="size-3.5" aria-hidden="true" />
                  {project.platforms}
                </span>
              ) : null}

              {project.targetReleaseDate ? (
                <span className="flex items-center gap-1.5">
                  <CalendarDays className="size-3.5" aria-hidden="true" />
                  Hedef çıkış: {formatDate(project.targetReleaseDate)}
                </span>
              ) : null}

              <span className="flex items-center gap-1.5">
                <SquareKanban className="size-3.5" aria-hidden="true" />
                {project.completedTaskCount}/{project.taskCount} görev
              </span>

              {project.overdueTaskCount > 0 ? (
                <span className="flex items-center gap-1.5 text-danger">
                  <TriangleAlert className="size-3.5" aria-hidden="true" />
                  {project.overdueTaskCount} geciken
                </span>
              ) : null}
            </div>
          </div>

          <div className="flex w-full items-center gap-3 sm:w-auto">
            {/* Üye avatarları; kalabalıksa kalanı sayı olarak gösterilir. */}
            <div className="flex items-center">
              {project.members.slice(0, 5).map((member, index) => (
                <Tooltip key={member.id} content={member.user.fullName}>
                  <span className={index > 0 ? '-ml-2' : undefined}>
                    <Avatar
                      fullName={member.user.fullName}
                      avatarUrl={member.user.avatarUrl}
                      size="sm"
                      className="ring-2 ring-surface"
                    />
                  </span>
                </Tooltip>
              ))}
              {project.members.length > 5 ? (
                <span className="-ml-2 grid size-8 place-items-center rounded-full bg-surface-raised text-[11px] font-medium ring-2 ring-surface">
                  +{project.members.length - 5}
                </span>
              ) : (
                <span className="ml-2 flex items-center gap-1 text-xs text-muted-foreground">
                  <Users className="size-3.5" aria-hidden="true" />
                  {project.memberCount}
                </span>
              )}
            </div>

            {canCreateTask ? (
              <Button
                onClick={() => openTaskForm(WorkItemStatus.Pending)}
                className="ml-auto sm:ml-0"
              >
                <Plus aria-hidden="true" />
                Yeni görev
              </Button>
            ) : null}
          </div>
        </div>

        <div className="mt-4 space-y-1.5">
          <div className="flex items-center justify-between text-xs">
            <span className="text-muted-foreground">Proje ilerlemesi</span>
            <span className="tabular-nums">%{project.progressPercent}</span>
          </div>
          <Progress value={project.progressPercent} color={project.colorHex} className="h-2" />
        </div>

        {project.activeSprint ? (
          <Link
            to={`/sprintler/${project.activeSprint.id}`}
            className="mt-4 flex items-center gap-3 rounded-lg border border-border p-3 transition-colors hover:border-border-strong"
          >
            <Badge variant="primary">Aktif sprint</Badge>
            <span className="min-w-0 flex-1 truncate text-sm font-medium">
              {project.activeSprint.name}
            </span>
            <span className="shrink-0 text-xs tabular-nums text-muted-foreground">
              %{project.activeSprint.progressPercent}
            </span>
          </Link>
        ) : null}
      </header>

      <KanbanBoard
        projectId={projectId}
        onOpenTask={handleOpenTask}
        onAddTask={openTaskForm}
        canCreate={canCreateTask}
      />

      <TaskFormDialog
        open={taskFormOpen}
        onOpenChange={setTaskFormOpen}
        projectId={projectId}
        initialStatus={newTaskStatus}
      />
    </div>
  );
}
