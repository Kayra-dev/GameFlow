import { Construction } from 'lucide-react';

import { EmptyState } from '@/components/ui/empty-state';

/**
 * Henüz geliştirilmemiş ekranlar için geçici içerik.
 * Rotanın çalıştığını doğrular ve kullanıcıyı boş beyaz sayfayla baş başa bırakmaz.
 */
export function PagePlaceholder({ title, description }: { title: string; description?: string }) {
  return (
    <div className="mx-auto w-full max-w-7xl">
      {title ? <h1 className="text-2xl font-semibold tracking-tight">{title}</h1> : null}

      <div className={`rounded-card border border-dashed border-border ${title ? 'mt-6' : ''}`}>
        <EmptyState
          icon={Construction}
          title="Bu ekran henüz hazır değil"
          description={
            description ??
            'Bu modülün arayüzü sonraki aşamada geliştirilecek. API tarafı hazır ve çalışıyor.'
          }
        />
      </div>
    </div>
  );
}
