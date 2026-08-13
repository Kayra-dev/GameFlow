import { useQuery } from '@tanstack/react-query';
import { Search, SquareKanban } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useDebouncedValue } from '@/hooks/use-debounced-value';
import { deadlineToneClasses, formatDueDate, getDeadlineTone } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import {
  WorkItemPriority,
  WorkItemStatus,
  workItemPriorityLabels,
  workItemStatusLabels,
  workItemTypeLabels,
} from '@/types/enums';

import { workItemsApi } from './api/work-items-api';

const priorityVariant: Record<WorkItemPriority, 'neutral' | 'info' | 'warning' | 'danger'> = {
  [WorkItemPriority.Lowest]: 'neutral',
  [WorkItemPriority.Low]: 'info',
  [WorkItemPriority.Medium]: 'warning',
  [WorkItemPriority.High]: 'warning',
  [WorkItemPriority.Critical]: 'danger',
};

/** "Görevlerim": kullanıcıya atanmış görevlerin filtrelenebilir listesi. */
export function MyTasksPage() {
  const [scope, setScope] = useState<'mine' | 'all'>('mine');
  const [statusFilter, setStatusFilter] = useState('active');
  const [search, setSearch] = useState('');

  const debouncedSearch = useDebouncedValue(search, 300);

  const params = {
    page: 1,
    pageSize: 50,
    onlyMine: scope === 'mine',
    search: debouncedSearch || undefined,
    onlyActive: statusFilter === 'active',
    onlyOverdue: statusFilter === 'overdue',
    status:
      statusFilter === 'active' || statusFilter === 'overdue'
        ? undefined
        : (Number(statusFilter) as WorkItemStatus),
    sortBy: 2, // Son teslim tarihi
    sortDescending: false,
  };

  const { data, isLoading } = useQuery({
    queryKey: queryKeys.workItems.list(params),
    queryFn: () => workItemsApi.list(params),
  });

  return (
    <div className="mx-auto w-full max-w-5xl space-y-5">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Görevlerim</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Size atanmış görevler, en yakın teslim tarihine göre sıralı.
        </p>
      </header>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <Tabs value={scope} onValueChange={(value) => setScope(value as 'mine' | 'all')}>
          <TabsList>
            <TabsTrigger value="mine">Bana atanan</TabsTrigger>
            <TabsTrigger value="all">Tümü</TabsTrigger>
          </TabsList>
        </Tabs>

        <div className="relative flex-1 sm:max-w-xs">
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-subtle-foreground"
            aria-hidden="true"
          />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Başlık veya anahtar ara…"
            aria-label="Görev ara"
            className="pl-9"
          />
        </div>

        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="sm:w-44" aria-label="Duruma göre filtrele">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="active">Açık görevler</SelectItem>
            <SelectItem value="overdue">Geciken</SelectItem>
            {Object.entries(workItemStatusLabels).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }, (_, index) => (
            <Skeleton key={index} className="h-16 rounded-card" />
          ))}
        </div>
      ) : data && data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={SquareKanban}
            title="Görev bulunamadı"
            description={
              scope === 'mine'
                ? 'Size atanmış, bu filtreye uyan görev yok.'
                : 'Bu filtreye uyan görev yok.'
            }
          />
        </Card>
      ) : (
        <Card className="overflow-hidden">
          <ul className="divide-y divide-border">
            {data?.items.map((task) => {
              const tone = getDeadlineTone(task.dueDate, task.status === WorkItemStatus.Done);

              return (
                <li key={task.id}>
                  <Link
                    to={`/gorevler/${task.key}`}
                    className="flex items-center gap-3 px-4 py-3 transition-colors hover:bg-surface-raised/40"
                  >
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="shrink-0 font-mono text-xs text-subtle-foreground">
                          {task.key}
                        </span>
                        <p
                          className={cn(
                            'truncate text-sm font-medium',
                            task.status === WorkItemStatus.Done &&
                              'text-muted-foreground line-through',
                          )}
                        >
                          {task.title}
                        </p>
                      </div>
                      <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
                        <span className="text-muted-foreground">{task.projectName}</span>
                        <span className="text-muted-foreground">
                          {workItemStatusLabels[task.status]}
                        </span>
                        <span className="text-subtle-foreground">
                          {workItemTypeLabels[task.type]}
                        </span>
                        <span className={cn('font-medium', deadlineToneClasses[tone])}>
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
        </Card>
      )}

      {data && data.totalCount > data.items.length ? (
        <p className="text-center text-xs text-muted-foreground">
          {data.items.length} / {data.totalCount} görev gösteriliyor
        </p>
      ) : null}
    </div>
  );
}
