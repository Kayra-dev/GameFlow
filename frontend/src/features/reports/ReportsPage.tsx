import { useQuery } from '@tanstack/react-query';
import { BarChart3, CheckCircle2, ListChecks, TriangleAlert } from 'lucide-react';
import { useState } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip as ChartTooltip,
  XAxis,
  YAxis,
} from 'recharts';

import { Avatar } from '@/components/ui/avatar';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Progress } from '@/components/ui/progress';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { StatCard } from '@/features/dashboard/components/StatCard';
import { projectsApi } from '@/features/projects/api/projects-api';
import { queryKeys } from '@/lib/query-client';

import { reportsApi } from './api/reports-api';

/** Grafiklerde kullanılan ortak eksen/ızgara ayarları. */
const axisProps = {
  stroke: 'var(--subtle-foreground)',
  fontSize: 11,
  tickLine: false,
  axisLine: false,
} as const;

const tooltipStyle = {
  backgroundColor: 'var(--surface)',
  border: '1px solid var(--border)',
  borderRadius: '0.75rem',
  fontSize: '12px',
  color: 'var(--foreground)',
} as const;

export function ReportsPage() {
  const [projectFilter, setProjectFilter] = useState('all');

  const { data: projects } = useQuery({
    queryKey: queryKeys.projects.list({}),
    queryFn: () => projectsApi.list(),
  });

  const params = { projectId: projectFilter === 'all' ? undefined : projectFilter };

  const { data, isLoading } = useQuery({
    queryKey: queryKeys.reports(params),
    queryFn: () => reportsApi.get(params),
  });

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-7xl space-y-4">
        <Skeleton className="h-10 w-64" />
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-28 rounded-card" />
          ))}
        </div>
        <Skeleton className="h-80 rounded-card" />
      </div>
    );
  }

  if (!data || data.totalTaskCount === 0) {
    return (
      <div className="mx-auto w-full max-w-7xl space-y-5">
        <header>
          <h1 className="text-2xl font-semibold tracking-tight">Raporlar</h1>
        </header>
        <Card>
          <EmptyState
            icon={BarChart3}
            title="Raporlanacak veri yok"
            description="Görev oluşturup tamamladıkça buradaki grafikler dolmaya başlar."
          />
        </Card>
      </div>
    );
  }

  // Sıfır değerli dilimler pasta grafiğini kirletir.
  const statusPie = data.statusDistribution.filter((point) => point.value > 0);

  return (
    <div className="mx-auto w-full max-w-7xl space-y-5">
      <header className="flex flex-wrap items-center gap-3">
        <div className="flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">Raporlar</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Takım performansı, görev dağılımı ve sprint başarı oranları.
          </p>
        </div>

        <Select value={projectFilter} onValueChange={setProjectFilter}>
          <SelectTrigger className="sm:w-56" aria-label="Projeye göre filtrele">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm projeler</SelectItem>
            {projects?.map((project) => (
              <SelectItem key={project.id} value={project.id}>
                {project.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard icon={ListChecks} label="Toplam görev" value={data.totalTaskCount} />
        <StatCard
          icon={CheckCircle2}
          label="Tamamlanan"
          value={data.completedTaskCount}
          tone="success"
        />
        <StatCard
          icon={TriangleAlert}
          label="Geciken"
          value={data.overdueTaskCount}
          tone="danger"
        />
        <StatCard
          icon={BarChart3}
          label="Tamamlanma"
          value={`%${data.completionPercent}`}
          tone="primary"
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Durum dağılımı */}
        <Card>
          <CardHeader>
            <CardTitle>Durum dağılımı</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <Pie
                  data={statusPie}
                  dataKey="value"
                  nameKey="label"
                  innerRadius={60}
                  outerRadius={95}
                  paddingAngle={2}
                  strokeWidth={0}
                >
                  {statusPie.map((point) => (
                    <Cell key={point.label} fill={point.colorHex ?? 'var(--primary)'} />
                  ))}
                </Pie>
                <ChartTooltip contentStyle={tooltipStyle} />
              </PieChart>
            </ResponsiveContainer>

            <ul className="mt-2 flex flex-wrap justify-center gap-x-4 gap-y-1.5">
              {statusPie.map((point) => (
                <li key={point.label} className="flex items-center gap-1.5 text-xs">
                  <span
                    className="size-2 rounded-full"
                    style={{ backgroundColor: point.colorHex ?? 'var(--primary)' }}
                    aria-hidden="true"
                  />
                  {point.label}
                  <span className="tabular-nums text-muted-foreground">{point.value}</span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>

        {/* Öncelik dağılımı */}
        <Card>
          <CardHeader>
            <CardTitle>Öncelik dağılımı</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={data.priorityDistribution}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="label" {...axisProps} />
                <YAxis allowDecimals={false} {...axisProps} />
                <ChartTooltip contentStyle={tooltipStyle} />
                <Bar dataKey="value" radius={[6, 6, 0, 0]}>
                  {data.priorityDistribution.map((point) => (
                    <Cell key={point.label} fill={point.colorHex ?? 'var(--primary)'} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Haftalık trend */}
        <Card>
          <CardHeader>
            <CardTitle>Haftalık tamamlanan görevler</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={240}>
              <LineChart data={data.weeklyCompleted}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="label" {...axisProps} />
                <YAxis allowDecimals={false} {...axisProps} />
                <ChartTooltip contentStyle={tooltipStyle} />
                <Line
                  type="monotone"
                  dataKey="value"
                  stroke="var(--primary)"
                  strokeWidth={2}
                  dot={{ r: 3, fill: 'var(--primary)' }}
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Aylık trend */}
        <Card>
          <CardHeader>
            <CardTitle>Aylık tamamlanan görevler</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={data.monthlyCompleted}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="label" {...axisProps} />
                <YAxis allowDecimals={false} {...axisProps} />
                <ChartTooltip contentStyle={tooltipStyle} />
                <Bar dataKey="value" fill="var(--primary)" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* Sprint başarı oranı */}
      {data.sprintSuccess.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Sprint başarı oranı</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={data.sprintSuccess}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="label" {...axisProps} />
                <YAxis domain={[0, 100]} unit="%" {...axisProps} />
                <ChartTooltip contentStyle={tooltipStyle} formatter={(value) => `%${value}`} />
                <Bar dataKey="value" fill="var(--color-success)" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      ) : null}

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Takım performansı */}
        {data.teamPerformance.length > 0 ? (
          <Card>
            <CardHeader>
              <CardTitle>Takım performansı</CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-3">
                {data.teamPerformance.map((row) => (
                  <li key={row.teamId} className="space-y-1.5">
                    <div className="flex items-center gap-2">
                      <span
                        className="size-2 shrink-0 rounded-full"
                        style={{ backgroundColor: row.colorHex }}
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate text-sm font-medium">
                        {row.teamName}
                      </span>
                      <span className="shrink-0 text-xs tabular-nums text-muted-foreground">
                        %{row.completionPercent}
                      </span>
                    </div>
                    <Progress value={row.completionPercent} color={row.colorHex} />
                    <p className="text-xs text-muted-foreground">
                      {row.completedTaskCount} tamamlandı · {row.activeTaskCount} aktif
                      {row.overdueTaskCount > 0 ? (
                        <span className="text-danger"> · {row.overdueTaskCount} geciken</span>
                      ) : null}
                    </p>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        ) : null}

        {/* Kullanıcı performansı */}
        {data.userPerformance.length > 0 ? (
          <Card>
            <CardHeader>
              <CardTitle>Kişi performansı</CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="divide-y divide-border">
                {data.userPerformance.map((row) => (
                  <li key={row.userId} className="flex items-center gap-3 py-2.5">
                    <Avatar fullName={row.fullName} avatarUrl={row.avatarUrl} size="sm" />
                    <span className="min-w-0 flex-1 truncate text-sm">{row.fullName}</span>
                    <span className="shrink-0 text-xs text-muted-foreground">
                      <span className="text-success">{row.completedTaskCount}</span> /{' '}
                      {row.activeTaskCount} aktif
                      {row.storyPoints > 0 ? ` · ${row.storyPoints} puan` : ''}
                    </span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        ) : null}
      </div>
    </div>
  );
}
