import {
  BarChart3,
  Bell,
  CalendarClock,
  CalendarDays,
  FolderKanban,
  LayoutDashboard,
  Megaphone,
  MessageSquare,
  Rocket,
  Settings,
  SquareKanban,
  Users,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

import { SystemRole } from '@/types/enums';

export interface NavigationItem {
  label: string;
  to: string;
  icon: LucideIcon;
  /** Boşsa tüm rollere açık. */
  roles?: SystemRole[];
}

export interface NavigationGroup {
  label: string | null;
  items: NavigationItem[];
}

/**
 * Kenar çubuğu menüsü. Rol kısıtı olan öğeler yetkisiz kullanıcıya
 * hiç gösterilmez; yetki denetimi ayrıca sunucuda da yapılır.
 */
export const navigationGroups: NavigationGroup[] = [
  {
    label: null,
    items: [
      { label: 'Panom', to: '/', icon: LayoutDashboard },
      { label: 'Görevlerim', to: '/gorevler', icon: SquareKanban },
      { label: 'Takvim', to: '/takvim', icon: CalendarDays },
      { label: 'Toplantılar', to: '/toplantilar', icon: CalendarClock },
      { label: 'Bildirimler', to: '/bildirimler', icon: Bell },
    ],
  },
  {
    label: 'Çalışma alanı',
    items: [
      { label: 'Projeler', to: '/projeler', icon: FolderKanban },
      { label: 'Takımlar', to: '/takimlar', icon: Users },
      { label: 'Sprintler', to: '/sprintler', icon: Rocket },
      { label: 'Sohbet', to: '/sohbet', icon: MessageSquare },
    ],
  },
  {
    label: 'Bilgi',
    items: [
      { label: 'Duyurular', to: '/duyurular', icon: Megaphone },
      {
        label: 'Raporlar',
        to: '/raporlar',
        icon: BarChart3,
        roles: [SystemRole.Admin, SystemRole.TeamLeader],
      },
    ],
  },
  {
    label: 'Yönetim',
    items: [
      {
        label: 'Yönetim paneli',
        to: '/yonetim',
        icon: Settings,
        roles: [SystemRole.Admin],
      },
    ],
  },
];

export function filterNavigation(
  groups: NavigationGroup[],
  role: SystemRole | undefined,
): NavigationGroup[] {
  return groups
    .map((group) => ({
      ...group,
      items: group.items.filter(
        (item) => !item.roles || (role !== undefined && item.roles.includes(role)),
      ),
    }))
    .filter((group) => group.items.length > 0);
}
