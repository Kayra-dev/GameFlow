import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  MeasuringStrategy,
  PointerSensor,
  TouchSensor,
  closestCorners,
  pointerWithin,
  useSensor,
  useSensors,
  type Announcements,
  type CollisionDetection,
  type DragEndEvent,
  type DragOverEvent,
  type DragStartEvent,
  type ScreenReaderInstructions,
} from '@dnd-kit/core';
import { restrictToWindowEdges } from '@dnd-kit/modifiers';
import { sortableKeyboardCoordinates } from '@dnd-kit/sortable';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { RotateCcw, Search, SquareKanban, X } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
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
import { useDebouncedValue } from '@/hooks/use-debounced-value';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import type { KanbanBoardDto, UserSummary, WorkItemSummary } from '@/types/api';
import {
  WorkItemPriority,
  WorkItemStatus,
  WorkItemType,
  kanbanColumnOrder,
  workItemPriorityLabels,
  workItemStatusLabels,
  workItemTypeLabels,
} from '@/types/enums';

import { workItemsApi } from '../api/work-items-api';
import { KanbanColumn } from './KanbanColumn';
import { TaskCard } from './TaskCard';

type ColumnMap = Map<WorkItemStatus, WorkItemSummary[]>;

type KanbanBoardProps = {
  projectId: string;
  teamId?: string;
  sprintId?: string;
  onOpenTask: (task: WorkItemSummary) => void;
  onAddTask: (status: WorkItemStatus) => void;
  canCreate: boolean;
};

/** Sürükleme tutamaktan başlatıldığı için eşik küçük tutulabilir. */
const DRAG_ACTIVATION_DISTANCE = 4;

/** Dokunmatikte kaydırmayla karışmaması için basılı tutma süresi. */
const TOUCH_ACTIVATION_DELAY_MS = 180;

const screenReaderInstructions: ScreenReaderInstructions = {
  draggable:
    'Kartı klavyeyle taşımak için Boşluk tuşuna basın. ' +
    'Ok tuşlarıyla kolonlar ve kartlar arasında gezinin, ' +
    'bırakmak için tekrar Boşluk, vazgeçmek için Escape tuşuna basın.',
};

export function KanbanBoard({
  projectId,
  teamId,
  sprintId,
  onOpenTask,
  onAddTask,
  canCreate,
}: KanbanBoardProps) {
  const queryClient = useQueryClient();
  const currentUserId = useAuthStore((state) => state.user?.id);

  const [activeTask, setActiveTask] = useState<WorkItemSummary | null>(null);
  /** Sürükleme sürerken kartların canlı konumu; bırakılınca temizlenir. */
  const [dragColumns, setDragColumns] = useState<ColumnMap | null>(null);
  const [collapsed, setCollapsed] = useState<Set<WorkItemStatus>>(() => new Set());

  const [search, setSearch] = useState('');
  const [assigneeFilter, setAssigneeFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');
  const [priorityFilter, setPriorityFilter] = useState('all');

  const debouncedSearch = useDebouncedValue(search, 200);

  const boardQueryKey = queryKeys.workItems.board({ projectId, teamId, sprintId });

  const { data: board, isLoading, isError } = useQuery({
    queryKey: boardQueryKey,
    queryFn: () => workItemsApi.board(projectId, teamId, sprintId),
  });

  const sensors = useSensors(
    // Tutamağa yapılan tıklamanın sürükleme sayılmaması için küçük bir eşik.
    useSensor(PointerSensor, {
      activationConstraint: { distance: DRAG_ACTIVATION_DISTANCE },
    }),
    // Dokunmatikte parmakla kaydırma ile taşımayı ayırmak için gecikme kullanılır;
    // mesafe eşiği burada sayfayı kaydırmayı imkânsız hâle getirirdi.
    useSensor(TouchSensor, {
      activationConstraint: { delay: TOUCH_ACTIVATION_DELAY_MS, tolerance: 6 },
    }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  /** Sunucudan gelen pano; durum → kart listesi. */
  const serverColumns = useMemo<ColumnMap>(() => {
    const map: ColumnMap = new Map();

    for (const status of kanbanColumnOrder) {
      map.set(status, []);
    }

    for (const column of board?.columns ?? []) {
      map.set(column.status, column.items);
    }

    return map;
  }, [board]);

  // Sürükleme sırasında kartlar dragColumns üzerinden yerleşir; böylece kart
  // parmağı takip ederken hedef kolonda gerçekten görünür, yalnızca kolon
  // vurgulanmakla kalmaz.
  const workingColumns = dragColumns ?? serverColumns;

  /** Atanan kişi filtresi için panodaki benzersiz kullanıcılar. */
  const assignees = useMemo(() => {
    const map = new Map<string, UserSummary>();

    for (const items of serverColumns.values()) {
      for (const item of items) {
        if (item.assignee) map.set(item.assignee.id, item.assignee);
      }
    }

    return [...map.values()].sort((a, b) => a.fullName.localeCompare(b.fullName, 'tr'));
  }, [serverColumns]);

  const hasFilters =
    debouncedSearch.trim() !== '' ||
    assigneeFilter !== 'all' ||
    typeFilter !== 'all' ||
    priorityFilter !== 'all';

  const matchesFilters = useCallback(
    (task: WorkItemSummary) => {
      const query = debouncedSearch.trim().toLocaleLowerCase('tr');

      if (
        query &&
        !task.title.toLocaleLowerCase('tr').includes(query) &&
        !task.key.toLocaleLowerCase('tr').includes(query)
      ) {
        return false;
      }

      if (assigneeFilter === 'unassigned' && task.assignee) return false;
      if (assigneeFilter === 'mine' && task.assignee?.id !== currentUserId) return false;

      if (
        assigneeFilter !== 'all' &&
        assigneeFilter !== 'unassigned' &&
        assigneeFilter !== 'mine' &&
        task.assignee?.id !== assigneeFilter
      ) {
        return false;
      }

      if (typeFilter !== 'all' && task.type !== Number(typeFilter)) return false;
      if (priorityFilter !== 'all' && task.priority !== Number(priorityFilter)) return false;

      return true;
    },
    [debouncedSearch, assigneeFilter, typeFilter, priorityFilter, currentUserId],
  );

  /** Ekranda görünen kartlar. Sürüklenen kart filtreye takılsa bile kalır. */
  const displayColumns = useMemo<ColumnMap>(() => {
    if (!hasFilters) return workingColumns;

    const map: ColumnMap = new Map();

    for (const [status, items] of workingColumns) {
      map.set(
        status,
        items.filter((item) => item.id === activeTask?.id || matchesFilters(item)),
      );
    }

    return map;
  }, [workingColumns, hasFilters, matchesFilters, activeTask]);

  const visibleCount = useMemo(
    () => [...displayColumns.values()].reduce((total, items) => total + items.length, 0),
    [displayColumns],
  );

  const totalCount = useMemo(
    () => [...serverColumns.values()].reduce((total, items) => total + items.length, 0),
    [serverColumns],
  );

  const move = useMutation({
    mutationFn: ({
      taskId,
      targetStatus,
      precedingItemId,
      followingItemId,
    }: {
      taskId: string;
      targetStatus: WorkItemStatus;
      precedingItemId: string | null;
      followingItemId: string | null;
      /** Bırakma anında alınan pano; hata durumunda buraya dönülür. */
      rollbackTo: KanbanBoardDto | undefined;
    }) => workItemsApi.move(taskId, { targetStatus, precedingItemId, followingItemId }),

    // İyimser yerleşim bırakma anında handleDragEnd içinde yazılır; burada
    // yalnızca eş zamanlı bir yeniden çekimin onu ezmesi engellenir.
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: boardQueryKey });
    },

    onError: (error, variables) => {
      if (variables.rollbackTo) {
        queryClient.setQueryData(boardQueryKey, variables.rollbackTo);
      }

      toast.error(getErrorMessage(error));
    },

    onSuccess: (_data, variables) => {
      toast.success(`Görev “${workItemStatusLabels[variables.targetStatus]}” kolonuna taşındı.`);
    },

    onSettled: () => {
      // Sunucudaki gerçek sıra değeriyle eşitlenir.
      void queryClient.invalidateQueries({ queryKey: boardQueryKey });
      // Dashboard sayaçları ve görev listeleri de etkilenir.
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });

  const findColumnOf = (columns: ColumnMap, taskId: string): WorkItemStatus | null => {
    for (const [status, items] of columns) {
      if (items.some((item) => item.id === taskId)) return status;
    }

    return null;
  };

  /**
   * Kolonlar hem kart hem kolon olarak bırakma hedefi sunar. İşaretçi bir
   * kartın üzerindeyse o kart, kolonun boşluğundaysa kolon seçilir; ikisi de
   * altında değilse (kolonlar arası boşluk) en yakın kolona düşülür.
   */
  const collisionDetection: CollisionDetection = useCallback((args) => {
    const pointerCollisions = pointerWithin(args);

    return pointerCollisions.length > 0 ? pointerCollisions : closestCorners(args);
  }, []);

  const handleDragStart = (event: DragStartEvent) => {
    const task = event.active.data.current?.task as WorkItemSummary | undefined;

    setActiveTask(task ?? null);
    setDragColumns(new Map(serverColumns));
  };

  /**
   * Sürükleme sürerken kartın yeni yerini hesaplar. Konum, filtrelenmemiş tam
   * liste üzerinde tutulur; bırakıldığında komşular buradan okunur ve sunucuya
   * "şu ikisinin arasına" denir.
   */
  const handleDragOver = (event: DragOverEvent) => {
    const { active, over } = event;
    if (!over) return;

    setDragColumns((current) => {
      const columns = current ?? serverColumns;

      const activeId = String(active.id);
      const sourceStatus = findColumnOf(columns, activeId);
      if (sourceStatus === null) return current;

      const overData = over.data.current;

      const targetStatus =
        overData?.type === 'column'
          ? (overData.status as WorkItemStatus)
          : findColumnOf(columns, String(over.id));

      if (targetStatus === null) return current;

      const sourceItems = columns.get(sourceStatus) ?? [];
      const sourceIndex = sourceItems.findIndex((item) => item.id === activeId);
      const task = sourceItems[sourceIndex];
      if (!task) return current;

      // Hedef listedeki ekleme noktası.
      const targetItems =
        sourceStatus === targetStatus
          ? sourceItems.filter((item) => item.id !== activeId)
          : [...(columns.get(targetStatus) ?? [])];

      let insertIndex = targetItems.length;

      if (overData?.type === 'task') {
        const overIndex = targetItems.findIndex((item) => item.id === String(over.id));

        if (overIndex >= 0) {
          // İşaretçi hedef kartın alt yarısındaysa altına, üst yarısındaysa
          // üstüne yerleşir. Rect karşılaştırması kolonlar arasında da doğrudur.
          const activeRect = active.rect.current.translated;
          const isBelow =
            activeRect !== null && activeRect.top > over.rect.top + over.rect.height / 2;

          insertIndex = isBelow ? overIndex + 1 : overIndex;
        }
      }

      const moved: WorkItemSummary =
        task.status === targetStatus ? task : { ...task, status: targetStatus };

      targetItems.splice(insertIndex, 0, moved);

      // Değişiklik yoksa yeni Map üretilmez; gereksiz render engellenir.
      const previousTarget = columns.get(targetStatus) ?? [];
      const unchanged =
        sourceStatus === targetStatus &&
        previousTarget.length === targetItems.length &&
        previousTarget.every((item, index) => item.id === targetItems[index]?.id);

      if (unchanged) return current;

      const next: ColumnMap = new Map(columns);
      next.set(targetStatus, targetItems);

      if (sourceStatus !== targetStatus) {
        next.set(
          sourceStatus,
          sourceItems.filter((item) => item.id !== activeId),
        );
      }

      return next;
    });
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const task = activeTask;
    const finalColumns = dragColumns;

    setActiveTask(null);

    if (!task || !finalColumns || !event.over) {
      setDragColumns(null);
      return;
    }

    const targetStatus = findColumnOf(finalColumns, task.id);

    if (targetStatus === null) {
      setDragColumns(null);
      return;
    }

    const targetItems = finalColumns.get(targetStatus) ?? [];
    const index = targetItems.findIndex((item) => item.id === task.id);

    const sourceItems = serverColumns.get(task.status) ?? [];
    const originalIndex = sourceItems.findIndex((item) => item.id === task.id);

    // Kart yerinden kımıldamadıysa sunucuya gitmeye gerek yok.
    if (task.status === targetStatus && originalIndex === index) {
      setDragColumns(null);
      return;
    }

    const precedingItemId = index > 0 ? (targetItems[index - 1]?.id ?? null) : null;
    const followingItemId = targetItems[index + 1]?.id ?? null;

    // Bırakma anındaki yerleşim önbelleğe yazılır: kart parmağın bıraktığı
    // yerde kalır, sunucu yanıtı beklenmez. Hata gelirse rollbackTo geri yükler.
    const rollbackTo = queryClient.getQueryData<KanbanBoardDto>(boardQueryKey);

    queryClient.setQueryData<KanbanBoardDto>(boardQueryKey, (current) =>
      current ? toBoardDto(current, finalColumns) : current,
    );

    setDragColumns(null);

    move.mutate({ taskId: task.id, targetStatus, precedingItemId, followingItemId, rollbackTo });
  };

  const handleDragCancel = () => {
    setActiveTask(null);
    setDragColumns(null);
  };

  const toggleCollapse = (status: WorkItemStatus) => {
    setCollapsed((current) => {
      const next = new Set(current);

      if (next.has(status)) {
        next.delete(status);
      } else {
        next.add(status);
      }

      return next;
    });
  };

  const clearFilters = () => {
    setSearch('');
    setAssigneeFilter('all');
    setTypeFilter('all');
    setPriorityFilter('all');
  };

  /** Ekran okuyucu duyuruları; sürükleme klavyeyle de yapılabildiği için gerekli. */
  const announcements: Announcements = useMemo(
    () => ({
      onDragStart: ({ active }) => {
        const task = active.data.current?.task as WorkItemSummary | undefined;
        return `${task?.key ?? 'Kart'} alındı.`;
      },
      onDragOver: ({ active, over }) => {
        if (!over) return undefined;

        const task = active.data.current?.task as WorkItemSummary | undefined;
        const overData = over.data.current;

        const status =
          overData?.type === 'column'
            ? (overData.status as WorkItemStatus)
            : (overData?.task as WorkItemSummary | undefined)?.status;

        if (status === undefined) return undefined;

        return `${task?.key ?? 'Kart'}, ${workItemStatusLabels[status]} kolonunun üzerinde.`;
      },
      onDragEnd: ({ active, over }) => {
        const task = active.data.current?.task as WorkItemSummary | undefined;

        if (!over) return `${task?.key ?? 'Kart'} taşınmadı.`;

        const overData = over.data.current;

        const status =
          overData?.type === 'column'
            ? (overData.status as WorkItemStatus)
            : (overData?.task as WorkItemSummary | undefined)?.status;

        return status === undefined
          ? `${task?.key ?? 'Kart'} bırakıldı.`
          : `${task?.key ?? 'Kart'}, ${workItemStatusLabels[status]} kolonuna bırakıldı.`;
      },
      onDragCancel: ({ active }) => {
        const task = active.data.current?.task as WorkItemSummary | undefined;
        return `${task?.key ?? 'Kart'} taşıması iptal edildi, kart eski yerine döndü.`;
      },
    }),
    [],
  );

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-10 rounded-lg" />
        <div className="flex gap-3 overflow-hidden pb-2">
          {kanbanColumnOrder.map((status) => (
            <Skeleton key={status} className="h-96 w-[19rem] shrink-0 rounded-card" />
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <EmptyState
        icon={SquareKanban}
        title="Pano yüklenemedi"
        description="Sunucuya ulaşılamıyor. Sayfayı yenileyip tekrar deneyin."
      />
    );
  }

  return (
    <div className="space-y-3">
      {/* Pano araç çubuğu: filtreler kartları yalnızca ekranda süzer,
          sunucuya yeni istek gitmez. */}
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative w-full sm:w-56">
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-subtle-foreground"
            aria-hidden="true"
          />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Başlık veya anahtar ara…"
            aria-label="Panoda ara"
            className="h-9 pl-9"
          />
        </div>

        <Select value={assigneeFilter} onValueChange={setAssigneeFilter}>
          <SelectTrigger className="h-9 w-full sm:w-44" aria-label="Atanan kişiye göre süz">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Herkes</SelectItem>
            <SelectItem value="mine">Bana atananlar</SelectItem>
            <SelectItem value="unassigned">Atanmamış</SelectItem>
            {assignees.map((assignee) => (
              <SelectItem key={assignee.id} value={assignee.id}>
                {assignee.fullName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={typeFilter} onValueChange={setTypeFilter}>
          <SelectTrigger className="h-9 w-full sm:w-40" aria-label="Türe göre süz">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm türler</SelectItem>
            {Object.values(WorkItemType).map((type) => (
              <SelectItem key={type} value={String(type)}>
                {workItemTypeLabels[type]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={priorityFilter} onValueChange={setPriorityFilter}>
          <SelectTrigger className="h-9 w-full sm:w-40" aria-label="Önceliğe göre süz">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm öncelikler</SelectItem>
            {Object.values(WorkItemPriority).map((priority) => (
              <SelectItem key={priority} value={String(priority)}>
                {workItemPriorityLabels[priority]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {hasFilters ? (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            <X aria-hidden="true" />
            Filtreleri temizle
          </Button>
        ) : null}

        <div className="ml-auto flex items-center gap-2">
          <span className="text-xs tabular-nums text-muted-foreground">
            {hasFilters ? `${visibleCount} / ${totalCount} kart` : `${totalCount} kart`}
          </span>

          {collapsed.size > 0 ? (
            <Button variant="ghost" size="sm" onClick={() => setCollapsed(new Set())}>
              <RotateCcw aria-hidden="true" />
              Kolonları aç
            </Button>
          ) : null}
        </div>
      </div>

      <DndContext
        sensors={sensors}
        collisionDetection={collisionDetection}
        modifiers={[restrictToWindowEdges]}
        // Kolonlar sürükleme sırasında büyüyüp küçüldüğü için sürekli ölçülür;
        // aksi halde kart, taşındıktan sonra eski ölçülere göre hedef arar.
        measuring={{ droppable: { strategy: MeasuringStrategy.Always } }}
        // Kenara yaklaşınca pano yatayda kendiliğinden kayar.
        autoScroll={{ threshold: { x: 0.2, y: 0.1 } }}
        accessibility={{ announcements, screenReaderInstructions }}
        onDragStart={handleDragStart}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
        onDragCancel={handleDragCancel}
      >
        <div
          className={cn(
            'flex gap-3 overflow-x-auto pb-3',
            // Sürükleme sırasında metin seçimi kapatılır.
            activeTask && 'select-none',
          )}
        >
          {kanbanColumnOrder.map((status) => (
            <KanbanColumn
              key={status}
              status={status}
              title={workItemStatusLabels[status]}
              tasks={displayColumns.get(status) ?? []}
              unfilteredCount={(workingColumns.get(status) ?? []).length}
              onOpenTask={onOpenTask}
              onAddTask={onAddTask}
              canCreate={canCreate}
              // Taşıma yetkisi görev bazlıdır (üye kendi görevini taşıyabilir),
              // bu yüzden sunucuda denetlenir; reddedilirse kart geri döner.
              canDrag
              isCollapsed={collapsed.has(status)}
              onToggleCollapse={toggleCollapse}
              isDragActive={activeTask !== null}
            />
          ))}
        </div>

        {/* Sürüklenen kartın imleci takip eden kopyası */}
        <DragOverlay dropAnimation={{ duration: 200, easing: 'cubic-bezier(0.22, 1, 0.36, 1)' }}>
          {activeTask ? <TaskCard task={activeTask} onOpen={() => undefined} isOverlay /> : null}
        </DragOverlay>
      </DndContext>
    </div>
  );
}

/** Kolon haritasını sunucudan gelen pano biçimine geri çevirir. */
function toBoardDto(board: KanbanBoardDto, columns: ColumnMap): KanbanBoardDto {
  return {
    ...board,
    columns: board.columns.map((column) => {
      const items = columns.get(column.status);

      return items ? { ...column, items, totalCount: items.length } : column;
    }),
  };
}
