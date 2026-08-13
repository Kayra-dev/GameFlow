import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Progress } from '@/components/ui/progress';
import { getErrorMessage } from '@/lib/api-client';
import { queryKeys } from '@/lib/query-client';
import { cn } from '@/lib/utils';
import type { ChecklistItemDto } from '@/types/api';

import { workItemsApi } from '../api/work-items-api';

type TaskChecklistProps = {
  workItemId: string;
  items: ChecklistItemDto[];
  canEdit: boolean;
};

export function TaskChecklist({ workItemId, items, canEdit }: TaskChecklistProps) {
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState('');

  const completedCount = items.filter((item) => item.isCompleted).length;
  const percent = items.length === 0 ? 0 : Math.round((completedCount / items.length) * 100);

  /** Değişiklikten sonra hem görev detayı hem pano sayaçları tazelenir. */
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });
  };

  const add = useMutation({
    mutationFn: (text: string) => workItemsApi.addChecklistItem(workItemId, text),
    onSuccess: () => {
      invalidate();
      setDraft('');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const toggle = useMutation({
    mutationFn: ({ item, isCompleted }: { item: ChecklistItemDto; isCompleted: boolean }) =>
      workItemsApi.updateChecklistItem(workItemId, item.id, item.text, isCompleted),
    onSuccess: invalidate,
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (itemId: string) => workItemsApi.deleteChecklistItem(workItemId, itemId),
    onSuccess: invalidate,
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <section className="space-y-3">
      <div className="flex items-center gap-3">
        <h2 className="text-sm font-semibold">Kontrol listesi</h2>
        {items.length > 0 ? (
          <>
            <span className="text-xs tabular-nums text-muted-foreground">
              {completedCount}/{items.length}
            </span>
            <Progress value={percent} className="max-w-32 flex-1" />
          </>
        ) : null}
      </div>

      {items.length > 0 ? (
        <ul className="space-y-1">
          {items.map((item) => (
            <li key={item.id} className="group flex items-start gap-2.5 rounded-lg px-1 py-1.5">
              <Checkbox
                checked={item.isCompleted}
                disabled={!canEdit || toggle.isPending}
                onCheckedChange={(checked) =>
                  toggle.mutate({ item, isCompleted: checked === true })
                }
                aria-label={`"${item.text}" maddesini işaretle`}
                className="mt-0.5"
              />

              <span
                className={cn(
                  'min-w-0 flex-1 text-sm',
                  item.isCompleted && 'text-muted-foreground line-through',
                )}
              >
                {item.text}
              </span>

              {canEdit ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={`"${item.text}" maddesini sil`}
                  onClick={() => remove.mutate(item.id)}
                  disabled={remove.isPending}
                  // Fare kullanıcısında sadece hover'da görünür, klavyede odakla belirir.
                  className="opacity-0 transition-opacity group-hover:opacity-100 focus-visible:opacity-100"
                >
                  <Trash2 className="text-danger" aria-hidden="true" />
                </Button>
              ) : null}
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-muted-foreground">Kontrol listesi maddesi yok.</p>
      )}

      {canEdit ? (
        <div className="flex gap-2">
          <Input
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' && draft.trim()) {
                event.preventDefault();
                add.mutate(draft.trim());
              }
            }}
            placeholder="Madde ekle ve Enter'a bas"
            aria-label="Yeni kontrol listesi maddesi"
          />
          <Button
            variant="secondary"
            size="icon"
            onClick={() => draft.trim() && add.mutate(draft.trim())}
            disabled={!draft.trim() || add.isPending}
            aria-label="Maddeyi ekle"
          >
            <Plus aria-hidden="true" />
          </Button>
        </div>
      ) : null}
    </section>
  );
}
