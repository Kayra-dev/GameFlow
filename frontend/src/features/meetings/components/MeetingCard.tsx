import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Check, ExternalLink, MapPin, Trash2, Users, Video, X } from 'lucide-react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Tooltip } from '@/components/ui/tooltip';
import { getErrorMessage } from '@/lib/api-client';
import { formatDateTime, formatTime } from '@/lib/dates';
import { cn } from '@/lib/utils';
import { isAdmin, useAuthStore } from '@/stores/auth-store';
import type { MeetingDto } from '@/types/api';
import { MeetingStatus, meetingStatusLabels } from '@/types/enums';

import { meetingsApi } from '../api/meetings-api';

const statusVariant: Record<MeetingStatus, 'primary' | 'info' | 'success' | 'neutral'> = {
  [MeetingStatus.Scheduled]: 'primary',
  [MeetingStatus.InProgress]: 'info',
  [MeetingStatus.Completed]: 'success',
  [MeetingStatus.Cancelled]: 'neutral',
};

type MeetingCardProps = {
  meeting: MeetingDto;
  onDelete: (meeting: MeetingDto) => void;
  /** Ayrıntı sayfasında başlık zaten bağlantı değil. */
  linkToDetail?: boolean;
};

/**
 * Tek bir toplantı. Katılım yanıtı ve silme işlemleri buradadır; her ikisi de
 * sunucuda ayrıca yetkilendirilir, buradaki denetim yalnızca arayüzü sadeleştirir.
 */
export function MeetingCard({ meeting, onDelete, linkToDetail = true }: MeetingCardProps) {
  const queryClient = useQueryClient();
  const currentUser = useAuthStore((state) => state.user);

  const canDelete = isAdmin(currentUser) || meeting.organizer.id === currentUser?.id;

  const canRespond =
    meeting.status === MeetingStatus.Scheduled &&
    meeting.attendees.some((attendee) => attendee.user.id === currentUser?.id);

  const respond = useMutation({
    mutationFn: (isAccepted: boolean) => meetingsApi.respond(meeting.id, isAccepted),
    onSuccess: (updated) => {
      void queryClient.invalidateQueries({ queryKey: ['meetings'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });

      toast.success(
        updated.myResponse
          ? `“${meeting.title}” toplantısına katılacaksınız.`
          : `“${meeting.title}” toplantısına katılmayacaksınız.`,
      );
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const acceptedCount = meeting.attendees.filter((attendee) => attendee.isAccepted).length;
  const declinedCount = meeting.attendees.filter(
    (attendee) => attendee.isAccepted === false,
  ).length;

  return (
    <Card className="p-4">
      <div className="flex flex-wrap items-start gap-4">
        <div className="min-w-0 flex-1 basis-[min(100%,16rem)]">
          <div className="flex flex-wrap items-center gap-2">
            {linkToDetail ? (
              <Link
                to={`/toplantilar/${meeting.id}`}
                className="truncate text-sm font-semibold outline-none hover:underline focus-visible:ring-2 focus-visible:ring-ring"
              >
                {meeting.title}
              </Link>
            ) : (
              <h3 className="truncate text-sm font-semibold">{meeting.title}</h3>
            )}

            <Badge variant={statusVariant[meeting.status]}>
              {meetingStatusLabels[meeting.status]}
            </Badge>

            {meeting.projectName ? (
              <Badge variant="neutral">{meeting.projectName}</Badge>
            ) : meeting.teamName ? (
              <Badge variant="neutral">{meeting.teamName}</Badge>
            ) : null}

            {meeting.myResponse === true ? (
              <Badge variant="success">Katılıyorsunuz</Badge>
            ) : meeting.myResponse === false ? (
              <Badge variant="danger">Katılmıyorsunuz</Badge>
            ) : canRespond ? (
              <Badge variant="warning">Yanıt bekleniyor</Badge>
            ) : null}
          </div>

          <p className="mt-1 text-xs text-muted-foreground">
            {formatDateTime(meeting.startsAt)} – {formatTime(meeting.endsAt)}
          </p>

          {meeting.description ? (
            <p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{meeting.description}</p>
          ) : null}

          <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-muted-foreground">
            {meeting.location ? (
              <span className="flex items-center gap-1.5">
                <MapPin className="size-3.5" aria-hidden="true" />
                {meeting.location}
              </span>
            ) : null}

            {meeting.meetingUrl ? (
              <a
                href={meeting.meetingUrl}
                target="_blank"
                rel="noreferrer noopener"
                className="flex items-center gap-1.5 text-primary hover:underline"
              >
                <Video className="size-3.5" aria-hidden="true" />
                Toplantıya katıl
                <ExternalLink className="size-3" aria-hidden="true" />
              </a>
            ) : null}

            <span className="flex items-center gap-1.5">
              <Users className="size-3.5" aria-hidden="true" />
              {acceptedCount} katılıyor
              {declinedCount > 0 ? ` · ${declinedCount} katılmıyor` : ''} ·{' '}
              {meeting.attendees.length} davetli
            </span>

            <span>Düzenleyen: {meeting.organizer.fullName}</span>
          </div>

          <div className="mt-2.5 flex items-center">
            {meeting.attendees.slice(0, 8).map((attendee, index) => (
              <Tooltip
                key={attendee.user.id}
                content={`${attendee.user.fullName} · ${
                  attendee.isAccepted === null
                    ? 'yanıt bekleniyor'
                    : attendee.isAccepted
                      ? 'katılıyor'
                      : 'katılmıyor'
                }`}
              >
                <span className={index > 0 ? '-ml-2' : undefined}>
                  <Avatar
                    fullName={attendee.user.fullName}
                    avatarUrl={attendee.user.avatarUrl}
                    size="xs"
                    className={cn(
                      'ring-2 ring-surface',
                      attendee.isAccepted === false && 'opacity-40 grayscale',
                    )}
                  />
                </span>
              </Tooltip>
            ))}
            {meeting.attendees.length > 8 ? (
              <span className="-ml-2 grid size-6 place-items-center rounded-full bg-surface-raised text-[10px] font-medium ring-2 ring-surface">
                +{meeting.attendees.length - 8}
              </span>
            ) : null}
          </div>
        </div>

        <div className="flex shrink-0 flex-wrap items-center gap-2">
          {canRespond ? (
            <>
              <Button
                variant={meeting.myResponse === true ? 'primary' : 'secondary'}
                size="sm"
                disabled={respond.isPending}
                onClick={() => respond.mutate(true)}
              >
                <Check aria-hidden="true" />
                Katılacağım
              </Button>
              <Button
                variant={meeting.myResponse === false ? 'danger' : 'secondary'}
                size="sm"
                disabled={respond.isPending}
                onClick={() => respond.mutate(false)}
              >
                <X aria-hidden="true" />
                Katılmayacağım
              </Button>
            </>
          ) : null}

          {canDelete ? (
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={() => onDelete(meeting)}
              aria-label={`${meeting.title} toplantısını iptal et`}
            >
              <Trash2 aria-hidden="true" />
            </Button>
          ) : null}
        </div>
      </div>
    </Card>
  );
}
