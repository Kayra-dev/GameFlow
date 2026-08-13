import { FileQuestion } from 'lucide-react';
import { Link } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';

export default function NotFoundPage() {
  return (
    <EmptyState
      icon={FileQuestion}
      title="Sayfa bulunamadı"
      description="Aradığınız sayfa taşınmış veya hiç var olmamış olabilir."
      action={
        <Button asChild variant="secondary">
          <Link to="/">Panoya dön</Link>
        </Button>
      }
      className="min-h-[60vh]"
    />
  );
}
