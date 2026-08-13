import { useQuery } from '@tanstack/react-query';
import {
  AlarmClock,
  CalendarClock,
  CheckCircle2,
  ListChecks,
  Megaphone,
  Rocket,
  TriangleAlert,
  Users,
} from 'lucide-react';
import { Link } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Progress } from '@/components/ui/progress';
import { Skeleton } from '@/components/ui/skeleton';
import { formatDateTime, formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import { announcementPriorityLabels } from '@/types/enums';

import { dashboardApi } from './api/dashboard-api';
import { StatCard } from './components/StatCard';
import { TaskListCard } from './components/TaskListCard';

/**
 * Kartların sırayla belirmesi için basamaklı gecikme.
 * Animasyon salt dekoratiftir: çalışmasa bile kart görünür kalır
 * (bkz. globals.css içindeki fill-mode notu).
 */
function entrance(index: number) {
  return {
    className: 'animate-fade-up',
    style: { animationDelay: `${index * 60}ms` },
  };
}

export function DashboardPage() {
  const user = useAuthStore((state) => state.user);

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.dashboard(undefined, true),
    queryFn: () => dashboardApi.get({ onlyMyTasks: true }),
  });

  const firstName = user?.fullName.split(' ')[0] ?? '';

  if (isError) {
    return (
      <EmptyState
        icon={TriangleAlert}
        title="Panonuz yüklenemedi"
        description="Sunucuya ulaşılamıyor. Bağlantınızı kontrol edip sayfayı yenileyin."
      />
    );
  }

  return (
    <div className="mx-auto w-full max-w-7xl space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">
          Merhaba{firstName ? `, ${firstName}` : ''}
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Bugünün özeti ve seni bekleyen işler.
        </p>
      </header>

      {/* Özet kartları */}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {isLoading ? (
          Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-28 rounded-card" />
          ))
        ) : (
          <>
            <div {...entrance(0)}>
              <StatCard
                icon={ListChecks}
                label="Aktif görevlerim"
                value={data?.activeTaskCount ?? 0}
                tone="primary"
              />
            </div>
            <div {...entrance(1)}>
              <StatCard
                icon={CheckCircle2}
                label="Tamamlanan"
                value={data?.completedTaskCount ?? 0}
                tone="success"
              />
            </div>
            <div {...entrance(2)}>
              <StatCard
                icon={AlarmClock}
                label="Bugün bitecek"
                value={data?.todayTasks.length ?? 0}
                tone="warning"
              />
            </div>
            <div {...entrance(3)}>
              <StatCard
                icon={TriangleAlert}
                label="Geciken"
                value={data?.overdueTasks.length ?? 0}
                tone="danger"
              />
            </div>
          </>
        )}
      </div>

      {/* Tamamlanma yüzdesi */}
      {isLoading ? (
        <Skeleton className="h-24 rounded-card" />
      ) : (
        <div {...entrance(4)}>
          <Card>
            <CardContent className="pt-5">
              <div className="flex items-baseline justify-between gap-4">
                <div>
                  <p className="text-sm font-medium">Görev tamamlama oranı</p>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {data?.completedTaskCount ?? 0} / {data?.totalTaskCount ?? 0} görev tamamlandı
                  </p>
                </div>
                <p className="text-2xl font-semibold tabular-nums">
                  %{data?.completionPercent ?? 0}
                </p>
              </div>
              <Progress value={data?.completionPercent ?? 0} className="mt-4 h-2" />
            </CardContent>
          </Card>
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-3">
        {/* Sol kolon: görev listeleri */}
        <div className="space-y-4 lg:col-span-2">
          <TaskListCard
            title="Bugün bitecek görevler"
            icon={AlarmClock}
            tasks={data?.todayTasks ?? []}
            isLoading={isLoading}
            emptyMessage="Bugün için son teslim tarihi olan görev yok."
          />
          <TaskListCard
            title="Geciken görevler"
            icon={TriangleAlert}
            tasks={data?.overdueTasks ?? []}
            isLoading={isLoading}
            emptyMessage="Geciken görev yok, harika."
          />
          <TaskListCard
            title="Yaklaşan son tarihler"
            icon={CalendarClock}
            tasks={data?.upcomingDeadlines ?? []}
            isLoading={isLoading}
            emptyMessage="Önümüzdeki 7 günde son teslim tarihi yok."
          />
        </div>

        {/* Sağ kolon: bağlam kartları */}
        <div className="space-y-4">
          <Card>
            <CardHeader className="flex-row items-center gap-2">
              <Rocket className="size-4 text-subtle-foreground" aria-hidden="true" />
              <CardTitle>Aktif sprintler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {isLoading ? (
                <Skeleton className="h-16" />
              ) : data?.activeSprints.length ? (
                data.activeSprints.map((sprint) => (
                  <Link
                    key={sprint.id}
                    to={`/sprintler/${sprint.id}`}
                    className="block rounded-lg border border-border p-3 transition-colors hover:border-border-strong"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <p className="truncate text-sm font-medium">{sprint.name}</p>
                      <span className="shrink-0 text-xs tabular-nums text-muted-foreground">
                        %{sprint.progressPercent}
                      </span>
                    </div>
                    <Progress value={sprint.progressPercent} className="mt-2" />
                    <p className="mt-2 text-xs text-muted-foreground">
                      {sprint.completedTaskCount}/{sprint.taskCount} görev
                    </p>
                  </Link>
                ))
              ) : (
                <p className="py-2 text-sm text-muted-foreground">Aktif sprint yok.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center gap-2">
              <Megaphone className="size-4 text-subtle-foreground" aria-hidden="true" />
              <CardTitle>Duyurular</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {isLoading ? (
                <Skeleton className="h-16" />
              ) : data?.announcements.length ? (
                data.announcements.map((announcement) => (
                  <div key={announcement.id} className="space-y-1.5">
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-sm font-medium">{announcement.title}</p>
                      <Badge
                        variant={
                          announcement.priority === 3
                            ? 'danger'
                            : announcement.priority === 2
                              ? 'warning'
                              : 'neutral'
                        }
                      >
                        {announcementPriorityLabels[announcement.priority]}
                      </Badge>
                    </div>
                    <p className="line-clamp-2 text-xs text-muted-foreground">
                      {announcement.content}
                    </p>
                    <p className="text-[11px] text-subtle-foreground">
                      {announcement.author.fullName} · {formatRelative(announcement.publishedAt)}
                    </p>
                  </div>
                ))
              ) : (
                <p className="py-2 text-sm text-muted-foreground">Duyuru yok.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center gap-2">
              <CalendarClock className="size-4 text-subtle-foreground" aria-hidden="true" />
              <CardTitle>Yaklaşan toplantılar</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {isLoading ? (
                <Skeleton className="h-16" />
              ) : data?.upcomingMeetings.length ? (
                data.upcomingMeetings.map((meeting) => (
                  <Link
                    key={meeting.id}
                    to={`/toplantilar/${meeting.id}`}
                    className="block space-y-1 rounded-md outline-none hover:underline focus-visible:ring-2 focus-visible:ring-ring"
                  >
                    <p className="truncate text-sm font-medium">{meeting.title}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatDateTime(meeting.startsAt)}
                    </p>
                  </Link>
                ))
              ) : (
                <p className="py-2 text-sm text-muted-foreground">Yaklaşan toplantı yok.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center gap-2">
              <Users className="size-4 text-subtle-foreground" aria-hidden="true" />
              <CardTitle>Çevrimiçi</CardTitle>
            </CardHeader>
            <CardContent>
              {isLoading ? (
                <Skeleton className="h-10" />
              ) : data?.onlineUsers.length ? (
                <div className="flex flex-wrap gap-2">
                  {data.onlineUsers.map((onlineUser) => (
                    <Avatar
                      key={onlineUser.id}
                      fullName={onlineUser.fullName}
                      avatarUrl={onlineUser.avatarUrl}
                      size="sm"
                      isOnline
                    />
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Şu anda başka kimse çevrimiçi değil.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Son aktiviteler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {isLoading ? (
                <Skeleton className="h-24" />
              ) : data?.recentActivities.length ? (
                data.recentActivities.slice(0, 8).map((activity) => (
                  <div key={activity.id} className="flex gap-2.5">
                    <Avatar
                      fullName={activity.actor?.fullName ?? 'Sistem'}
                      avatarUrl={activity.actor?.avatarUrl}
                      size="xs"
                    />
                    <div className="min-w-0 flex-1">
                      <p className="text-xs leading-relaxed text-foreground">
                        {activity.description}
                      </p>
                      <p className="text-[11px] text-subtle-foreground">
                        {formatRelative(activity.createdAt)}
                      </p>
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-sm text-muted-foreground">Henüz aktivite yok.</p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
