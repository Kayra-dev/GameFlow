import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, TriangleAlert } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { Skeleton } from '@/components/ui/skeleton';
import { formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import type { MeetingDto } from '@/types/api';

import { meetingsApi } from './api/meetings-api';
import { DeleteMeetingDialog } from './components/DeleteMeetingDialog';
import { MeetingCard } from './components/MeetingCard';

/** Toplantı ayrıntısı. Takvimdeki toplantı kayıtları buraya bağlanır. */
export function MeetingDetailPage() {
  const { meetingId = '' } = useParams();
  const navigate = useNavigate();
  const [deleting, setDeleting] = useState<MeetingDto | null>(null);

  const { data: meeting, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings({ id: meetingId }),
    queryFn: () => meetingsApi.detail(meetingId),
    enabled: Boolean(meetingId),
  });

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-3xl space-y-5">
        <Skeleton className="h-40 rounded-card" />
        <Skeleton className="h-64 rounded-card" />
      </div>
    );
  }

  if (isError || !meeting) {
    return (
      <EmptyState
        icon={TriangleAlert}
        title="Toplantı bulunamadı"
        description="Toplantı iptal edilmiş olabilir veya görme yetkiniz yok."
        action={
          <Button asChild variant="secondary">
            <Link to="/toplantilar">Toplantılara dön</Link>
          </Button>
        }
      />
    );
  }

  return (
    <div className="mx-auto w-full max-w-3xl space-y-5">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/toplantilar">
          <ArrowLeft aria-hidden="true" />
          Toplantılar
        </Link>
      </Button>

      <MeetingCard meeting={meeting} onDelete={setDeleting} linkToDetail={false} />

      {meeting.description ? (
        <Card>
          <CardContent className="pt-5">
            <h2 className="mb-2 text-sm font-semibold">Gündem</h2>
            <p className="text-sm whitespace-pre-wrap text-muted-foreground">
              {meeting.description}
            </p>
          </CardContent>
        </Card>
      ) : null}

      <Card>
        <CardContent className="pt-5">
          <h2 className="mb-3 text-sm font-semibold">
            Katılımcılar ({meeting.attendees.length})
          </h2>

          <ul className="divide-y divide-border">
            {meeting.attendees.map((attendee) => (
              <li key={attendee.user.id} className="flex items-center gap-3 py-2.5">
                <Avatar
                  fullName={attendee.user.fullName}
                  avatarUrl={attendee.user.avatarUrl}
                  size="sm"
                  isOnline={attendee.user.isOnline}
                />

                <div className="min-w-0 flex-1">
                  <Link
                    to={`/kisiler/${attendee.user.id}`}
                    className="truncate text-sm font-medium hover:underline"
                  >
                    {attendee.user.fullName}
                  </Link>
                  <p className="truncate text-xs text-muted-foreground">
                    {attendee.user.jobTitle ?? attendee.user.email}
                  </p>
                </div>

                {attendee.respondedAt ? (
                  <span className="hidden shrink-0 text-xs text-subtle-foreground sm:inline">
                    {formatRelative(attendee.respondedAt)}
                  </span>
                ) : null}

                {attendee.isAccepted === null ? (
                  <Badge variant="warning">Yanıt yok</Badge>
                ) : attendee.isAccepted ? (
                  <Badge variant="success">Katılıyor</Badge>
                ) : (
                  <Badge variant="danger">Katılmıyor</Badge>
                )}
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>

      <DeleteMeetingDialog
        meeting={deleting}
        onClose={() => setDeleting(null)}
        onDeleted={() => navigate('/toplantilar')}
      />
    </div>
  );
}
