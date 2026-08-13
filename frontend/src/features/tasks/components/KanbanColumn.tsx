import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { ChevronDown, Inbox, Plus } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import type { WorkItemSummary } from '@/types/api';
import { WorkItemStatus } from '@/types/enums';

import { TaskCard } from './TaskCard';

/** Kolon rengi; raporlardaki durum paletiyle aynı. */
export const kanbanStatusColors: Record<WorkItemStatus, string> = {
  [WorkItemStatus.Pending]: '#64748B',
  [WorkItemStatus.Todo]: '#3B82F6',
  [WorkItemStatus.InProgress]: '#8B5CF6',
  [WorkItemStatus.CodeReview]: '#F59E0B',
  [WorkItemStatus.Testing]: '#06B6D4',
  [WorkItemStatus.Done]: '#22C55E',
  [WorkItemStatus.Cancelled]: '#EF4444',
};

type KanbanColumnProps = {
  status: WorkItemStatus;
  title: string;
  tasks: WorkItemSummary[];
  /** Filtre uygulanmadan önceki kart sayısı; başlıkta "3 / 12" gösterimi için. */
  unfilteredCount: number;
  onOpenTask: (task: WorkItemSummary) => void;
  onAddTask: (status: WorkItemStatus) => void;
  canCreate: boolean;
  canDrag: boolean;
  isCollapsed: boolean;
  onToggleCollapse: (status: WorkItemStatus) => void;
  /** Sürükleme sürüyor mu; hedef olmayan kolonlar da bırakma alanını belli eder. */
  isDragActive: boolean;
};

export function KanbanColumn({
  status,
  title,
  tasks,
  unfilteredCount,
  onOpenTask,
  onAddTask,
  canCreate,
  canDrag,
  isCollapsed,
  onToggleCollapse,
  isDragActive,
}: KanbanColumnProps) {
  // Kolonun gövdesi de bırakma hedefi: boş kolona ve kartların altındaki
  // boşluğa da kart bırakılabilmeli.
  const { setNodeRef, isOver } = useDroppable({
    id: `column-${status}`,
    data: { type: 'column', status },
  });

  const color = kanbanStatusColors[status];
  const isFiltered = unfilteredCount !== tasks.length;

  if (isCollapsed) {
    return (
      <section
        aria-label={`${title} (daraltılmış)`}
        className="flex w-11 shrink-0 flex-col items-center gap-3 rounded-card border border-border bg-surface/40 py-3"
      >
        <button
          type="button"
          onClick={() => onToggleCollapse(status)}
          aria-label={`${title} kolonunu genişlet`}
          aria-expanded={false}
          className={cn(
            'flex flex-1 flex-col items-center gap-3 rounded-lg px-1 outline-none',
            'text-muted-foreground transition-colors hover:text-foreground',
            'focus-visible:ring-2 focus-visible:ring-ring',
          )}
        >
          <span
            className="size-2 shrink-0 rounded-full"
            style={{ backgroundColor: color }}
            aria-hidden="true"
          />
          <span className="rounded bg-surface-raised px-1 py-0.5 text-[11px] tabular-nums">
            {tasks.length}
          </span>
          {/* Dikey başlık: daraltılmış kolonda hangi durum olduğu okunabilir kalır. */}
          <span className="text-xs font-medium whitespace-nowrap [writing-mode:vertical-rl]">
            {title}
          </span>
        </button>
      </section>
    );
  }

  return (
    <section
      aria-label={title}
      className={cn(
        'flex w-[19rem] shrink-0 flex-col overflow-hidden rounded-card border bg-surface/40',
        'transition-colors',
        isOver ? 'border-primary/70 bg-primary/5' : 'border-border',
      )}
    >
      {/* Üst çizgi kolonun rengini taşır; başlıkta ayrıca nokta gerekmez. */}
      <span className="h-1 w-full shrink-0" style={{ backgroundColor: color }} aria-hidden="true" />

      <header className="flex items-center gap-2 border-b border-border/70 px-3 py-2.5">
        <h3 className="truncate text-sm font-semibold">{title}</h3>

        <span
          className="shrink-0 rounded bg-surface-raised px-1.5 py-0.5 text-[11px] tabular-nums text-muted-foreground"
          title={isFiltered ? `Filtreyle eşleşen ${tasks.length} / toplam ${unfilteredCount}` : undefined}
        >
          {isFiltered ? `${tasks.length}/${unfilteredCount}` : tasks.length}
        </span>

        <div className="ml-auto flex shrink-0 items-center">
          {canCreate ? (
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={() => onAddTask(status)}
              aria-label={`${title} kolonuna görev ekle`}
            >
              <Plus aria-hidden="true" />
            </Button>
          ) : null}

          <Button
            variant="ghost"
            size="icon-sm"
            onClick={() => onToggleCollapse(status)}
            aria-label={`${title} kolonunu daralt`}
            aria-expanded
          >
            <ChevronDown aria-hidden="true" />
          </Button>
        </div>
      </header>

      <div
        ref={setNodeRef}
        className={cn(
          // Sabit yükseklik + kendi kaydırması: uzun kolon sayfayı uzatmaz,
          // kolon başlıkları hep aynı hizada kalır.
          'flex max-h-[calc(100vh-22rem)] min-h-40 flex-1 flex-col gap-2 overflow-y-auto p-2',
          'scroll-py-2',
        )}
      >
        <SortableContext items={tasks.map((task) => task.id)} strategy={verticalListSortingStrategy}>
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} onOpen={onOpenTask} canDrag={canDrag} />
          ))}
        </SortableContext>

        {tasks.length === 0 ? (
          <div
            className={cn(
              'flex flex-1 flex-col items-center justify-center gap-1.5 rounded-lg border border-dashed',
              'px-3 py-6 text-center transition-colors',
              isOver
                ? 'border-primary/70 bg-primary/8 text-primary'
                : isDragActive
                  ? 'border-border-strong text-muted-foreground'
                  : 'border-border/70 text-subtle-foreground',
            )}
          >
            <Inbox className="size-4" aria-hidden="true" />
            <p className="text-xs">
              {isDragActive ? 'Buraya bırak' : isFiltered ? 'Filtreyle eşleşen kart yok' : 'Kart yok'}
            </p>
          </div>
        ) : null}

        {/* Kartların altındaki serbest alan da bırakma hedefidir: listenin
            sonuna taşımak için son kartın üstüne nişan almak gerekmez. */}
        {tasks.length > 0 && isDragActive ? (
          <div
            className={cn(
              'shrink-0 rounded-lg border border-dashed transition-colors',
              isOver ? 'h-10 border-primary/60 bg-primary/5' : 'h-6 border-transparent',
            )}
            aria-hidden="true"
          />
        ) : null}
      </div>
    </section>
  );
}
