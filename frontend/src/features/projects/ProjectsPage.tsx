import { ProjectsPanel } from './ProjectsPanel';

export function ProjectsPage() {
  return (
    <div className="mx-auto w-full max-w-7xl space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Projeler</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Üyesi olduğunuz oyun projeleri. Görevler, sprintler ve sohbet her projede ayrı yönetilir.
        </p>
      </header>

      <ProjectsPanel />
    </div>
  );
}
