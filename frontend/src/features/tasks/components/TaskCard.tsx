import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
  Bug,
  CalendarClock,
  CheckSquare,
  GripVertical,
  Image as ImageIcon,
  Layers,
  Lightbulb,
  MessageSquare,
  Music,
  Paperclip,
  Search as SearchIcon,
  Sparkles,
  SquareStack,
  Swords,
  Wrench,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

import { Avatar } from '@/components/ui/avatar';
import { deadlineToneClasses, formatDueDate, getDeadlineTone } from '@/lib/dates';
import { cn } from '@/lib/utils';
import type { WorkItemSummary } from '@/types/api';
import {
  WorkItemPriority,
  WorkItemStatus,
  WorkItemType,
  workItemPriorityLabels,
  workItemTypeLabels,
} from '@/types/enums';

/** Görev türüne karşılık gelen ikon. Kart üzerinde tür rozetinin yerini tutar. */
const typeIcons: Record<WorkItemType, LucideIcon> = {
  [WorkItemType.Feature]: Sparkles,
  [WorkItemType.Bug]: Bug,
  [WorkItemType.Task]: CheckSquare,
  [WorkItemType.Improvement]: Wrench,
  [WorkItemType.Research]: SearchIcon,
  [WorkItemType.ArtAsset]: ImageIcon,
  [WorkItemType.AudioAsset]: Music,
  [WorkItemType.LevelDesign]: Layers,
  [WorkItemType.Narrative]: Lightbulb,
  [WorkItemType.Playtest]: Swords,
};

const typeIconColors: Record<WorkItemType, string> = {
  [WorkItemType.Feature]: 'text-primary',
  [WorkItemType.Bug]: 'text-danger',
  [WorkItemType.Task]: 'text-muted-foreground',
  [WorkItemType.Improvement]: 'text-info',
  [WorkItemType.Research]: 'text-info',
  [WorkItemType.ArtAsset]: 'text-warning',
  [WorkItemType.AudioAsset]: 'text-success',
  [WorkItemType.LevelDesign]: 'text-warning',
  [WorkItemType.Narrative]: 'text-primary',
  [WorkItemType.Playtest]: 'text-danger',
};

/** Öncelik şeridi: kartın sol kenarındaki renk çizgisi. */
const priorityStripe: Record<WorkItemPriority, string> = {
  [WorkItemPriority.Lowest]: 'bg-subtle-foreground',
  [WorkItemPriority.Low]: 'bg-info',
  [WorkItemPriority.Medium]: 'bg-warning',
  [WorkItemPriority.High]: 'bg-[oklch(0.7_0.19_45)]',
  [WorkItemPriority.Critical]: 'bg-danger',
};

type TaskCardProps = {
  task: WorkItemSummary;
  onOpen: (task: WorkItemSummary) => void;
  /** Sürükleme sırasında imlecin altında taşınan kopya. */
  isOverlay?: boolean;
  /** Sürükleme yetkisi yoksa tutamak gizlenir, kart yalnızca açılır. */
  canDrag?: boolean;
};

/**
 * Kanban kartı.
 *
 * Sürükleme ve tıklama ayrı hedeflere bağlanmıştır: kartın gövdesi bir düğmedir
 * ve görevi açar, sol kenardaki tutamak ise kartı taşır. Önceki tasarımda ikisi
 * aynı elemandaydı ve "sürüklemek mi istedi, tıklamak mı" ayrımı bir mesafe
 * eşiğine bırakılmıştı; kısa sürüklemeler yanlışlıkla görevi açıyordu.
 *
 * Klavye: kart gövdesinde Enter/Boşluk görevi açar. Tutamakta Boşluk kartı
 * kaldırır, oklar taşır, Boşluk bırakır, Escape iptal eder.
 */
export function TaskCard({ task, onOpen, isOverlay = false, canDrag = true }: TaskCardProps) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } =
    useSortable({
      id: task.id,
      data: { type: 'task', task },
      // Sürükleme kopyası kendi başına render edildiği için sortable devre dışı.
      disabled: isOverlay || !canDrag,
    });

  const TypeIcon = typeIcons[task.type];
  const isDone = task.status === WorkItemStatus.Done;
  const tone = getDeadlineTone(task.dueDate, isDone);

  const checklistDone =
    task.checklistTotal > 0 && task.checklistCompleted === task.checklistTotal;

  return (
    <article
      ref={setNodeRef}
      style={{ transform: CSS.Translate.toString(transform), transition }}
      aria-label={`${task.key}: ${task.title}`}
      className={cn(
        'group/card relative flex overflow-hidden rounded-xl border border-border bg-surface',
        'shadow-soft transition-[border-color,box-shadow,opacity] select-none',
        'hover:border-border-strong hover:shadow-float',
        'focus-within:border-primary/60 focus-within:ring-2 focus-within:ring-ring',
        // Sürüklenen kartın orijinali yerinde soluk bir iskelet olarak kalır;
        // böylece bırakınca nereye döneceği görünür.
        isDragging && !isOverlay && 'opacity-40 shadow-none',
        isOverlay && 'rotate-1 cursor-grabbing border-primary/50 shadow-float',
      )}
    >
      {/* Öncelik şeridi */}
      <span
        className={cn('w-1 shrink-0', priorityStripe[task.priority])}
        aria-hidden="true"
        title={workItemPriorityLabels[task.priority]}
      />

      {/* Sürükleme tutamağı: kartın tek taşıma noktası. */}
      {canDrag ? (
        <button
          ref={setActivatorNodeRef}
          type="button"
          {...attributes}
          {...listeners}
          aria-label={`${task.key} kartını taşı`}
          className={cn(
            'flex w-5 shrink-0 cursor-grab touch-none items-center justify-center',
            'text-subtle-foreground/0 transition-colors outline-none',
            'hover:bg-surface-raised hover:text-muted-foreground',
            'group-hover/card:text-subtle-foreground',
            'focus-visible:bg-surface-raised focus-visible:text-foreground',
            'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset',
            'active:cursor-grabbing',
            isOverlay && 'text-muted-foreground',
          )}
        >
          <GripVertical className="size-3.5" aria-hidden="true" />
        </button>
      ) : null}

      {/* Kart gövdesi: tıklanınca görev açılır. */}
      <button
        type="button"
        onClick={() => onOpen(task)}
        className="min-w-0 flex-1 space-y-2.5 p-3 pl-2 text-left outline-none"
      >
        <div className="flex items-start gap-2">
          <TypeIcon
            className={cn('mt-0.5 size-3.5 shrink-0', typeIconColors[task.type])}
            aria-label={workItemTypeLabels[task.type]}
          />
          <span
            className={cn(
              'line-clamp-3 flex-1 text-sm leading-snug font-medium',
              isDone && 'text-muted-foreground line-through',
            )}
          >
            {task.title}
          </span>
        </div>

        {task.labels.length > 0 ? (
          <div className="flex flex-wrap gap-1">
            {task.labels.slice(0, 3).map((label) => (
              <span
                key={label.id}
                className="rounded px-1.5 py-0.5 text-[10px] font-medium"
                style={{ backgroundColor: `${label.colorHex}22`, color: label.colorHex }}
              >
                {label.name}
              </span>
            ))}
            {task.labels.length > 3 ? (
              <span className="text-[10px] text-subtle-foreground">
                +{task.labels.length - 3}
              </span>
            ) : null}
          </div>
        ) : null}

        <div className="flex items-center gap-2.5 text-[11px] text-subtle-foreground">
          <span className="font-mono">{task.key}</span>

          {task.dueDate ? (
            <span className={cn('flex items-center gap-1 font-medium', deadlineToneClasses[tone])}>
              <CalendarClock className="size-3" aria-hidden="true" />
              {formatDueDate(task.dueDate)}
            </span>
          ) : null}

          <span className="ml-auto flex items-center gap-2">
            {task.checklistTotal > 0 ? (
              <span
                className={cn('flex items-center gap-0.5', checklistDone && 'text-success')}
                title="Kontrol listesi"
              >
                <CheckSquare className="size-3" aria-hidden="true" />
                {task.checklistCompleted}/{task.checklistTotal}
              </span>
            ) : null}

            {task.commentCount > 0 ? (
              <span className="flex items-center gap-0.5" title="Yorum">
                <MessageSquare className="size-3" aria-hidden="true" />
                {task.commentCount}
              </span>
            ) : null}

            {task.attachmentCount > 0 ? (
              <span className="flex items-center gap-0.5" title="Dosya">
                <Paperclip className="size-3" aria-hidden="true" />
                {task.attachmentCount}
              </span>
            ) : null}

            {task.subItemCount > 0 ? (
              <span className="flex items-center gap-0.5" title="Alt görev">
                <SquareStack className="size-3" aria-hidden="true" />
                {task.subItemCount}
              </span>
            ) : null}
          </span>
        </div>

        <div className="flex items-center justify-between gap-2">
          <span className="text-[11px] text-subtle-foreground">
            {workItemPriorityLabels[task.priority]}
          </span>

          {task.assignee ? (
            <Avatar
              fullName={task.assignee.fullName}
              avatarUrl={task.assignee.avatarUrl}
              size="xs"
            />
          ) : (
            <span className="text-[11px] text-subtle-foreground">Atanmadı</span>
          )}
        </div>
      </button>
    </article>
  );
}
