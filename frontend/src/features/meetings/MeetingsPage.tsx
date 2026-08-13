import { useQuery } from '@tanstack/react-query';
import { CalendarClock, Plus } from 'lucide-react';
import { useState } from 'react';

import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { queryKeys } from '@/lib/query-client';
import { isLeader, useAuthStore } from '@/stores/auth-store';
import type { MeetingDto } from '@/types/api';

import { meetingsApi, type MeetingListParams } from './api/meetings-api';
import { DeleteMeetingDialog } from './components/DeleteMeetingDialog';
import { MeetingCard } from './components/MeetingCard';
import { MeetingFormDialog } from './components/MeetingFormDialog';

type Scope = 'upcoming' | 'mine' | 'all';

const scopeParams: Record<Scope, MeetingListParams> = {
  upcoming: { onlyUpcoming: true },
  mine: { onlyMine: true },
  all: {},
};

const emptyDescriptions: Record<Scope, string> = {
  upcoming: 'Gelecek tarihli toplantı bulunmuyor.',
  mine: 'Düzenlediğiniz veya davetli olduğunuz toplantı yok.',
  all: 'Görebildiğiniz hiçbir toplantı yok.',
};

/** Toplantı listesi. Oluşturma yetkisi lider ve yöneticidedir. */
export function MeetingsPage() {
  const user = useAuthStore((state) => state.user);

  const [scope, setScope] = useState<Scope>('upcoming');
  const [formOpen, setFormOpen] = useState(false);
  const [deleting, setDeleting] = useState<MeetingDto | null>(null);

  const params = scopeParams[scope];

  const { data: meetings, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings(params),
    queryFn: () => meetingsApi.list(params),
  });

  // Sunucu ayrıca takım/proje bazlı denetim yapar; burada yalnızca üyelere
  // hiç işe yaramayacak bir düğme gösterilmemesi amaçlanır.
  const canCreate = isLeader(user);

  return (
    <div className="mx-auto w-full max-w-5xl space-y-5">
      <header className="flex flex-wrap items-start gap-4">
        <div className="min-w-0 flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">Toplantılar</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Planlanan toplantılar, katılımcılar ve katılım yanıtları.
          </p>
        </div>

        {canCreate ? (
          <Button onClick={() => setFormOpen(true)}>
            <Plus aria-hidden="true" />
            Yeni toplantı
          </Button>
        ) : null}
      </header>

      <Tabs value={scope} onValueChange={(value) => setScope(value as Scope)}>
        <TabsList>
          <TabsTrigger value="upcoming">Yaklaşan</TabsTrigger>
          <TabsTrigger value="mine">Beni ilgilendiren</TabsTrigger>
          <TabsTrigger value="all">Tümü</TabsTrigger>
        </TabsList>
      </Tabs>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-36 rounded-card" />
          ))}
        </div>
      ) : isError ? (
        <Card>
          <EmptyState
            icon={CalendarClock}
            title="Toplantılar yüklenemedi"
            description="Sunucuya ulaşılamıyor. Sayfayı yenileyip tekrar deneyin."
          />
        </Card>
      ) : !meetings || meetings.length === 0 ? (
        <Card>
          <EmptyState
            icon={CalendarClock}
            title="Toplantı yok"
            description={emptyDescriptions[scope]}
            action={
              canCreate ? (
                <Button variant="secondary" onClick={() => setFormOpen(true)}>
                  <Plus aria-hidden="true" />
                  Yeni toplantı
                </Button>
              ) : null
            }
          />
        </Card>
      ) : (
        <div className="space-y-3">
          {meetings.map((meeting) => (
            <MeetingCard key={meeting.id} meeting={meeting} onDelete={setDeleting} />
          ))}
        </div>
      )}

      <MeetingFormDialog open={formOpen} onOpenChange={setFormOpen} />

      <DeleteMeetingDialog meeting={deleting} onClose={() => setDeleting(null)} />
    </div>
  );
}
