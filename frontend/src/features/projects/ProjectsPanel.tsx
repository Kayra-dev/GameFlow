import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  FolderKanban,
  Gamepad2,
  MoreHorizontal,
  Pencil,
  Search,
  SquareKanban,
  Trash2,
  Users,
} from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { EmptyState } from '@/components/ui/empty-state';
import { Input } from '@/components/ui/input';
import { Progress } from '@/components/ui/progress';
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
import { isAdmin, useAuthStore } from '@/stores/auth-store';
import type { ProjectSummary } from '@/types/api';
import { ProjectStatus, projectStatusLabels } from '@/types/enums';

import { projectsApi } from './api/projects-api';
import { ProjectFormDialog } from './components/ProjectFormDialog';

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

/**
 * Proje listesi. Hem /projeler sayfasında hem yönetim panelinin Projeler
 * sekmesinde kullanılır; tek fark başlık, o yüzden dışarıdan verilir.
 */
export function ProjectsPanel() {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [formOpen, setFormOpen] = useState(false);
  const [editingProject, setEditingProject] = useState<ProjectSummary | null>(null);
  const [deletingProject, setDeletingProject] = useState<ProjectSummary | null>(null);

  const debouncedSearch = useDebouncedValue(search, 300);
  const user = useAuthStore((state) => state.user);
  const canCreate = isAdmin(user);

  const params = {
    search: debouncedSearch || undefined,
    status: statusFilter === 'all' ? undefined : (Number(statusFilter) as ProjectStatus),
  };

  const { data: projects, isLoading, isError } = useQuery({
    queryKey: queryKeys.projects.list(params),
    queryFn: () => projectsApi.list(params),
  });

  const openCreate = () => {
    setEditingProject(null);
    setFormOpen(true);
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1 sm:max-w-xs">
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-subtle-foreground"
            aria-hidden="true"
          />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Proje adı veya anahtar ara…"
            aria-label="Proje ara"
            className="pl-9"
          />
        </div>

        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="sm:w-48" aria-label="Duruma göre filtrele">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm durumlar</SelectItem>
            {Object.entries(projectStatusLabels).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {canCreate ? (
          <Button onClick={openCreate} className="sm:ml-auto">
            <FolderKanban aria-hidden="true" />
            Yeni proje
          </Button>
        ) : null}
      </div>

      {isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-48 rounded-card" />
          ))}
        </div>
      ) : isError ? (
        <Card>
          <EmptyState
            icon={FolderKanban}
            title="Projeler yüklenemedi"
            description="Sunucuya ulaşılamıyor. Sayfayı yenileyip tekrar deneyin."
          />
        </Card>
      ) : projects && projects.length === 0 ? (
        <Card>
          <EmptyState
            icon={Gamepad2}
            title={debouncedSearch ? 'Sonuç bulunamadı' : 'Henüz proje yok'}
            description={
              debouncedSearch
                ? `“${debouncedSearch}” aramasıyla eşleşen proje yok.`
                : canCreate
                  ? 'Görev, kanban ve sprint kullanabilmek için önce bir proje oluşturun.'
                  : 'Henüz bir projeye eklenmemişsiniz. Yöneticinizle görüşün.'
            }
            action={
              canCreate && !debouncedSearch ? (
                <Button variant="secondary" onClick={openCreate}>
                  <FolderKanban aria-hidden="true" />
                  İlk projeyi oluştur
                </Button>
              ) : null
            }
          />
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {projects?.map((project) => (
            <ProjectCard
              key={project.id}
              project={project}
              canManage={canCreate}
              onEdit={() => {
                setEditingProject(project);
                setFormOpen(true);
              }}
              onDelete={() => setDeletingProject(project)}
            />
          ))}
        </div>
      )}

      <ProjectFormDialog open={formOpen} onOpenChange={setFormOpen} project={editingProject} />

      <DeleteProjectDialog
        project={deletingProject}
        onClose={() => setDeletingProject(null)}
      />
    </div>
  );
}

function ProjectCard({
  project,
  canManage,
  onEdit,
  onDelete,
}: {
  project: ProjectSummary;
  canManage: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  // İptal edilen görevler yüzdeye katılmadığı için oran sunucudaki hesapla
  // aynı kalsın diye tamamlanan/toplam üzerinden gösterilir.
  const progressPercent =
    project.taskCount === 0
      ? 0
      : Math.round((project.completedTaskCount / project.taskCount) * 100);

  return (
    <Card className="flex h-full flex-col">
      <CardContent className="flex flex-1 flex-col gap-3 pt-5">
        <div className="flex items-start gap-3">
          <div
            className="grid size-10 shrink-0 place-items-center rounded-xl font-mono text-xs font-semibold text-white"
            style={{ backgroundColor: project.colorHex }}
            aria-hidden="true"
          >
            {project.key.slice(0, 3)}
          </div>

          <div className="min-w-0 flex-1">
            <Link
              to={`/projeler/${project.id}`}
              className="block truncate text-sm font-semibold outline-none hover:text-primary focus-visible:ring-2 focus-visible:ring-ring"
            >
              {project.name}
            </Link>
            <p className="font-mono text-xs text-subtle-foreground">{project.key}</p>
          </div>

          {canManage ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon-sm" aria-label={`${project.name} işlemleri`}>
                  <MoreHorizontal aria-hidden="true" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onSelect={onEdit}>
                  <Pencil aria-hidden="true" />
                  Düzenle
                </DropdownMenuItem>
                <DropdownMenuItem variant="danger" onSelect={onDelete}>
                  <Trash2 aria-hidden="true" />
                  Sil
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : null}
        </div>

        <Badge variant={statusVariant[project.status]} className="w-fit">
          {projectStatusLabels[project.status]}
        </Badge>

        <div className="mt-auto space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="text-muted-foreground">İlerleme</span>
            <span className="tabular-nums">%{progressPercent}</span>
          </div>
          <Progress value={progressPercent} color={project.colorHex} />

          <div className="flex items-center gap-4 pt-1 text-xs text-muted-foreground">
            <span className="flex items-center gap-1.5">
              <SquareKanban className="size-3.5" aria-hidden="true" />
              {project.completedTaskCount}/{project.taskCount} görev
            </span>
            <span className="flex items-center gap-1.5">
              <Users className="size-3.5" aria-hidden="true" />
              {project.memberCount} üye
            </span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function DeleteProjectDialog({
  project,
  onClose,
}: {
  project: ProjectSummary | null;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();

  const remove = useMutation({
    mutationFn: () => projectsApi.remove(project!.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects.all });
      // Görevler de projeyle birlikte gizlenir.
      void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });

      toast.success(`${project?.name} silindi.`);
      onClose();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog open={Boolean(project)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Projeyi sil</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{project?.name}</strong> ve içindeki{' '}
            {project?.taskCount ?? 0} görev erişilemez hâle gelecek. Sohbet geçmişi ve denetim
            kayıtları korunur.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Vazgeç
          </Button>
          <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
            <Trash2 aria-hidden="true" />
            Sil
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
