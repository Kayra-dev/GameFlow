import { FolderKanban, Users as UsersIcon, UserCog } from 'lucide-react';

import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ProjectsPanel } from '@/features/projects/ProjectsPanel';
import { TeamsPanel } from '@/features/teams/TeamsPanel';
import { UsersPanel } from '@/features/users/UsersPanel';

/**
 * Yönetim paneli. Yalnızca yöneticiler erişebilir (rota seviyesinde korunur,
 * ayrıca her uç nokta sunucuda da yetkilendirilir).
 */
export function AdminPage() {
  return (
    <div className="mx-auto w-full max-w-7xl space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Yönetim paneli</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Kullanıcı hesapları, takımlar ve projeleri buradan yönetin.
        </p>
      </header>

      <Tabs defaultValue="users">
        <TabsList>
          <TabsTrigger value="users">
            <UserCog aria-hidden="true" />
            Kullanıcılar
          </TabsTrigger>
          <TabsTrigger value="teams">
            <UsersIcon aria-hidden="true" />
            Takımlar
          </TabsTrigger>
          <TabsTrigger value="projects">
            <FolderKanban aria-hidden="true" />
            Projeler
          </TabsTrigger>
        </TabsList>

        <TabsContent value="users">
          <UsersPanel />
        </TabsContent>

        <TabsContent value="teams">
          <TeamsPanel />
        </TabsContent>

        <TabsContent value="projects">
          <ProjectsPanel />
        </TabsContent>
      </Tabs>
    </div>
  );
}
