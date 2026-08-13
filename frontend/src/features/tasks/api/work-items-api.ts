import { apiClient } from '@/lib/api-client';
import type {
  ChecklistItemDto,
  CommentDto,
  KanbanBoardDto,
  PagedResult,
  WorkItemDetail,
  WorkItemSummary,
} from '@/types/api';
import type { WorkItemPriority, WorkItemStatus, WorkItemType } from '@/types/enums';

export interface WorkItemListParams {
  page?: number;
  pageSize?: number;
  projectId?: string;
  teamId?: string;
  sprintId?: string;
  assigneeId?: string;
  onlyMine?: boolean;
  status?: WorkItemStatus;
  priority?: WorkItemPriority;
  type?: WorkItemType;
  labelId?: string;
  search?: string;
  onlyOverdue?: boolean;
  onlyActive?: boolean;
  sortBy?: number;
  sortDescending?: boolean;
}

export interface CreateWorkItemRequest {
  projectId: string;
  title: string;
  description?: string | null;
  status: WorkItemStatus;
  priority: WorkItemPriority;
  type: WorkItemType;
  assigneeId?: string | null;
  teamId?: string | null;
  sprintId?: string | null;
  parentId?: string | null;
  startDate?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  storyPoints?: number | null;
  labelIds: string[];
  checklistItems: string[];
}

export interface UpdateWorkItemRequest {
  title: string;
  description?: string | null;
  priority: WorkItemPriority;
  type: WorkItemType;
  assigneeId?: string | null;
  teamId?: string | null;
  sprintId?: string | null;
  startDate?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  loggedHours?: number | null;
  storyPoints?: number | null;
  labelIds: string[];
}

/**
 * Kanban'da sürükle-bırak isteği. Hedef kolon ve komşu kartlar gönderilir;
 * sunucu iki komşunun ortasını alarak yalnızca taşınan kaydı güncelle.
 */
export interface MoveWorkItemRequest {
  targetStatus: WorkItemStatus;
  precedingItemId?: string | null;
  followingItemId?: string | null;
}

export const workItemsApi = {
  async list(params: WorkItemListParams = {}): Promise<PagedResult<WorkItemSummary>> {
    const { data } = await apiClient.get<PagedResult<WorkItemSummary>>('/work-items', { params });
    return data;
  },

  async board(
    projectId: string,
    teamId?: string,
    sprintId?: string,
  ): Promise<KanbanBoardDto> {
    const { data } = await apiClient.get<KanbanBoardDto>('/work-items/board', {
      params: { projectId, teamId, sprintId },
    });
    return data;
  },

  async detail(id: string): Promise<WorkItemDetail> {
    const { data } = await apiClient.get<WorkItemDetail>(`/work-items/${id}`);
    return data;
  },

  async byKey(key: string): Promise<WorkItemDetail> {
    const { data } = await apiClient.get<WorkItemDetail>(`/work-items/by-key/${key}`);
    return data;
  },

  async create(request: CreateWorkItemRequest): Promise<WorkItemDetail> {
    const { data } = await apiClient.post<WorkItemDetail>('/work-items', request);
    return data;
  },

  async update(id: string, request: UpdateWorkItemRequest): Promise<WorkItemDetail> {
    const { data } = await apiClient.put<WorkItemDetail>(`/work-items/${id}`, request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/work-items/${id}`);
  },

  async move(id: string, request: MoveWorkItemRequest): Promise<WorkItemSummary> {
    const { data } = await apiClient.put<WorkItemSummary>(`/work-items/${id}/move`, request);
    return data;
  },

  async changeStatus(id: string, status: WorkItemStatus): Promise<WorkItemSummary> {
    const { data } = await apiClient.put<WorkItemSummary>(`/work-items/${id}/status`, { status });
    return data;
  },

  async assign(id: string, assigneeId: string | null): Promise<WorkItemSummary> {
    const { data } = await apiClient.put<WorkItemSummary>(`/work-items/${id}/assignee`, {
      assigneeId,
    });
    return data;
  },

  async addChecklistItem(id: string, text: string): Promise<ChecklistItemDto[]> {
    const { data } = await apiClient.post<ChecklistItemDto[]>(`/work-items/${id}/checklist`, {
      text,
    });
    return data;
  },

  async updateChecklistItem(
    id: string,
    itemId: string,
    text: string,
    isCompleted: boolean,
  ): Promise<ChecklistItemDto[]> {
    const { data } = await apiClient.put<ChecklistItemDto[]>(
      `/work-items/${id}/checklist/${itemId}`,
      { text, isCompleted },
    );
    return data;
  },

  async deleteChecklistItem(id: string, itemId: string): Promise<ChecklistItemDto[]> {
    const { data } = await apiClient.delete<ChecklistItemDto[]>(
      `/work-items/${id}/checklist/${itemId}`,
    );
    return data;
  },

  async addComment(id: string, content: string, parentCommentId?: string): Promise<CommentDto> {
    const { data } = await apiClient.post<CommentDto>(`/work-items/${id}/comments`, {
      content,
      parentCommentId: parentCommentId ?? null,
    });
    return data;
  },

  async updateComment(id: string, commentId: string, content: string): Promise<CommentDto> {
    const { data } = await apiClient.put<CommentDto>(`/work-items/${id}/comments/${commentId}`, {
      content,
    });
    return data;
  },

  async deleteComment(id: string, commentId: string): Promise<void> {
    await apiClient.delete(`/work-items/${id}/comments/${commentId}`);
  },

  async uploadAttachment(id: string, file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);

    await apiClient.post(`/work-items/${id}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  async deleteAttachment(id: string, attachmentId: string): Promise<void> {
    await apiClient.delete(`/work-items/${id}/attachments/${attachmentId}`);
  },
};
