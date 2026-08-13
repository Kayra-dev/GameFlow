import { apiClient } from '@/lib/api-client';
import type { MeetingDto } from '@/types/api';
import type { MeetingStatus } from '@/types/enums';

export interface MeetingListParams {
  projectId?: string;
  teamId?: string;
  status?: MeetingStatus;
  onlyUpcoming?: boolean;
  onlyMine?: boolean;
}

export interface MeetingRequest {
  title: string;
  description?: string | null;
  startsAt: string;
  endsAt: string;
  location?: string | null;
  meetingUrl?: string | null;
  projectId?: string | null;
  teamId?: string | null;
  attendeeIds: string[];
}

export interface UpdateMeetingRequest extends MeetingRequest {
  status: MeetingStatus;
}

export const meetingsApi = {
  async list(params: MeetingListParams = {}): Promise<MeetingDto[]> {
    const { data } = await apiClient.get<MeetingDto[]>('/meetings', { params });
    return data;
  },

  async detail(id: string): Promise<MeetingDto> {
    const { data } = await apiClient.get<MeetingDto>(`/meetings/${id}`);
    return data;
  },

  async create(request: MeetingRequest): Promise<MeetingDto> {
    const { data } = await apiClient.post<MeetingDto>('/meetings', request);
    return data;
  },

  async update(id: string, request: UpdateMeetingRequest): Promise<MeetingDto> {
    const { data } = await apiClient.put<MeetingDto>(`/meetings/${id}`, request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/meetings/${id}`);
  },

  /** Katılım yanıtı. Sunucu yalnızca katılımcı listesindekilerden kabul eder. */
  async respond(id: string, isAccepted: boolean): Promise<MeetingDto> {
    const { data } = await apiClient.post<MeetingDto>(`/meetings/${id}/respond`, { isAccepted });
    return data;
  },
};
