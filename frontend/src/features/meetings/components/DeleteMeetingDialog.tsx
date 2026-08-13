import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { getErrorMessage } from '@/lib/api-client';
import type { MeetingDto } from '@/types/api';

import { meetingsApi } from '../api/meetings-api';

type DeleteMeetingDialogProps = {
  meeting: MeetingDto | null;
  onClose: () => void;
  /** Silme başarılıysa çağrılır; ayrıntı sayfası listeye döner. */
  onDeleted?: () => void;
};

export function DeleteMeetingDialog({ meeting, onClose, onDeleted }: DeleteMeetingDialogProps) {
  const queryClient = useQueryClient();

  const remove = useMutation({
    mutationFn: () => meetingsApi.remove(meeting!.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['meetings'] });
      void queryClient.invalidateQueries({ queryKey: ['calendar'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });

      toast.success(`“${meeting?.title}” toplantısı iptal edildi.`);
      onClose();
      onDeleted?.();
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog open={Boolean(meeting)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Toplantıyı iptal et</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{meeting?.title}</strong> silinecek ve
            katılımcıların takviminden kalkacak. Bu işlem geri alınamaz.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Vazgeç
          </Button>
          <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
            <Trash2 aria-hidden="true" />
            İptal et
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
