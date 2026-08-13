import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  addDays,
  addMonths,
  addWeeks,
  endOfDay,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  isToday,
  startOfDay,
  startOfMonth,
  startOfWeek,
} from 'date-fns';
import { tr } from 'date-fns/locale';
import { CalendarDays, CalendarPlus, ChevronLeft, ChevronRight, Trash2, Users } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { MeetingFormDialog } from '@/features/meetings/components/MeetingFormDialog';
import { getErrorMessage } from '@/lib/api-client';
import { formatMonthYear, formatTime } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import { isLeader, useAuthStore } from '@/stores/auth-store';
import type { CalendarItemDto } from '@/types/api';
import { calendarEventTypeLabels } from '@/types/enums';

import { calendarApi } from './api/calendar-api';
import { EventFormDialog } from './components/EventFormDialog';

type ViewMode = 'month' | 'week' | 'day';

/** Takvim: ay, hafta ve gün görünümleri aynı uç noktayı farklı aralıklarla çağırır. */
export function CalendarPage() {
  const [view, setView] = useState<ViewMode>('month');
  const [anchor, setAnchor] = useState(new Date());

  const user = useAuthStore((state) => state.user);

  const [eventFormOpen, setEventFormOpen] = useState(false);
  const [meetingFormOpen, setMeetingFormOpen] = useState(false);
  /** Boş bir güne tıklanınca form o günle açılır. */
  const [formDate, setFormDate] = useState<Date | undefined>(undefined);

  // Ay görünümünde ızgara tam haftalarla dolar; bu yüzden aralık aydan geniştir.
  const { from, to } = useMemo(() => {
    if (view === 'month') {
      return {
        from: startOfWeek(startOfMonth(anchor), { weekStartsOn: 1 }),
        to: endOfWeek(endOfMonth(anchor), { weekStartsOn: 1 }),
      };
    }

    if (view === 'week') {
      return {
        from: startOfWeek(anchor, { weekStartsOn: 1 }),
        to: endOfWeek(anchor, { weekStartsOn: 1 }),
      };
    }

    return { from: startOfDay(anchor), to: endOfDay(anchor) };
  }, [view, anchor]);

  const params = { from: from.toISOString(), to: to.toISOString() };

  const { data: items, isLoading } = useQuery({
    queryKey: queryKeys.calendar(params),
    queryFn: () => calendarApi.items(params),
  });

  /** Günlere göre gruplanmış öğeler; ızgara ve liste aynı kaynaktan beslenir. */
  const itemsByDay = useMemo(() => {
    const map = new Map<string, CalendarItemDto[]>();

    for (const item of items ?? []) {
      const dayKey = format(new Date(item.startsAt), 'yyyy-MM-dd');
      const existing = map.get(dayKey);

      if (existing) {
        existing.push(item);
      } else {
        map.set(dayKey, [item]);
      }
    }

    return map;
  }, [items]);

  const step = (direction: 1 | -1) => {
    setAnchor((current) => {
      if (view === 'month') return addMonths(current, direction);
      if (view === 'week') return addWeeks(current, direction);
      return addDays(current, direction);
    });
  };

  const title =
    view === 'month'
      ? formatMonthYear(anchor)
      : view === 'week'
        ? `${format(from, 'd MMM', { locale: tr })} – ${format(to, 'd MMM yyyy', { locale: tr })}`
        : format(anchor, 'd MMMM yyyy, EEEE', { locale: tr });

  const openEventForm = (date?: Date) => {
    setFormDate(date);
    setEventFormOpen(true);
  };

  return (
    <div className="mx-auto w-full max-w-7xl space-y-5">
      <header className="flex flex-wrap items-start gap-4">
        <div className="min-w-0 flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">Takvim</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Görev son tarihleri, sprint tarihleri, toplantılar ve etkinlikler bir arada.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Toplantı oluşturmak lider/yönetici yetkisi ister; etkinlik herkese açıktır. */}
          {isLeader(user) ? (
            <Button
              variant="secondary"
              onClick={() => {
                setFormDate(view === 'month' ? undefined : anchor);
                setMeetingFormOpen(true);
              }}
            >
              <Users aria-hidden="true" />
              Yeni toplantı
            </Button>
          ) : null}

          <Button onClick={() => openEventForm(view === 'month' ? undefined : anchor)}>
            <CalendarPlus aria-hidden="true" />
            Yeni etkinlik
          </Button>
        </div>
      </header>

      <div className="flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-1">
          <Button variant="secondary" size="icon" onClick={() => step(-1)} aria-label="Önceki">
            <ChevronLeft aria-hidden="true" />
          </Button>
          <Button variant="secondary" size="icon" onClick={() => step(1)} aria-label="Sonraki">
            <ChevronRight aria-hidden="true" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => setAnchor(new Date())}>
            Bugün
          </Button>
        </div>

        <h2 className="text-base font-medium first-letter:uppercase">{title}</h2>

        <Tabs
          value={view}
          onValueChange={(value) => setView(value as ViewMode)}
          className="ml-auto"
        >
          <TabsList>
            <TabsTrigger value="month">Ay</TabsTrigger>
            <TabsTrigger value="week">Hafta</TabsTrigger>
            <TabsTrigger value="day">Gün</TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      {isLoading ? (
        <Skeleton className="h-[32rem] rounded-card" />
      ) : view === 'month' ? (
        <MonthGrid
          anchor={anchor}
          from={from}
          to={to}
          itemsByDay={itemsByDay}
          onSelectDay={openEventForm}
        />
      ) : (
        <DayList from={from} to={to} itemsByDay={itemsByDay} showDayHeaders={view === 'week'} />
      )}

      <EventFormDialog
        open={eventFormOpen}
        onOpenChange={setEventFormOpen}
        defaultDate={formDate}
      />

      <MeetingFormDialog
        open={meetingFormOpen}
        onOpenChange={setMeetingFormOpen}
        defaultDate={formDate}
      />
    </div>
  );
}

function MonthGrid({
  anchor,
  from,
  to,
  itemsByDay,
  onSelectDay,
}: {
  anchor: Date;
  from: Date;
  to: Date;
  itemsByDay: Map<string, CalendarItemDto[]>;
  onSelectDay: (day: Date) => void;
}) {
  const days: Date[] = [];

  for (let day = from; day <= to; day = addDays(day, 1)) {
    days.push(day);
  }

  const weekDayNames = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

  return (
    <Card className="overflow-hidden">
      <div className="grid grid-cols-7 border-b border-border">
        {weekDayNames.map((name) => (
          <div
            key={name}
            className="px-2 py-2 text-center text-[11px] font-medium tracking-wider text-subtle-foreground uppercase"
          >
            {name}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7">
        {days.map((day) => {
          const dayItems = itemsByDay.get(format(day, 'yyyy-MM-dd')) ?? [];
          const isCurrentMonth = isSameMonth(day, anchor);

          return (
            <div
              key={day.toISOString()}
              className={cn(
                'group/day relative min-h-24 border-r border-b border-border p-1.5 last:border-r-0',
                !isCurrentMonth && 'bg-background/40',
              )}
            >
              <div className="flex items-center justify-between">
                <span
                  className={cn(
                    'inline-grid size-6 place-items-center rounded-full text-xs tabular-nums',
                    isToday(day) && 'bg-primary font-semibold text-primary-foreground',
                    !isCurrentMonth && 'text-subtle-foreground',
                  )}
                >
                  {format(day, 'd')}
                </span>

                {/* Güne etkinlik ekleme; yalnızca imleç hücrenin üzerindeyken
                    ya da düğme odaktayken görünür. */}
                <button
                  type="button"
                  onClick={() => onSelectDay(day)}
                  aria-label={`${format(day, 'd MMMM yyyy', { locale: tr })} gününe etkinlik ekle`}
                  className={cn(
                    'grid size-5 place-items-center rounded text-subtle-foreground opacity-0',
                    'transition-opacity outline-none hover:bg-surface-raised hover:text-foreground',
                    'group-hover/day:opacity-100 focus-visible:opacity-100',
                    'focus-visible:ring-2 focus-visible:ring-ring',
                  )}
                >
                  <CalendarPlus className="size-3.5" aria-hidden="true" />
                </button>
              </div>

              <div className="mt-1 space-y-1">
                {dayItems.slice(0, 3).map((item) => (
                  <CalendarChip key={`${item.id}-${item.type}`} item={item} />
                ))}
                {dayItems.length > 3 ? (
                  <p className="px-1 text-[10px] text-subtle-foreground">
                    +{dayItems.length - 3} daha
                  </p>
                ) : null}
              </div>
            </div>
          );
        })}
      </div>
    </Card>
  );
}

function DayList({
  from,
  to,
  itemsByDay,
  showDayHeaders,
}: {
  from: Date;
  to: Date;
  itemsByDay: Map<string, CalendarItemDto[]>;
  showDayHeaders: boolean;
}) {
  const days: Date[] = [];

  for (let day = from; day <= to; day = addDays(day, 1)) {
    days.push(day);
  }

  const hasAny = days.some(
    (day) => (itemsByDay.get(format(day, 'yyyy-MM-dd')) ?? []).length > 0,
  );

  if (!hasAny) {
    return (
      <Card>
        <EmptyState
          icon={CalendarDays}
          title="Bu aralıkta kayıt yok"
          description="Görev son tarihleri, sprint tarihleri ve toplantılar burada listelenir."
        />
      </Card>
    );
  }

  return (
    <div className="space-y-3">
      {days.map((day) => {
        const dayItems = itemsByDay.get(format(day, 'yyyy-MM-dd')) ?? [];

        if (dayItems.length === 0 && showDayHeaders) return null;
        if (dayItems.length === 0) return null;

        return (
          <Card key={day.toISOString()}>
            <CardContent className="pt-5">
              <h3
                className={cn(
                  'mb-3 text-sm font-semibold first-letter:uppercase',
                  isSameDay(day, new Date()) && 'text-primary',
                )}
              >
                {format(day, 'd MMMM EEEE', { locale: tr })}
              </h3>

              <ul className="space-y-2">
                {dayItems.map((item) => (
                  <li key={`${item.id}-${item.type}`}>
                    <CalendarRow item={item} />
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}

/** Ay ızgarasındaki küçük renkli etiket. */
function CalendarChip({ item }: { item: CalendarItemDto }) {
  const content = (
    <span
      className="block truncate rounded px-1.5 py-0.5 text-[10px] font-medium"
      style={{ backgroundColor: `${item.colorHex}22`, color: item.colorHex }}
      title={item.title}
    >
      {item.isAllDay ? '' : `${formatTime(item.startsAt)} `}
      {item.title}
    </span>
  );

  return item.link ? (
    <Link to={item.link} className="block outline-none focus-visible:ring-2 focus-visible:ring-ring">
      {content}
    </Link>
  ) : (
    content
  );
}

/**
 * Hafta/gün görünümündeki liste satırı.
 *
 * Bağlantısı olan öğeler (görev son tarihi, sprint, toplantı) kaynak kayda
 * gider. Bağlantısı olmayanlar elle eklenmiş takvim etkinlikleridir; yalnızca
 * onlar buradan silinebilir.
 */
function CalendarRow({ item }: { item: CalendarItemDto }) {
  const queryClient = useQueryClient();

  const isCustomEvent = item.link === null;

  const remove = useMutation({
    mutationFn: () => calendarApi.deleteEvent(item.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['calendar'] });
      toast.success(`“${item.title}” takvimden kaldırıldı.`);
    },
    // Silme yetkisi sunucuda denetlenir (oluşturan, lider, proje yöneticisi
    // veya yönetici); yetkisiz istekte hata mesajı gösterilir.
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const body = (
    <>
      <span
        className="h-8 w-0.5 shrink-0 rounded-full"
        style={{ backgroundColor: item.colorHex }}
        aria-hidden="true"
      />

      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{item.title}</p>
        <p className="text-xs text-muted-foreground">
          {item.isAllDay ? 'Tüm gün' : formatTime(item.startsAt)}
          {item.projectName ? ` · ${item.projectName}` : ''}
        </p>
      </div>

      <Badge variant="neutral" className="shrink-0">
        {calendarEventTypeLabels[item.type]}
      </Badge>
    </>
  );

  const rowClasses =
    'flex items-center gap-3 rounded-lg border border-border px-3 py-2 transition-colors hover:border-border-strong';

  if (item.link) {
    return (
      <Link
        to={item.link}
        className={cn(rowClasses, 'outline-none focus-visible:ring-2 focus-visible:ring-ring')}
      >
        {body}
      </Link>
    );
  }

  return (
    <div className={cn(rowClasses, 'group/row')}>
      {body}

      {isCustomEvent ? (
        <Button
          variant="ghost"
          size="icon-sm"
          disabled={remove.isPending}
          onClick={() => remove.mutate()}
          aria-label={`${item.title} etkinliğini sil`}
          className="shrink-0 opacity-0 group-hover/row:opacity-100 focus-visible:opacity-100"
        >
          <Trash2 aria-hidden="true" />
        </Button>
      ) : null}
    </div>
  );
}
