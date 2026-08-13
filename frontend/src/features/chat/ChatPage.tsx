import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Crown,
  Hash,
  Loader2,
  MessageSquare,
  Paperclip,
  Send,
  Users as UsersIcon,
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { Textarea } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { useChatConnection } from '@/hooks/use-realtime';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import type { MessageDto } from '@/types/api';
import { ChatRoomType } from '@/types/enums';

import { chatApi } from './api/chat-api';
import { MessageBubble } from './components/MessageBubble';

export function ChatPage() {
  const { roomId } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const currentUserId = useAuthStore((state) => state.user?.id);

  const [draft, setDraft] = useState('');
  const [replyTo, setReplyTo] = useState<MessageDto | null>(null);
  const [typingUsers, setTypingUsers] = useState<Set<string>>(new Set());

  const scrollRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { data: rooms, isLoading: roomsLoading } = useQuery({
    queryKey: queryKeys.chat.rooms,
    queryFn: chatApi.rooms,
  });

  // Adres odasız açıldıysa ilk odaya yönlendirilir.
  useEffect(() => {
    if (!roomId && rooms && rooms.length > 0) {
      navigate(`/sohbet/${rooms[0]!.id}`, { replace: true });
    }
  }, [roomId, rooms, navigate]);

  const activeRoom = rooms?.find((room) => room.id === roomId);

  const { data: history, isLoading: messagesLoading } = useQuery({
    queryKey: queryKeys.chat.messages(roomId ?? ''),
    queryFn: () => chatApi.messages(roomId!, { pageSize: 50 }),
    enabled: Boolean(roomId),
  });

  const messages = history?.items ?? [];

  /** Yeni mesaj geldiğinde ya da oda değiştiğinde en alta kaydırılır. */
  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight });
  }, [messages.length, roomId]);

  // Oda açıkken mesajlar okundu işaretlenir.
  const markAsRead = useMutation({
    mutationFn: () => chatApi.markAsRead(roomId!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.rooms });
    },
  });

  useEffect(() => {
    if (roomId && messages.length > 0 && activeRoom && activeRoom.unreadCount > 0) {
      markAsRead.mutate();
    }
    // markAsRead kasıtlı olarak bağımlılıkta değil: mutation nesnesi her
    // render'da yenilenir ve sonsuz döngü oluşturur.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [roomId, messages.length, activeRoom?.unreadCount]);

  const { sendTyping } = useChatConnection(roomId, {
    onMessageReceived: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId ?? '') });
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.rooms });
    },
    onMessageEdited: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId ?? '') });
    },
    onMessageDeleted: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId ?? '') });
    },
    onUserTyping: (_room, userId, isTyping) => {
      setTypingUsers((previous) => {
        const next = new Set(previous);

        if (isTyping) {
          next.add(userId);
        } else {
          next.delete(userId);
        }

        return next;
      });
    },
  });

  const send = useMutation({
    mutationFn: (content: string) => chatApi.send(roomId!, content, replyTo?.id),
    onSuccess: () => {
      setDraft('');
      setReplyTo(null);
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId ?? '') });
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.rooms });
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const sendFile = useMutation({
    mutationFn: (file: File) => chatApi.sendAttachment(roomId!, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId ?? '') });
      toast.success('Dosya paylaşıldı.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  /** Yazma göstergesi: her tuşta değil, aralıklı olarak bildirilir. */
  const handleDraftChange = (value: string) => {
    setDraft(value);
    sendTyping(true);

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    typingTimeoutRef.current = setTimeout(() => sendTyping(false), 1500);
  };

  const submit = () => {
    const content = draft.trim();

    if (content) {
      send.mutate(content);
      sendTyping(false);
    }
  };

  const typingNames = messages
    .map((message) => message.sender)
    .filter((sender) => typingUsers.has(sender.id) && sender.id !== currentUserId)
    .map((sender) => sender.fullName);

  const uniqueTypingNames = [...new Set(typingNames)];

  return (
    <div className="mx-auto flex h-[calc(100dvh-7rem)] w-full max-w-7xl gap-4">
      {/* Oda listesi */}
      <aside className="hidden w-64 shrink-0 flex-col rounded-card border border-border bg-surface md:flex">
        <div className="border-b border-border px-4 py-3">
          <h1 className="text-sm font-semibold">Sohbet</h1>
        </div>

        <nav className="flex-1 overflow-y-auto p-2" aria-label="Sohbet odaları">
          {roomsLoading ? (
            <div className="space-y-2 p-1">
              {Array.from({ length: 4 }, (_, index) => (
                <Skeleton key={index} className="h-12" />
              ))}
            </div>
          ) : rooms?.length ? (
            <ul className="space-y-0.5">
              {rooms.map((room) => (
                <li key={room.id}>
                  <button
                    type="button"
                    onClick={() => navigate(`/sohbet/${room.id}`)}
                    className={cn(
                      'flex w-full items-start gap-2.5 rounded-lg px-2.5 py-2 text-left',
                      'transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring',
                      room.id === roomId
                        ? 'bg-primary/12'
                        : 'hover:bg-surface-raised',
                    )}
                  >
                    {room.type === ChatRoomType.Leaders ? (
                      <Crown className="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true" />
                    ) : room.type === ChatRoomType.Team ? (
                      <UsersIcon
                        className="mt-0.5 size-4 shrink-0"
                        style={{ color: room.colorHex ?? undefined }}
                        aria-hidden="true"
                      />
                    ) : (
                      <Hash
                        className="mt-0.5 size-4 shrink-0"
                        style={{ color: room.colorHex ?? undefined }}
                        aria-hidden="true"
                      />
                    )}

                    <span className="min-w-0 flex-1">
                      <span className="flex items-center gap-2">
                        <span
                          className={cn(
                            'truncate text-sm',
                            room.id === roomId ? 'font-medium text-primary' : 'font-medium',
                          )}
                        >
                          {room.name}
                        </span>
                        {room.unreadCount > 0 ? (
                          <span className="ml-auto grid min-w-4 shrink-0 place-items-center rounded-full bg-danger px-1 text-[10px] font-semibold text-white">
                            {room.unreadCount}
                          </span>
                        ) : null}
                      </span>

                      {room.lastMessage ? (
                        <span className="mt-0.5 block truncate text-xs text-subtle-foreground">
                          {room.lastMessage.sender.fullName.split(' ')[0]}:{' '}
                          {room.lastMessage.content}
                        </span>
                      ) : (
                        <span className="mt-0.5 block text-xs text-subtle-foreground">
                          Henüz mesaj yok
                        </span>
                      )}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="p-4 text-center text-sm text-muted-foreground">
              Erişebileceğiniz sohbet odası yok.
            </p>
          )}
        </nav>
      </aside>

      {/* Mesaj alanı */}
      <section className="flex min-w-0 flex-1 flex-col rounded-card border border-border bg-surface">
        {!activeRoom ? (
          <EmptyState
            icon={MessageSquare}
            title="Sohbet odası seçin"
            description="Soldaki listeden bir oda seçerek konuşmaya başlayın."
            className="flex-1"
          />
        ) : (
          <>
            <header className="flex items-center gap-3 border-b border-border px-4 py-3">
              {activeRoom.type === ChatRoomType.Leaders ? (
                <Crown className="size-4 text-warning" aria-hidden="true" />
              ) : (
                <UsersIcon
                  className="size-4"
                  style={{ color: activeRoom.colorHex ?? undefined }}
                  aria-hidden="true"
                />
              )}
              <div className="min-w-0">
                <h2 className="truncate text-sm font-semibold">{activeRoom.name}</h2>
                {activeRoom.description ? (
                  <p className="truncate text-xs text-muted-foreground">
                    {activeRoom.description}
                  </p>
                ) : null}
              </div>
            </header>

            <div ref={scrollRef} className="flex-1 space-y-1 overflow-y-auto p-4">
              {messagesLoading ? (
                <div className="space-y-3">
                  {Array.from({ length: 5 }, (_, index) => (
                    <Skeleton key={index} className="h-14" />
                  ))}
                </div>
              ) : messages.length === 0 ? (
                <EmptyState
                  icon={MessageSquare}
                  title="Henüz mesaj yok"
                  description="İlk mesajı siz yazın."
                />
              ) : (
                messages.map((message, index) => (
                  <MessageBubble
                    key={message.id}
                    message={message}
                    roomId={activeRoom.id}
                    isOwn={message.sender.id === currentUserId}
                    // Aynı kişinin art arda mesajlarında avatar ve ad tekrarlanmaz.
                    isGrouped={
                      index > 0 && messages[index - 1]!.sender.id === message.sender.id
                    }
                    onReply={setReplyTo}
                  />
                ))
              )}
            </div>

            {uniqueTypingNames.length > 0 ? (
              <p className="px-4 pb-1 text-xs text-subtle-foreground">
                {uniqueTypingNames.join(', ')} yazıyor…
              </p>
            ) : null}

            {replyTo ? (
              <div className="mx-4 mb-2 flex items-start gap-2 rounded-lg border-l-2 border-primary bg-surface-raised px-3 py-2">
                <div className="min-w-0 flex-1">
                  <p className="text-xs font-medium text-primary">
                    {replyTo.sender.fullName} kişisine yanıt
                  </p>
                  <p className="truncate text-xs text-muted-foreground">{replyTo.content}</p>
                </div>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  onClick={() => setReplyTo(null)}
                  aria-label="Yanıtı iptal et"
                >
                  ×
                </Button>
              </div>
            ) : null}

            <div className="flex items-end gap-2 border-t border-border p-3">
              <input
                ref={fileInputRef}
                type="file"
                className="hidden"
                onChange={(event) => {
                  const file = event.target.files?.[0];
                  if (file) sendFile.mutate(file);
                  event.target.value = '';
                }}
              />

              <Button
                variant="ghost"
                size="icon"
                onClick={() => fileInputRef.current?.click()}
                disabled={sendFile.isPending}
                aria-label="Dosya paylaş"
              >
                {sendFile.isPending ? (
                  <Loader2 className="animate-spin" aria-hidden="true" />
                ) : (
                  <Paperclip aria-hidden="true" />
                )}
              </Button>

              <Textarea
                value={draft}
                onChange={(event) => handleDraftChange(event.target.value)}
                onKeyDown={(event) => {
                  // Enter gönderir, Shift+Enter satır atlar.
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault();
                    submit();
                  }
                }}
                rows={1}
                placeholder="Mesaj yaz… (Shift+Enter ile satır atla)"
                aria-label="Mesaj"
                className="max-h-32 min-h-10 flex-1 resize-none py-2"
              />

              <Button
                size="icon"
                onClick={submit}
                disabled={!draft.trim() || send.isPending}
                aria-label="Mesajı gönder"
              >
                {send.isPending ? (
                  <Loader2 className="animate-spin" aria-hidden="true" />
                ) : (
                  <Send aria-hidden="true" />
                )}
              </Button>
            </div>
          </>
        )}
      </section>
    </div>
  );
}
