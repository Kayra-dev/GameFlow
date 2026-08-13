import type {
  AnnouncementPriority,
  AttachmentCategory,
  CalendarEventType,
  ChatRoomType,
  MeetingStatus,
  NotificationType,
  ProjectStatus,
  SprintStatus,
  SystemRole,
  TeamCategory,
  TeamRole,
  WorkItemPriority,
  WorkItemStatus,
  WorkItemType,
} from './enums';

/** Backend'in ProblemDetails biçimindeki hata yanıtı. */
export interface ApiProblem {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/* ---------- Kimlik doğrulama ---------- */

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: CurrentUser;
}

export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  jobTitle: string | null;
  avatarUrl: string | null;
  role: SystemRole;
  mustChangePassword: boolean;
  /** Kullanıcının lider olduğu takım kimlikleri. */
  ledTeamIds: string[];
}

/* ---------- Kullanıcılar ---------- */

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  jobTitle: string | null;
  avatarUrl: string | null;
  role: SystemRole;
  isOnline: boolean;
  lastSeenAt: string | null;
}

export interface UserDetail extends UserSummary {
  bio: string | null;
  isActive: boolean;
  createdAt: string;
  teams: UserTeamDto[];
  projects: UserProjectDto[];
  completedTaskCount: number;
  activeTaskCount: number;
}

export interface UserTeamDto {
  id: string;
  name: string;
  category: TeamCategory;
  colorHex: string;
  teamRole: TeamRole;
}

export interface UserProjectDto {
  id: string;
  name: string;
  key: string;
  colorHex: string;
}

/* ---------- Takımlar ---------- */

export interface TeamSummary {
  id: string;
  name: string;
  category: TeamCategory;
  colorHex: string;
  iconKey: string | null;
  memberCount: number;
  leader: UserSummary | null;
}

export interface TeamDetail extends TeamSummary {
  description: string | null;
  createdAt: string;
  members: TeamMemberDto[];
  chatRoomId: string | null;
  /** Tamamlanan görev yüzdesi (0-100). */
  progressPercent: number;
  totalTaskCount: number;
  completedTaskCount: number;
  activeTaskCount: number;
  overdueTaskCount: number;
}

export interface TeamMemberDto {
  id: string;
  user: UserSummary;
  teamRole: TeamRole;
  joinedAt: string;
}

/* ---------- Projeler ---------- */

export interface ProjectSummary {
  id: string;
  name: string;
  key: string;
  status: ProjectStatus;
  colorHex: string;
  coverImageUrl: string | null;
  memberCount: number;
  taskCount: number;
  completedTaskCount: number;
}

export interface ProjectDetail extends ProjectSummary {
  description: string | null;
  genre: string | null;
  platforms: string | null;
  startDate: string | null;
  targetReleaseDate: string | null;
  createdAt: string;
  members: ProjectMemberDto[];
  activeSprint: SprintSummary | null;
  overdueTaskCount: number;
  progressPercent: number;
}

export interface ProjectMemberDto {
  id: string;
  user: UserSummary;
  isManager: boolean;
  joinedAt: string;
}

/* ---------- Görevler ---------- */

export interface WorkItemSummary {
  id: string;
  key: string;
  title: string;
  status: WorkItemStatus;
  priority: WorkItemPriority;
  type: WorkItemType;
  startDate: string | null;
  dueDate: string | null;
  boardOrder: number;
  assignee: UserSummary | null;
  projectId: string;
  projectKey: string;
  projectName: string;
  teamId: string | null;
  teamName: string | null;
  sprintId: string | null;
  storyPoints: number | null;
  labels: LabelDto[];
  commentCount: number;
  attachmentCount: number;
  checklistTotal: number;
  checklistCompleted: number;
  subItemCount: number;
  /** Son teslime kalan gün. null: tarih yok, negatif: gecikmiş. */
  daysUntilDue: number | null;
  isOverdue: boolean;
}

export interface WorkItemDetail extends WorkItemSummary {
  description: string | null;
  estimatedHours: number | null;
  loggedHours: number | null;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  reporter: UserSummary | null;
  sprintName: string | null;
  parentId: string | null;
  parentKey: string | null;
  subItems: WorkItemSummary[];
  checklistItems: ChecklistItemDto[];
  attachments: AttachmentDto[];
  comments: CommentDto[];
  activities: ActivityDto[];
}

export interface LabelDto {
  id: string;
  name: string;
  colorHex: string;
}

export interface ChecklistItemDto {
  id: string;
  text: string;
  isCompleted: boolean;
  order: number;
  completedAt: string | null;
}

export interface AttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  category: AttachmentCategory;
  url: string;
  uploadedBy: UserSummary | null;
  createdAt: string;
}

export interface CommentDto {
  id: string;
  content: string;
  author: UserSummary;
  createdAt: string;
  isEdited: boolean;
  editedAt: string | null;
  parentCommentId: string | null;
}

export interface ActivityDto {
  id: string;
  /** ActivityType enum değeri. */
  type: number;
  description: string;
  actor: UserSummary | null;
  createdAt: string;
}

/* ---------- Kanban ---------- */

export interface KanbanColumnDto {
  status: WorkItemStatus;
  title: string;
  totalCount: number;
  items: WorkItemSummary[];
}

export interface KanbanBoardDto {
  projectId: string;
  projectKey: string;
  columns: KanbanColumnDto[];
}

export interface DeadlineOverviewDto {
  dueToday: WorkItemSummary[];
  upcoming: WorkItemSummary[];
  overdue: WorkItemSummary[];
}

/* ---------- Sprintler ---------- */

export interface SprintSummary {
  id: string;
  name: string;
  status: SprintStatus;
  startDate: string;
  endDate: string;
  taskCount: number;
  completedTaskCount: number;
  progressPercent: number;
}

export interface SprintDetail extends SprintSummary {
  goal: string | null;
  projectId: string;
  projectKey: string;
  teamId: string | null;
  teamName: string | null;
  startedAt: string | null;
  completedAt: string | null;
  retrospectiveNotes: string | null;
  totalStoryPoints: number;
  completedStoryPoints: number;
  /** Bitişe kalan gün; negatifse gecikmiş. */
  remainingDays: number;
}

export interface SprintReportDto {
  sprintId: string;
  sprintName: string;
  status: SprintStatus;
  startDate: string;
  endDate: string;
  totalTaskCount: number;
  completedTaskCount: number;
  cancelledTaskCount: number;
  remainingTaskCount: number;
  overdueTaskCount: number;
  totalStoryPoints: number;
  completedStoryPoints: number;
  progressPercent: number;
  successPercent: number;
  totalEstimatedHours: number;
  totalLoggedHours: number;
  statusBreakdown: SprintStatusBreakdownDto[];
  memberContributions: SprintMemberContributionDto[];
}

export interface SprintStatusBreakdownDto {
  status: WorkItemStatus;
  label: string;
  count: number;
}

export interface SprintMemberContributionDto {
  user: UserSummary;
  assignedCount: number;
  completedCount: number;
  storyPoints: number;
}

/* ---------- Sohbet ---------- */

export interface ChatRoomDto {
  id: string;
  name: string;
  type: ChatRoomType;
  description: string | null;
  teamId: string | null;
  projectId: string | null;
  colorHex: string | null;
  unreadCount: number;
  lastMessage: MessageDto | null;
}

export interface MessageDto {
  id: string;
  chatRoomId: string;
  content: string;
  sender: UserSummary;
  createdAt: string;
  isEdited: boolean;
  editedAt: string | null;
  replyToMessageId: string | null;
  replyToPreview: string | null;
  replyToSenderName: string | null;
  attachments: AttachmentDto[];
  readByCount: number;
  isReadByMe: boolean;
}

/** Sohbet geçmişi sayfası. İmleç tabanlı; sayfa numarası yoktur. */
export interface MessagePageDto {
  items: MessageDto[];
  hasMore: boolean;
  nextCursor: string | null;
}

export interface MessageReadReceiptDto {
  user: UserSummary;
  readAt: string;
}

/* ---------- Bildirimler ---------- */

export interface NotificationDto {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  link: string | null;
  isRead: boolean;
  createdAt: string;
  actor: UserSummary | null;
}

/* ---------- Takvim ve toplantılar ---------- */

export interface CalendarItemDto {
  id: string;
  title: string;
  type: CalendarEventType;
  startsAt: string;
  endsAt: string | null;
  isAllDay: boolean;
  colorHex: string;
  /** İlgili kayda gitmek için istemci içi yol. */
  link: string | null;
  projectId: string | null;
  projectName: string | null;
  teamId: string | null;
  teamName: string | null;
}

export interface MeetingDto {
  id: string;
  title: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  location: string | null;
  meetingUrl: string | null;
  status: MeetingStatus;
  organizer: UserSummary;
  projectId: string | null;
  projectName: string | null;
  teamId: string | null;
  teamName: string | null;
  attendees: MeetingAttendeeDto[];
  /** Oturum sahibinin yanıtı: null = yanıt verilmedi. */
  myResponse: boolean | null;
}

export interface MeetingAttendeeDto {
  user: UserSummary;
  isAccepted: boolean | null;
  respondedAt: string | null;
}

/* ---------- Duyurular ---------- */

export interface AnnouncementDto {
  id: string;
  title: string;
  content: string;
  priority: AnnouncementPriority;
  isPinned: boolean;
  publishedAt: string;
  expiresAt: string | null;
  author: UserSummary;
  projectId: string | null;
  projectName: string | null;
}

/* ---------- Dashboard ---------- */

export interface DashboardDto {
  todayTasks: WorkItemSummary[];
  upcomingDeadlines: WorkItemSummary[];
  overdueTasks: WorkItemSummary[];
  completionPercent: number;
  totalTaskCount: number;
  completedTaskCount: number;
  activeTaskCount: number;
  recentActivities: ActivityDto[];
  announcements: AnnouncementDto[];
  onlineUsers: UserSummary[];
  activeSprints: SprintSummary[];
  upcomingMeetings: MeetingDto[];
}

/* ---------- Raporlar ---------- */

export interface ReportSeriesPoint {
  label: string;
  value: number;
  colorHex: string | null;
}

export interface TeamPerformanceRow {
  teamId: string;
  teamName: string;
  colorHex: string;
  completedTaskCount: number;
  activeTaskCount: number;
  overdueTaskCount: number;
  completionPercent: number;
  memberCount: number;
}

export interface UserPerformanceRow {
  userId: string;
  fullName: string;
  avatarUrl: string | null;
  completedTaskCount: number;
  activeTaskCount: number;
  overdueTaskCount: number;
  storyPoints: number;
}

export interface ReportsDto {
  teamPerformance: TeamPerformanceRow[];
  userPerformance: UserPerformanceRow[];
  statusDistribution: ReportSeriesPoint[];
  priorityDistribution: ReportSeriesPoint[];
  typeDistribution: ReportSeriesPoint[];
  weeklyCompleted: ReportSeriesPoint[];
  monthlyCompleted: ReportSeriesPoint[];
  sprintSuccess: ReportSeriesPoint[];
  totalTaskCount: number;
  completedTaskCount: number;
  overdueTaskCount: number;
  completionPercent: number;
}

/* ---------- Arama ---------- */

export interface SearchResultsDto {
  users: UserSummary[];
  tasks: WorkItemSummary[];
  teams: TeamSummary[];
  projects: ProjectSummary[];
  attachments: AttachmentDto[];
  /** Tüm türlerdeki sonuçların toplamı. */
  totalCount: number;
}
