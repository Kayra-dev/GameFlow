import type { LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { deadlineToneClasses, formatDueDate, getDeadlineTone } from '@/lib/dates';
import { cn } from '@/lib/utils';
import type { WorkItemSummary } from '@/types/api';
import { WorkItemPriority, workItemPriorityLabels, workItemStatusLabels } from '@/types/enums';

type TaskListCardProps = {
  title: string;
  icon: LucideIcon;
  tasks: WorkItemSummary[];
  isLoading?: boolean;
  emptyMessage: string;
};

const priorityVariant = {
  [WorkItemPriority.Lowest]: 'neutral',
  [WorkItemPriority.Low]: 'info',
  [WorkItemPriority.Medium]: 'warning',
  [WorkItemPriority.High]: 'warning',
  [WorkItemPriority.Critical]: 'danger',
} as const;

export function TaskListCard({
  title,
  icon: Icon,
  tasks,
  isLoading,
  emptyMessage,
}: TaskListCardProps) {
  return (
    <Card>
      <CardHeader className="flex-row items-center gap-2">
        <Icon className="size-4 text-subtle-foreground" aria-hidden="true" />
        <CardTitle>{title}</CardTitle>
        {tasks.length > 0 ? (
          <span className="ml-auto text-xs tabular-nums text-muted-foreground">
            {tasks.length}
          </span>
        ) : null}
      </CardHeader>

      <CardContent>
        {isLoading ? (
          <div className="space-y-2">
            <Skeleton className="h-14" />
            <Skeleton className="h-14" />
          </div>
        ) : tasks.length === 0 ? (
          <p className="py-2 text-sm text-muted-foreground">{emptyMessage}</p>
        ) : (
          <ul className="divide-y divide-border">
            {tasks.map((task) => {
              const tone = getDeadlineTone(task.dueDate, task.status === 6);

              return (
                <li key={task.id}>
                  <Link
                    to={`/gorevler/${task.key}`}
                    className="flex items-center gap-3 py-3 transition-colors hover:bg-surface-raised/50"
                  >
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="shrink-0 font-mono text-xs text-subtle-foreground">
                          {task.key}
                        </span>
                        <p className="truncate text-sm font-medium">{task.title}</p>
                      </div>
                      <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1">
                        <span className="text-xs text-muted-foreground">
                          {workItemStatusLabels[task.status]}
                        </span>
                        <span className={cn('text-xs font-medium', deadlineToneClasses[tone])}>
                          {formatDueDate(task.dueDate)}
                        </span>
                      </div>
                    </div>

                    <Badge variant={priorityVariant[task.priority]} className="hidden sm:flex">
                      {workItemPriorityLabels[task.priority]}
                    </Badge>

                    {task.assignee ? (
                      <Avatar
                        fullName={task.assignee.fullName}
                        avatarUrl={task.assignee.avatarUrl}
                        size="xs"
                      />
                    ) : null}
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
