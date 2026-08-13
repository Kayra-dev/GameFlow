import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Crown, MessageSquare, TriangleAlert, Users } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Progress } from '@/components/ui/progress';
import { Skeleton } from '@/components/ui/skeleton';
import { StatCard } from '@/features/dashboard/components/StatCard';
import { formatDate } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { TeamRole, teamCategoryLabels, teamRoleLabels } from '@/types/enums';

import { teamsApi } from './api/teams-api';

export function TeamDetailPage() {
  const { teamId = '' } = useParams();

  const { data: team, isLoading, isError } = useQuery({
    queryKey: queryKeys.teams.detail(teamId),
    queryFn: () => teamsApi.detail(teamId),
    enabled: Boolean(teamId),
  });

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-5xl space-y-4">
        <Skeleton className="h-32 rounded-card" />
        <Skeleton className="h-64 rounded-card" />
      </div>
    );
  }

  if (isError || !team) {
    return (
      <EmptyState
        icon={TriangleAlert}
        title="Takım bulunamadı"
        description="Takım silinmiş olabilir veya erişim yetkiniz yok."
        action={
          <Button asChild variant="secondary">
            <Link to="/takimlar">Takımlara dön</Link>
          </Button>
        }
      />
    );
  }

  return (
    <div className="mx-auto w-full max-w-5xl space-y-4">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/takimlar">
          <ArrowLeft aria-hidden="true" />
          Takımlar
        </Link>
      </Button>

      <Card>
        <CardContent className="space-y-4 pt-5">
          <div className="flex flex-wrap items-start gap-4">
            <div
              className="size-12 shrink-0 rounded-xl"
              style={{ backgroundColor: team.colorHex }}
              aria-hidden="true"
            />

            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-xl font-semibold tracking-tight">{team.name}</h1>
                <Badge variant="neutral">{teamCategoryLabels[team.category]}</Badge>
              </div>
              {team.description ? (
                <p className="mt-1 text-sm text-muted-foreground">{team.description}</p>
              ) : null}
              <p className="mt-1 text-xs text-subtle-foreground">
                Kuruluş: {formatDate(team.createdAt)}
              </p>
            </div>

            {team.chatRoomId ? (
              <Button asChild variant="secondary">
                <Link to={`/sohbet/${team.chatRoomId}`}>
                  <MessageSquare aria-hidden="true" />
                  Takım sohbeti
                </Link>
              </Button>
            ) : null}
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center justify-between text-xs">
              <span className="text-muted-foreground">
                {team.completedTaskCount}/{team.totalTaskCount} görev tamamlandı
              </span>
              <span className="tabular-nums">%{team.progressPercent}</span>
            </div>
            <Progress value={team.progressPercent} color={team.colorHex} className="h-2" />
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard icon={Users} label="Üye" value={team.memberCount} />
        <StatCard icon={Users} label="Aktif görev" value={team.activeTaskCount} tone="primary" />
        <StatCard
          icon={TriangleAlert}
          label="Geciken"
          value={team.overdueTaskCount}
          tone="danger"
        />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Üyeler</CardTitle>
        </CardHeader>
        <CardContent>
          {team.members.length === 0 ? (
            <p className="text-sm text-muted-foreground">Bu takımda henüz üye yok.</p>
          ) : (
            <ul className="divide-y divide-border">
              {team.members.map((member) => (
                <li key={member.id}>
                  <Link
                    to={`/kisiler/${member.user.id}`}
                    className="flex items-center gap-3 py-2.5 transition-colors hover:bg-surface-raised/40"
                  >
                    <Avatar
                      fullName={member.user.fullName}
                      avatarUrl={member.user.avatarUrl}
                      size="sm"
                      isOnline={member.user.isOnline}
                    />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{member.user.fullName}</p>
                      <p className="truncate text-xs text-muted-foreground">
                        {member.user.jobTitle ?? member.user.email}
                      </p>
                    </div>

                    {member.teamRole === TeamRole.Leader ? (
                      <Badge variant="warning">
                        <Crown aria-hidden="true" />
                        {teamRoleLabels[TeamRole.Leader]}
                      </Badge>
                    ) : null}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
