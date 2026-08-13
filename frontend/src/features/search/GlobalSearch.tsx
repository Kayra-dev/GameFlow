import * as DialogPrimitive from '@radix-ui/react-dialog';
import { useQuery } from '@tanstack/react-query';
import { Command } from 'cmdk';
import {
  FileText,
  FolderKanban,
  Paperclip,
  Search,
  SquareKanban,
  Users as UsersIcon,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Avatar } from '@/components/ui/avatar';
import { Spinner } from '@/components/ui/spinner';
import { useDebouncedValue } from '@/hooks/use-debounced-value';
import { queryKeys } from '@/lib/query-client';
import { systemRoleLabels, workItemStatusLabels } from '@/types/enums';

import { searchApi } from './api/search-api';

type GlobalSearchProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

/**
 * Komut paleti tarzı global arama. ⌘K / Ctrl+K ile açılır.
 * Sunucu en az 2 karakter ister; kısa sorgularda istek atılmaz.
 */
export function GlobalSearch({ open, onOpenChange }: GlobalSearchProps) {
  const [query, setQuery] = useState('');
  const debouncedQuery = useDebouncedValue(query, 250);
  const navigate = useNavigate();

  // Klavye kısayolu tüm uygulamada geçerli.
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'k' && (event.metaKey || event.ctrlKey)) {
        event.preventDefault();
        onOpenChange(true);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onOpenChange]);

  // Panel kapanınca sorgu sıfırlanır ki tekrar açıldığında temiz başlasın.
  useEffect(() => {
    if (!open) {
      setQuery('');
    }
  }, [open]);

  const { data, isFetching } = useQuery({
    queryKey: queryKeys.search(debouncedQuery),
    queryFn: () => searchApi.search(debouncedQuery),
    enabled: open && debouncedQuery.trim().length >= 2,
    staleTime: 15_000,
  });

  const go = (path: string) => {
    onOpenChange(false);
    navigate(path);
  };

  const hasResults = (data?.totalCount ?? 0) > 0;

  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm data-[state=open]:animate-in" />

        <DialogPrimitive.Content
          aria-label="Global arama"
          className="fixed top-[15vh] left-1/2 z-50 w-[min(94vw,40rem)] -translate-x-1/2 overflow-hidden rounded-card border border-border bg-surface shadow-float data-[state=open]:animate-scale-in"
        >
          <Command shouldFilter={false} loop>
            <div className="flex items-center gap-3 border-b border-border px-4">
              <Search className="size-4 shrink-0 text-subtle-foreground" aria-hidden="true" />
              <Command.Input
                value={query}
                onValueChange={setQuery}
                placeholder="Görev, proje, takım, kişi veya dosya ara…"
                className="h-14 flex-1 bg-transparent text-sm text-foreground placeholder:text-subtle-foreground outline-none"
              />
              {isFetching ? <Spinner className="text-subtle-foreground" /> : null}
            </div>

            <Command.List className="max-h-[22rem] overflow-y-auto p-2">
              {debouncedQuery.trim().length < 2 ? (
                <p className="px-3 py-8 text-center text-sm text-muted-foreground">
                  Aramak için en az 2 karakter yazın.
                </p>
              ) : null}

              {debouncedQuery.trim().length >= 2 && !isFetching && !hasResults ? (
                <p className="px-3 py-8 text-center text-sm text-muted-foreground">
                  “{debouncedQuery}” için sonuç bulunamadı.
                </p>
              ) : null}

              {data?.tasks.length ? (
                <Command.Group
                  heading="Görevler"
                  className="[&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:pt-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-subtle-foreground [&_[cmdk-group-heading]]:uppercase"
                >
                  {data.tasks.map((task) => (
                    <Command.Item
                      key={task.id}
                      value={task.id}
                      onSelect={() => go(`/gorevler/${task.key}`)}
                      className="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 data-[selected=true]:bg-surface-raised"
                    >
                      <SquareKanban
                        className="size-4 shrink-0 text-subtle-foreground"
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate text-sm">{task.title}</span>
                      <span className="shrink-0 font-mono text-xs text-subtle-foreground">
                        {task.key}
                      </span>
                      <span className="hidden shrink-0 text-xs text-muted-foreground sm:block">
                        {workItemStatusLabels[task.status]}
                      </span>
                    </Command.Item>
                  ))}
                </Command.Group>
              ) : null}

              {data?.projects.length ? (
                <Command.Group
                  heading="Projeler"
                  className="[&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:pt-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-subtle-foreground [&_[cmdk-group-heading]]:uppercase"
                >
                  {data.projects.map((project) => (
                    <Command.Item
                      key={project.id}
                      value={project.id}
                      onSelect={() => go(`/projeler/${project.id}`)}
                      className="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 data-[selected=true]:bg-surface-raised"
                    >
                      <FolderKanban
                        className="size-4 shrink-0"
                        style={{ color: project.colorHex }}
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate text-sm">{project.name}</span>
                      <span className="shrink-0 font-mono text-xs text-subtle-foreground">
                        {project.key}
                      </span>
                    </Command.Item>
                  ))}
                </Command.Group>
              ) : null}

              {data?.teams.length ? (
                <Command.Group
                  heading="Takımlar"
                  className="[&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:pt-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-subtle-foreground [&_[cmdk-group-heading]]:uppercase"
                >
                  {data.teams.map((team) => (
                    <Command.Item
                      key={team.id}
                      value={team.id}
                      onSelect={() => go(`/takimlar/${team.id}`)}
                      className="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 data-[selected=true]:bg-surface-raised"
                    >
                      <UsersIcon
                        className="size-4 shrink-0"
                        style={{ color: team.colorHex }}
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate text-sm">{team.name}</span>
                      <span className="shrink-0 text-xs text-muted-foreground">
                        {team.memberCount} üye
                      </span>
                    </Command.Item>
                  ))}
                </Command.Group>
              ) : null}

              {data?.users.length ? (
                <Command.Group
                  heading="Kişiler"
                  className="[&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:pt-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-subtle-foreground [&_[cmdk-group-heading]]:uppercase"
                >
                  {data.users.map((user) => (
                    <Command.Item
                      key={user.id}
                      value={user.id}
                      onSelect={() => go(`/kisiler/${user.id}`)}
                      className="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2 data-[selected=true]:bg-surface-raised"
                    >
                      <Avatar
                        fullName={user.fullName}
                        avatarUrl={user.avatarUrl}
                        size="xs"
                        isOnline={user.isOnline}
                      />
                      <span className="min-w-0 flex-1 truncate text-sm">{user.fullName}</span>
                      <span className="shrink-0 text-xs text-muted-foreground">
                        {systemRoleLabels[user.role]}
                      </span>
                    </Command.Item>
                  ))}
                </Command.Group>
              ) : null}

              {data?.attachments.length ? (
                <Command.Group
                  heading="Dosyalar"
                  className="[&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:pt-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-subtle-foreground [&_[cmdk-group-heading]]:uppercase"
                >
                  {data.attachments.map((attachment) => (
                    <Command.Item
                      key={attachment.id}
                      value={attachment.id}
                      onSelect={() => {
                        onOpenChange(false);
                        window.open(attachment.url, '_blank', 'noopener,noreferrer');
                      }}
                      className="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 data-[selected=true]:bg-surface-raised"
                    >
                      {attachment.category === 4 ? (
                        <FileText
                          className="size-4 shrink-0 text-subtle-foreground"
                          aria-hidden="true"
                        />
                      ) : (
                        <Paperclip
                          className="size-4 shrink-0 text-subtle-foreground"
                          aria-hidden="true"
                        />
                      )}
                      <span className="min-w-0 flex-1 truncate text-sm">{attachment.fileName}</span>
                    </Command.Item>
                  ))}
                </Command.Group>
              ) : null}
            </Command.List>
          </Command>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
