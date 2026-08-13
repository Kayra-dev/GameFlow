import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Megaphone, Pin, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { EmptyState } from '@/components/ui/empty-state';
import { Input, Textarea } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { getErrorMessage } from '@/lib/api-client';
import { formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { isAdmin, useAuthStore } from '@/stores/auth-store';
import { AnnouncementPriority, announcementPriorityLabels } from '@/types/enums';

import { announcementsApi } from './api/announcements-api';

const schema = z.object({
  title: z.string().min(1, 'Başlık zorunludur.').max(192, 'Başlık en fazla 192 karakter olabilir.'),
  content: z
    .string()
    .min(1, 'İçerik zorunludur.')
    .max(8000, 'İçerik en fazla 8000 karakter olabilir.'),
  priority: z.number().int(),
  isPinned: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

const priorityVariant: Record<AnnouncementPriority, 'neutral' | 'warning' | 'danger'> = {
  [AnnouncementPriority.Info]: 'neutral',
  [AnnouncementPriority.Warning]: 'warning',
  [AnnouncementPriority.Critical]: 'danger',
};

export function AnnouncementsPage() {
  const [formOpen, setFormOpen] = useState(false);
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const canManage = isAdmin(user);

  const params = {};

  const { data: announcements, isLoading } = useQuery({
    queryKey: queryKeys.announcements(params),
    queryFn: () => announcementsApi.list(params),
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      content: '',
      priority: AnnouncementPriority.Info,
      isPinned: false,
    },
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['announcements'] });
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const create = useMutation({
    mutationFn: (values: FormValues) =>
      announcementsApi.create({
        title: values.title,
        content: values.content,
        priority: values.priority as AnnouncementPriority,
        isPinned: values.isPinned,
        projectId: null,
        expiresAt: null,
      }),
    onSuccess: () => {
      invalidate();
      toast.success('Duyuru yayınlandı.');
      reset();
      setFormOpen(false);
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (id: string) => announcementsApi.remove(id),
    onSuccess: () => {
      invalidate();
      toast.success('Duyuru silindi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <div className="mx-auto w-full max-w-3xl space-y-5">
      <header className="flex flex-wrap items-center gap-3">
        <div className="flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">Duyurular</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Stüdyo genelindeki bildirimler ve önemli hatırlatmalar.
          </p>
        </div>

        {canManage ? (
          <Button onClick={() => setFormOpen(true)}>
            <Plus aria-hidden="true" />
            Yeni duyuru
          </Button>
        ) : null}
      </header>

      {isLoading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-28 rounded-card" />
          ))}
        </div>
      ) : announcements?.length === 0 ? (
        <Card>
          <EmptyState
            icon={Megaphone}
            title="Duyuru yok"
            description={
              canManage
                ? 'İlk duyuruyu yayınlayarak ekibi bilgilendirin.'
                : 'Yönetici duyuru yayınladığında burada görürsünüz.'
            }
          />
        </Card>
      ) : (
        <ul className="space-y-3">
          {announcements?.map((announcement) => (
            <li key={announcement.id}>
              <Card>
                <CardContent className="space-y-2 pt-5">
                  <div className="flex flex-wrap items-center gap-2">
                    {announcement.isPinned ? (
                      <Pin className="size-3.5 text-warning" aria-label="Sabitlenmiş" />
                    ) : null}
                    <h2 className="text-base font-semibold">{announcement.title}</h2>
                    <Badge variant={priorityVariant[announcement.priority]}>
                      {announcementPriorityLabels[announcement.priority]}
                    </Badge>

                    {canManage ? (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="ml-auto"
                        onClick={() => remove.mutate(announcement.id)}
                        aria-label="Duyuruyu sil"
                      >
                        <Trash2 className="text-danger" aria-hidden="true" />
                      </Button>
                    ) : null}
                  </div>

                  <p className="text-sm leading-relaxed whitespace-pre-wrap text-foreground">
                    {announcement.content}
                  </p>

                  <div className="flex items-center gap-2 pt-1">
                    <Avatar
                      fullName={announcement.author.fullName}
                      avatarUrl={announcement.author.avatarUrl}
                      size="xs"
                    />
                    <span className="text-xs text-muted-foreground">
                      {announcement.author.fullName} · {formatRelative(announcement.publishedAt)}
                      {announcement.projectName ? ` · ${announcement.projectName}` : ''}
                    </span>
                  </div>
                </CardContent>
              </Card>
            </li>
          ))}
        </ul>
      )}

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Yeni duyuru</DialogTitle>
            <DialogDescription>
              Duyuru tüm aktif kullanıcılara bildirim olarak da gönderilir.
            </DialogDescription>
          </DialogHeader>

          <form
            onSubmit={handleSubmit((values) => create.mutate(values))}
            noValidate
            className="space-y-4"
          >
            <div className="space-y-1.5">
              <Label htmlFor="ann-title">
                Başlık<span className="ml-0.5 text-danger">*</span>
              </Label>
              <Input
                id="ann-title"
                autoFocus
                placeholder="Cuma stüdyo toplantısı"
                aria-invalid={Boolean(errors.title)}
                {...register('title')}
              />
              {errors.title ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.title.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="ann-content">
                İçerik<span className="ml-0.5 text-danger">*</span>
              </Label>
              <Textarea
                id="ann-content"
                rows={4}
                placeholder="Tüm ekipler saat 15:00'te toplantı odasında."
                aria-invalid={Boolean(errors.content)}
                {...register('content')}
              />
              {errors.content ? (
                <p role="alert" className="text-xs text-danger">
                  {errors.content.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="ann-priority">Önem</Label>
              <Select
                value={String(watch('priority'))}
                onValueChange={(value) => setValue('priority', Number(value))}
              >
                <SelectTrigger id="ann-priority">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(announcementPriorityLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <label className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border border-border p-3">
              <span className="text-sm">
                Üste sabitle
                <span className="block text-xs text-muted-foreground">
                  Sabitlenen duyurular listenin en başında görünür.
                </span>
              </span>
              <Switch
                checked={watch('isPinned')}
                onCheckedChange={(checked) => setValue('isPinned', checked)}
              />
            </label>

            <DialogFooter>
              <Button type="button" variant="secondary" onClick={() => setFormOpen(false)}>
                Vazgeç
              </Button>
              <Button type="submit" disabled={create.isPending}>
                {create.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
                Yayınla
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
