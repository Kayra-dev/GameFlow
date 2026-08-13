import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2, Pencil, Send, Trash2, X } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { Avatar } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/input';
import { getErrorMessage } from '@/lib/api-client';
import { formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import type { CommentDto } from '@/types/api';

import { workItemsApi } from '../api/work-items-api';

type TaskCommentsProps = {
  workItemId: string;
  comments: CommentDto[];
  /** Yöneticiler ve takım liderleri başkasının yorumunu silebilir (moderasyon). */
  canModerate: boolean;
};

export function TaskComments({ workItemId, comments, canModerate }: TaskCommentsProps) {
  const queryClient = useQueryClient();
  const currentUserId = useAuthStore((state) => state.user?.id);

  const [draft, setDraft] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState('');

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });
  };

  const add = useMutation({
    mutationFn: (content: string) => workItemsApi.addComment(workItemId, content),
    onSuccess: () => {
      invalidate();
      setDraft('');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const update = useMutation({
    mutationFn: ({ commentId, content }: { commentId: string; content: string }) =>
      workItemsApi.updateComment(workItemId, commentId, content),
    onSuccess: () => {
      invalidate();
      setEditingId(null);
      setEditDraft('');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (commentId: string) => workItemsApi.deleteComment(workItemId, commentId),
    onSuccess: () => {
      invalidate();
      toast.success('Yorum silindi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const startEdit = (comment: CommentDto) => {
    setEditingId(comment.id);
    setEditDraft(comment.content);
  };

  return (
    <section className="space-y-4">
      <h2 className="text-sm font-semibold">
        Yorumlar
        {comments.length > 0 ? (
          <span className="ml-2 text-xs font-normal text-muted-foreground">
            {comments.length}
          </span>
        ) : null}
      </h2>

      {/* Yeni yorum */}
      <div className="space-y-2">
        <Textarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            // Ctrl/Cmd+Enter ile gönder; düz Enter satır atlar.
            if (event.key === 'Enter' && (event.metaKey || event.ctrlKey) && draft.trim()) {
              event.preventDefault();
              add.mutate(draft.trim());
            }
          }}
          rows={3}
          placeholder="Yorum yaz… (göndermek için ⌘/Ctrl + Enter)"
          aria-label="Yeni yorum"
        />
        <div className="flex justify-end">
          <Button
            size="sm"
            onClick={() => draft.trim() && add.mutate(draft.trim())}
            disabled={!draft.trim() || add.isPending}
          >
            {add.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <Send aria-hidden="true" />
            )}
            Gönder
          </Button>
        </div>
      </div>

      {comments.length === 0 ? (
        <p className="text-sm text-muted-foreground">Henüz yorum yok.</p>
      ) : (
        <ul className="space-y-4">
          {comments.map((comment) => {
            const isOwn = comment.author.id === currentUserId;
            const isEditing = editingId === comment.id;

            return (
              <li key={comment.id} className="flex gap-3">
                <Avatar
                  fullName={comment.author.fullName}
                  avatarUrl={comment.author.avatarUrl}
                  size="sm"
                />

                <div className="min-w-0 flex-1 space-y-1.5">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm font-medium">{comment.author.fullName}</span>
                    <span className="text-xs text-subtle-foreground">
                      {formatRelative(comment.createdAt)}
                    </span>
                    {comment.isEdited ? (
                      <span className="text-xs text-subtle-foreground">· düzenlendi</span>
                    ) : null}

                    {isOwn || canModerate ? (
                      <span className="ml-auto flex items-center gap-0.5">
                        {isOwn && !isEditing ? (
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label="Yorumu düzenle"
                            onClick={() => startEdit(comment)}
                          >
                            <Pencil aria-hidden="true" />
                          </Button>
                        ) : null}

                        <Button
                          variant="ghost"
                          size="icon-sm"
                          aria-label="Yorumu sil"
                          onClick={() => remove.mutate(comment.id)}
                          disabled={remove.isPending}
                        >
                          <Trash2 className="text-danger" aria-hidden="true" />
                        </Button>
                      </span>
                    ) : null}
                  </div>

                  {isEditing ? (
                    <div className="space-y-2">
                      <Textarea
                        value={editDraft}
                        onChange={(event) => setEditDraft(event.target.value)}
                        rows={3}
                        autoFocus
                        aria-label="Yorumu düzenle"
                      />
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          onClick={() =>
                            editDraft.trim() &&
                            update.mutate({ commentId: comment.id, content: editDraft.trim() })
                          }
                          disabled={!editDraft.trim() || update.isPending}
                        >
                          Kaydet
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => {
                            setEditingId(null);
                            setEditDraft('');
                          }}
                        >
                          <X aria-hidden="true" />
                          Vazgeç
                        </Button>
                      </div>
                    </div>
                  ) : (
                    // Satır sonları korunur; kullanıcı yorumu HTML olarak
                    // yorumlanmaz, düz metin olarak gösterilir.
                    <p className="text-sm leading-relaxed whitespace-pre-wrap">
                      {comment.content}
                    </p>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
