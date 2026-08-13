import { lazy } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';

import { AppShell } from '@/components/layout/AppShell';
import { AdminPage } from '@/features/admin/AdminPage';
import { LoginPage } from '@/features/auth/LoginPage';
import { DashboardPage } from '@/features/dashboard/DashboardPage';
import { SystemRole } from '@/types/enums';

import { ProtectedRoute } from './ProtectedRoute';
import { RootErrorBoundary } from './RootErrorBoundary';

/*
 * Ağır ekranlar tembel yüklenir; giriş paketi küçük kalır. Dashboard ve giriş
 * ekranı doğrudan içe alınır çünkü ilk açılışta neredeyse her zaman gerekirler.
 */
const AnnouncementsPage = lazy(() =>
  import('@/features/announcements/AnnouncementsPage').then((m) => ({
    default: m.AnnouncementsPage,
  })),
);
const CalendarPage = lazy(() =>
  import('@/features/calendar/CalendarPage').then((m) => ({ default: m.CalendarPage })),
);
const ChatPage = lazy(() =>
  import('@/features/chat/ChatPage').then((m) => ({ default: m.ChatPage })),
);
const MeetingsPage = lazy(() =>
  import('@/features/meetings/MeetingsPage').then((m) => ({ default: m.MeetingsPage })),
);
const MeetingDetailPage = lazy(() =>
  import('@/features/meetings/MeetingDetailPage').then((m) => ({
    default: m.MeetingDetailPage,
  })),
);
const NotificationsPage = lazy(() =>
  import('@/features/notifications/NotificationsPage').then((m) => ({
    default: m.NotificationsPage,
  })),
);
const ProfilePage = lazy(() =>
  import('@/features/profile/ProfilePage').then((m) => ({ default: m.ProfilePage })),
);
const ProjectDetailPage = lazy(() =>
  import('@/features/projects/ProjectDetailPage').then((m) => ({
    default: m.ProjectDetailPage,
  })),
);
const ProjectsPage = lazy(() =>
  import('@/features/projects/ProjectsPage').then((m) => ({ default: m.ProjectsPage })),
);
const ReportsPage = lazy(() =>
  import('@/features/reports/ReportsPage').then((m) => ({ default: m.ReportsPage })),
);
const SprintsPage = lazy(() =>
  import('@/features/sprints/SprintsPage').then((m) => ({ default: m.SprintsPage })),
);
const MyTasksPage = lazy(() =>
  import('@/features/tasks/MyTasksPage').then((m) => ({ default: m.MyTasksPage })),
);
const TaskDetailPage = lazy(() =>
  import('@/features/tasks/TaskDetailPage').then((m) => ({ default: m.TaskDetailPage })),
);
const TeamDetailPage = lazy(() =>
  import('@/features/teams/TeamDetailPage').then((m) => ({ default: m.TeamDetailPage })),
);
const TeamsPage = lazy(() =>
  import('@/features/teams/TeamsPage').then((m) => ({ default: m.TeamsPage })),
);
const NotFoundPage = lazy(() => import('./NotFoundPage'));

/**
 * Uygulama rotaları. Yollar Türkçedir çünkü arayüzün tamamı Türkçe
 * ve adresler kullanıcıya görünür.
 *
 * Uygulama bir alt dizinden de sunulabildiği için taban yol Vite'ın BASE_URL
 * değerinden alınır; Render'da kök ('/'), alt dizin yayınlarında /<depo-adi>/ olur.
 */
export const router = createBrowserRouter(
  [
    {
      path: '/giris',
      element: <LoginPage />,
      errorElement: <RootErrorBoundary />,
    },
    {
      path: '/',
      element: (
        <ProtectedRoute>
          <AppShell />
        </ProtectedRoute>
      ),
      errorElement: <RootErrorBoundary />,
      children: [
        { index: true, element: <DashboardPage /> },

        { path: 'gorevler', element: <MyTasksPage /> },
        { path: 'gorevler/:key', element: <TaskDetailPage /> },
        { path: 'takvim', element: <CalendarPage /> },
        { path: 'toplantilar', element: <MeetingsPage /> },
        { path: 'toplantilar/:meetingId', element: <MeetingDetailPage /> },
        { path: 'bildirimler', element: <NotificationsPage /> },

        { path: 'projeler', element: <ProjectsPage /> },
        { path: 'projeler/:projectId', element: <ProjectDetailPage /> },
        { path: 'takimlar', element: <TeamsPage /> },
        { path: 'takimlar/:teamId', element: <TeamDetailPage /> },
        { path: 'sprintler', element: <SprintsPage /> },
        { path: 'sohbet', element: <ChatPage /> },
        { path: 'sohbet/:roomId', element: <ChatPage /> },

        { path: 'duyurular', element: <AnnouncementsPage /> },
        { path: 'profil', element: <ProfilePage /> },
        { path: 'kisiler/:userId', element: <ProfilePage /> },

        {
          path: 'raporlar',
          element: (
            <ProtectedRoute allowedRoles={[SystemRole.Admin, SystemRole.TeamLeader]}>
              <ReportsPage />
            </ProtectedRoute>
          ),
        },
        {
          path: 'yonetim',
          element: (
            <ProtectedRoute allowedRoles={[SystemRole.Admin]}>
              <AdminPage />
            </ProtectedRoute>
          ),
        },

        { path: '404', element: <NotFoundPage /> },
        { path: '*', element: <Navigate to="/404" replace /> },
      ],
    },
  ],
  { basename: import.meta.env.BASE_URL },
);
