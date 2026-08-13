import { TeamsPanel } from './TeamsPanel';

export function TeamsPage() {
  return (
    <div className="mx-auto w-full max-w-7xl space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Takımlar</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Stüdyo departmanları. Her takımın kendi sohbet odası, görevleri ve ilerlemesi var.
        </p>
      </header>

      <TeamsPanel />
    </div>
  );
}
