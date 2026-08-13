import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Download,
  File,
  FileArchive,
  FileSpreadsheet,
  FileText,
  Film,
  Loader2,
  Music,
  Paperclip,
  Trash2,
  Upload,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useRef, useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { API_BASE_URL, getErrorMessage } from '@/lib/api-client';
import { formatRelative } from '@/lib/dates';
import { queryKeys } from '@/lib/query-client';
import { cn, formatFileSize } from '@/lib/utils';
import type { AttachmentDto } from '@/types/api';
import { AttachmentCategory } from '@/types/enums';

import { workItemsApi } from '../api/work-items-api';

const categoryIcons: Record<AttachmentCategory, LucideIcon> = {
  [AttachmentCategory.Image]: File,
  [AttachmentCategory.Pdf]: FileText,
  [AttachmentCategory.Archive]: FileArchive,
  [AttachmentCategory.Document]: FileText,
  [AttachmentCategory.Spreadsheet]: FileSpreadsheet,
  [AttachmentCategory.Video]: Film,
  [AttachmentCategory.Audio]: Music,
  [AttachmentCategory.Other]: File,
};

type TaskAttachmentsProps = {
  workItemId: string;
  attachments: AttachmentDto[];
  canEdit: boolean;
};

/** Sunucu göreli yol döndürür; tam adres API kökü ile birleştirilir. */
function toAbsoluteUrl(url: string): string {
  return url.startsWith('http') ? url : `${API_BASE_URL}${url}`;
}

export function TaskAttachments({ workItemId, attachments, canEdit }: TaskAttachmentsProps) {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setDragOver] = useState(false);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.workItems.all });
  };

  const upload = useMutation({
    mutationFn: (file: File) => workItemsApi.uploadAttachment(workItemId, file),
    onSuccess: () => {
      invalidate();
      toast.success('Dosya yüklendi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (attachmentId: string) => workItemsApi.deleteAttachment(workItemId, attachmentId),
    onSuccess: () => {
      invalidate();
      toast.success('Dosya silindi.');
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  const handleFiles = (files: FileList | null) => {
    if (!files) return;

    // Birden fazla dosya sırayla yüklenir; sunucu her isteği ayrı doğrular.
    for (const file of Array.from(files)) {
      upload.mutate(file);
    }
  };

  const images = attachments.filter(
    (attachment) => attachment.category === AttachmentCategory.Image,
  );
  const others = attachments.filter(
    (attachment) => attachment.category !== AttachmentCategory.Image,
  );

  return (
    <section className="space-y-3">
      <div className="flex items-center gap-3">
        <h2 className="text-sm font-semibold">Dosyalar</h2>
        {attachments.length > 0 ? (
          <span className="text-xs text-muted-foreground">{attachments.length}</span>
        ) : null}
      </div>

      {/* Resimler ızgara olarak önizlenir */}
      {images.length > 0 ? (
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {images.map((attachment) => (
            <div key={attachment.id} className="group relative">
              <a
                href={toAbsoluteUrl(attachment.url)}
                target="_blank"
                rel="noopener noreferrer"
                className="block overflow-hidden rounded-lg border border-border outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <img
                  src={toAbsoluteUrl(attachment.url)}
                  alt={attachment.fileName}
                  loading="lazy"
                  className="aspect-video w-full object-cover transition-transform group-hover:scale-105"
                />
              </a>

              {canEdit ? (
                <Button
                  variant="secondary"
                  size="icon-sm"
                  aria-label={`${attachment.fileName} dosyasını sil`}
                  onClick={() => remove.mutate(attachment.id)}
                  className="absolute top-1.5 right-1.5 opacity-0 transition-opacity group-hover:opacity-100 focus-visible:opacity-100"
                >
                  <Trash2 className="text-danger" aria-hidden="true" />
                </Button>
              ) : null}

              <p className="mt-1 truncate text-[11px] text-subtle-foreground">
                {attachment.fileName}
              </p>
            </div>
          ))}
        </div>
      ) : null}

      {/* Diğer dosyalar liste olarak */}
      {others.length > 0 ? (
        <ul className="space-y-1.5">
          {others.map((attachment) => {
            const Icon = categoryIcons[attachment.category];

            return (
              <li
                key={attachment.id}
                className="group flex items-center gap-2.5 rounded-lg border border-border px-3 py-2"
              >
                <Icon className="size-4 shrink-0 text-subtle-foreground" aria-hidden="true" />

                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm">{attachment.fileName}</p>
                  <p className="text-[11px] text-subtle-foreground">
                    {formatFileSize(attachment.sizeBytes)}
                    {attachment.uploadedBy ? ` · ${attachment.uploadedBy.fullName}` : ''}
                    {` · ${formatRelative(attachment.createdAt)}`}
                  </p>
                </div>

                <Button asChild variant="ghost" size="icon-sm">
                  <a
                    href={toAbsoluteUrl(attachment.url)}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={`${attachment.fileName} dosyasını indir`}
                  >
                    <Download aria-hidden="true" />
                  </a>
                </Button>

                {canEdit ? (
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`${attachment.fileName} dosyasını sil`}
                    onClick={() => remove.mutate(attachment.id)}
                    disabled={remove.isPending}
                  >
                    <Trash2 className="text-danger" aria-hidden="true" />
                  </Button>
                ) : null}
              </li>
            );
          })}
        </ul>
      ) : null}

      {attachments.length === 0 ? (
        <p className="text-sm text-muted-foreground">Dosya yok.</p>
      ) : null}

      {canEdit ? (
        <div
          onDragOver={(event) => {
            event.preventDefault();
            setDragOver(true);
          }}
          onDragLeave={() => setDragOver(false)}
          onDrop={(event) => {
            event.preventDefault();
            setDragOver(false);
            handleFiles(event.dataTransfer.files);
          }}
          className={cn(
            'rounded-lg border border-dashed border-border p-4 text-center transition-colors',
            isDragOver && 'border-primary bg-primary/5',
          )}
        >
          <input
            ref={fileInputRef}
            type="file"
            multiple
            className="hidden"
            onChange={(event) => {
              handleFiles(event.target.files);
              // Aynı dosyanın tekrar seçilebilmesi için girdi sıfırlanır.
              event.target.value = '';
            }}
          />

          <Paperclip
            className="mx-auto size-5 text-subtle-foreground"
            aria-hidden="true"
          />
          <p className="mt-2 text-xs text-muted-foreground">
            Dosyaları buraya sürükleyin veya
          </p>
          <Button
            variant="secondary"
            size="sm"
            className="mt-2"
            onClick={() => fileInputRef.current?.click()}
            disabled={upload.isPending}
          >
            {upload.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <Upload aria-hidden="true" />
            )}
            Dosya seç
          </Button>
          <p className="mt-2 text-[11px] text-subtle-foreground">
            Resim, PDF, ZIP, Word, Excel, video · en fazla 50 MB
          </p>
        </div>
      ) : null}
    </section>
  );
}
