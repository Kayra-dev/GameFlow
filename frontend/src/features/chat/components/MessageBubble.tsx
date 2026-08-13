import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Check, CheckCheck, Download, Pencil, Reply, Trash2, X } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { Avatar } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/input';
import { API_BASE_URL, getErrorMessage } from '@/lib/api-client';
import { formatTime } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { cn, formatFileSize } from '@/lib/utils';
import { isLeader, useAuthStore } from '@/stores/auth-store';
import type { MessageDto } from '@/types/api';
import { AttachmentCategory } from '@/types/enums';

import { chatApi } from '../api/chat-api';

type MessageBubbleProps = {
  message: MessageDto;
  roomId: string;
  isOwn: boolean;
  /** Aynı kişinin art arda mesajı: avatar ve ad gizlenir. */
  isGrouped: boolean;
  onReply: (message: MessageDto) => void;
};

function toAbsoluteUrl(url: string): string {
  return url.startsWith('http') ? url : `${API_BASE_URL}${url}`;
}

export function MessageBubble({
  message,
  roomId,
  isOwn,
  isGrouped,
  onReply,
}: MessageBubbleProps) {
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const [isEditing, setEditing] = useState(false);
  const [editDraft, setEditDraft] = useState(message.content);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId) });
    void queryClient.invalidateQueries({ queryKey: queryKeys.chat.rooms });
  };

  const edit = useMutation({
    mutationFn: (content: string) => chatApi.edit(roomId, message.id, content),
    onSuccess: () => {
      invalidate();
      setEditing(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: () => chatApi.remove(roomId, message.id),
    onSuccess: invalidate,
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  // Mesajı yalnızca göndereni düzenleyebilir; silmeyi ayrıca liderler de yapabilir.
  const canEdit = isOwn;
  const canDelete = isOwn || isLeader(user);

  return (
    <div className={cn('group flex gap-2.5', isGrouped ? 'mt-0.5' : 'mt-3')}>
      {/* Gruplanan mesajlarda avatar yerine boşluk bırakılır ki hizalama bozulmasın */}
      <div className="w-8 shrink-0">
        {!isGrouped ? (
          <Avatar
            fullName={message.sender.fullName}
            avatarUrl={message.sender.avatarUrl}
            size="sm"
            isOnline={message.sender.isOnline}
          />
        ) : null}
      </div>

      <div className="min-w-0 flex-1">
        {!isGrouped ? (
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium">{message.sender.fullName}</span>
            <span className="text-[11px] text-subtle-foreground">
              {formatTime(message.createdAt)}
            </span>
          </div>
        ) : null}

        {/* Yanıtlanan mesaj alıntısı */}
        {message.replyToMessageId && message.replyToPreview ? (
          <div className="mt-1 mb-1 border-l-2 border-border-strong pl-2">
            <p className="text-[11px] font-medium text-muted-foreground">
              {message.replyToSenderName}
            </p>
            <p className="truncate text-xs text-subtle-foreground">{message.replyToPreview}</p>
          </div>
        ) : null}

        {isEditing ? (
          <div className="mt-1 space-y-2">
            <Textarea
              value={editDraft}
              onChange={(event) => setEditDraft(event.target.value)}
              rows={2}
              autoFocus
              aria-label="Mesajı düzenle"
            />
            <div className="flex gap-2">
              <Button
                size="sm"
                onClick={() => editDraft.trim() && edit.mutate(editDraft.trim())}
                disabled={!editDraft.trim() || edit.isPending}
              >
                Kaydet
              </Button>
              <Button
                size="sm"
                variant="secondary"
                onClick={() => {
                  setEditing(false);
                  setEditDraft(message.content);
                }}
              >
                <X aria-hidden="true" />
                Vazgeç
              </Button>
            </div>
          </div>
        ) : (
          <div className="flex items-start gap-2">
            <p className="min-w-0 flex-1 text-sm leading-relaxed whitespace-pre-wrap">
              {message.content}
              {message.isEdited ? (
                <span className="ml-1.5 text-[11px] text-subtle-foreground">(düzenlendi)</span>
              ) : null}
            </p>

            {/* İşlemler yalnızca hover/odakta görünür */}
            <span className="flex shrink-0 items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
              <Button
                variant="ghost"
                size="icon-sm"
                onClick={() => onReply(message)}
                aria-label="Yanıtla"
              >
                <Reply aria-hidden="true" />
              </Button>

              {canEdit ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  onClick={() => setEditing(true)}
                  aria-label="Mesajı düzenle"
                >
                  <Pencil aria-hidden="true" />
                </Button>
              ) : null}

              {canDelete ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  onClick={() => remove.mutate()}
                  disabled={remove.isPending}
                  aria-label="Mesajı sil"
                >
                  <Trash2 className="text-danger" aria-hidden="true" />
                </Button>
              ) : null}
            </span>
          </div>
        )}

        {/* Dosya ekleri */}
        {message.attachments.length > 0 ? (
          <div className="mt-2 space-y-2">
            {message.attachments.map((attachment) =>
              attachment.category === AttachmentCategory.Image ? (
                <a
                  key={attachment.id}
                  href={toAbsoluteUrl(attachment.url)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="block w-fit overflow-hidden rounded-lg border border-border outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <img
                    src={toAbsoluteUrl(attachment.url)}
                    alt={attachment.fileName}
                    loading="lazy"
                    className="max-h-64 max-w-xs object-cover"
                  />
                </a>
              ) : (
                <a
                  key={attachment.id}
                  href={toAbsoluteUrl(attachment.url)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex w-fit items-center gap-2.5 rounded-lg border border-border px-3 py-2 transition-colors hover:border-border-strong"
                >
                  <Download className="size-4 text-subtle-foreground" aria-hidden="true" />
                  <span className="text-sm">{attachment.fileName}</span>
                  <span className="text-[11px] text-subtle-foreground">
                    {formatFileSize(attachment.sizeBytes)}
                  </span>
                </a>
              ),
            )}
          </div>
        ) : null}

        {/* Okundu göstergesi yalnızca kendi mesajlarımızda anlamlı */}
        {isOwn ? (
          <span
            className="mt-0.5 flex items-center gap-1 text-[11px] text-subtle-foreground"
            title={`${message.readByCount} kişi okudu`}
          >
            {message.readByCount > 1 ? (
              <>
                <CheckCheck className="size-3 text-info" aria-hidden="true" />
                {message.readByCount - 1} kişi okudu
              </>
            ) : (
              <>
                <Check className="size-3" aria-hidden="true" />
                Gönderildi
              </>
            )}
          </span>
        ) : null}
      </div>
    </div>
  );
}
