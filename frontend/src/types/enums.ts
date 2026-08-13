/**
 * Backend enum değerlerinin birebir karşılıkları.
 * Sayısal değerler `GameFlow.Domain.Enums` altındaki tanımlarla eşleşmek zorundadır.
 */

export const SystemRole = {
  Admin: 1,
  TeamLeader: 2,
  TeamMember: 3,
} as const;
export type SystemRole = (typeof SystemRole)[keyof typeof SystemRole];

export const systemRoleLabels: Record<SystemRole, string> = {
  [SystemRole.Admin]: 'Yönetici',
  [SystemRole.TeamLeader]: 'Takım Lideri',
  [SystemRole.TeamMember]: 'Takım Üyesi',
};

export const TeamRole = {
  Leader: 1,
  Member: 2,
} as const;
export type TeamRole = (typeof TeamRole)[keyof typeof TeamRole];

export const teamRoleLabels: Record<TeamRole, string> = {
  [TeamRole.Leader]: 'Lider',
  [TeamRole.Member]: 'Üye',
};

export const WorkItemStatus = {
  Pending: 1,
  Todo: 2,
  InProgress: 3,
  CodeReview: 4,
  Testing: 5,
  Done: 6,
  Cancelled: 7,
} as const;
export type WorkItemStatus = (typeof WorkItemStatus)[keyof typeof WorkItemStatus];

export const workItemStatusLabels: Record<WorkItemStatus, string> = {
  [WorkItemStatus.Pending]: 'Bekliyor',
  [WorkItemStatus.Todo]: 'Yapılacak',
  [WorkItemStatus.InProgress]: 'Devam Ediyor',
  [WorkItemStatus.CodeReview]: 'Kod İncelemede',
  [WorkItemStatus.Testing]: 'Testte',
  [WorkItemStatus.Done]: 'Tamamlandı',
  [WorkItemStatus.Cancelled]: 'İptal',
};

/** Kanban kolonlarının ekrandaki sırası. */
export const kanbanColumnOrder: WorkItemStatus[] = [
  WorkItemStatus.Pending,
  WorkItemStatus.Todo,
  WorkItemStatus.InProgress,
  WorkItemStatus.CodeReview,
  WorkItemStatus.Testing,
  WorkItemStatus.Done,
  WorkItemStatus.Cancelled,
];

export const WorkItemPriority = {
  Lowest: 1,
  Low: 2,
  Medium: 3,
  High: 4,
  Critical: 5,
} as const;
export type WorkItemPriority = (typeof WorkItemPriority)[keyof typeof WorkItemPriority];

export const workItemPriorityLabels: Record<WorkItemPriority, string> = {
  [WorkItemPriority.Lowest]: 'En Düşük',
  [WorkItemPriority.Low]: 'Düşük',
  [WorkItemPriority.Medium]: 'Orta',
  [WorkItemPriority.High]: 'Yüksek',
  [WorkItemPriority.Critical]: 'Kritik',
};

export const WorkItemType = {
  Feature: 1,
  Bug: 2,
  Task: 3,
  Improvement: 4,
  Research: 5,
  ArtAsset: 6,
  AudioAsset: 7,
  LevelDesign: 8,
  Narrative: 9,
  Playtest: 10,
} as const;
export type WorkItemType = (typeof WorkItemType)[keyof typeof WorkItemType];

export const workItemTypeLabels: Record<WorkItemType, string> = {
  [WorkItemType.Feature]: 'Özellik',
  [WorkItemType.Bug]: 'Hata',
  [WorkItemType.Task]: 'Görev',
  [WorkItemType.Improvement]: 'İyileştirme',
  [WorkItemType.Research]: 'Araştırma',
  [WorkItemType.ArtAsset]: 'Görsel Varlık',
  [WorkItemType.AudioAsset]: 'Ses Varlığı',
  [WorkItemType.LevelDesign]: 'Seviye Tasarımı',
  [WorkItemType.Narrative]: 'Hikâye',
  [WorkItemType.Playtest]: 'Oynanış Testi',
};

export const TeamCategory = {
  Software: 1,
  Design: 2,
  UiUx: 3,
  Animation: 4,
  Audio: 5,
  QualityAssurance: 6,
  Narrative: 7,
  Production: 8,
  Marketing: 9,
} as const;
export type TeamCategory = (typeof TeamCategory)[keyof typeof TeamCategory];

export const teamCategoryLabels: Record<TeamCategory, string> = {
  [TeamCategory.Software]: 'Yazılım',
  [TeamCategory.Design]: 'Tasarım',
  [TeamCategory.UiUx]: 'UI/UX',
  [TeamCategory.Animation]: 'Animasyon',
  [TeamCategory.Audio]: 'Ses',
  [TeamCategory.QualityAssurance]: 'Test',
  [TeamCategory.Narrative]: 'Hikâye',
  [TeamCategory.Production]: 'Prodüksiyon',
  [TeamCategory.Marketing]: 'Pazarlama',
};

export const SprintStatus = {
  Planned: 1,
  Active: 2,
  Completed: 3,
  Cancelled: 4,
} as const;
export type SprintStatus = (typeof SprintStatus)[keyof typeof SprintStatus];

export const sprintStatusLabels: Record<SprintStatus, string> = {
  [SprintStatus.Planned]: 'Planlandı',
  [SprintStatus.Active]: 'Aktif',
  [SprintStatus.Completed]: 'Tamamlandı',
  [SprintStatus.Cancelled]: 'İptal',
};

export const ProjectStatus = {
  Planning: 1,
  InDevelopment: 2,
  Alpha: 3,
  Beta: 4,
  Released: 5,
  OnHold: 6,
  Archived: 7,
} as const;
export type ProjectStatus = (typeof ProjectStatus)[keyof typeof ProjectStatus];

export const projectStatusLabels: Record<ProjectStatus, string> = {
  [ProjectStatus.Planning]: 'Planlama',
  [ProjectStatus.InDevelopment]: 'Geliştiriliyor',
  [ProjectStatus.Alpha]: 'Alfa',
  [ProjectStatus.Beta]: 'Beta',
  [ProjectStatus.Released]: 'Yayınlandı',
  [ProjectStatus.OnHold]: 'Beklemede',
  [ProjectStatus.Archived]: 'Arşivlendi',
};

export const NotificationType = {
  TaskAssigned: 1,
  DeadlineApproaching: 2,
  TaskCommented: 3,
  MeetingCreated: 4,
  TaskUpdated: 5,
  SprintStarted: 6,
  SprintCompleted: 7,
  AnnouncementPublished: 8,
  MessageReceived: 9,
  MentionedInComment: 10,
  TaskOverdue: 11,
  AddedToTeam: 12,
  AddedToProject: 13,
} as const;
export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export const ChatRoomType = {
  Team: 1,
  Leaders: 2,
  Project: 3,
} as const;
export type ChatRoomType = (typeof ChatRoomType)[keyof typeof ChatRoomType];

export const MeetingStatus = {
  Scheduled: 1,
  InProgress: 2,
  Completed: 3,
  Cancelled: 4,
} as const;
export type MeetingStatus = (typeof MeetingStatus)[keyof typeof MeetingStatus];

export const meetingStatusLabels: Record<MeetingStatus, string> = {
  [MeetingStatus.Scheduled]: 'Planlandı',
  [MeetingStatus.InProgress]: 'Devam Ediyor',
  [MeetingStatus.Completed]: 'Tamamlandı',
  [MeetingStatus.Cancelled]: 'İptal',
};

export const CalendarEventType = {
  Custom: 1,
  Meeting: 2,
  Deadline: 3,
  SprintStart: 4,
  SprintEnd: 5,
  Release: 6,
  Playtest: 7,
  Milestone: 8,
} as const;
export type CalendarEventType = (typeof CalendarEventType)[keyof typeof CalendarEventType];

export const calendarEventTypeLabels: Record<CalendarEventType, string> = {
  [CalendarEventType.Custom]: 'Etkinlik',
  [CalendarEventType.Meeting]: 'Toplantı',
  [CalendarEventType.Deadline]: 'Son Tarih',
  [CalendarEventType.SprintStart]: 'Sprint Başlangıcı',
  [CalendarEventType.SprintEnd]: 'Sprint Bitişi',
  [CalendarEventType.Release]: 'Sürüm',
  [CalendarEventType.Playtest]: 'Oynanış Testi',
  [CalendarEventType.Milestone]: 'Kilometre Taşı',
};

export const AnnouncementPriority = {
  Info: 1,
  Warning: 2,
  Critical: 3,
} as const;
export type AnnouncementPriority = (typeof AnnouncementPriority)[keyof typeof AnnouncementPriority];

export const announcementPriorityLabels: Record<AnnouncementPriority, string> = {
  [AnnouncementPriority.Info]: 'Bilgi',
  [AnnouncementPriority.Warning]: 'Önemli',
  [AnnouncementPriority.Critical]: 'Kritik',
};

export const AttachmentCategory = {
  Image: 1,
  Pdf: 2,
  Archive: 3,
  Document: 4,
  Spreadsheet: 5,
  Video: 6,
  Audio: 7,
  Other: 8,
} as const;
export type AttachmentCategory = (typeof AttachmentCategory)[keyof typeof AttachmentCategory];
