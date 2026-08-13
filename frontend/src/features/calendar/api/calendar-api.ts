import { apiClient } from '@/lib/api-client';
import type { CalendarItemDto } from '@/types/api';
import type { CalendarEventType } from '@/types/enums';

export interface CalendarRangeParams {
  from: string;
  to: string;
  projectId?: string;
  teamId?: string;
  onlyMine?: boolean;
}

export interface CalendarEventRequest {
  title: string;
  description?: string | null;
  type: CalendarEventType;
  startsAt: string;
  endsAt?: string | null;
  isAllDay: boolean;
  colorHex: string;
  projectId?: string | null;
  teamId?: string | null;
}

export const calendarApi = {
  async items(params: CalendarRangeParams): Promise<CalendarItemDto[]> {
    const { data } = await apiClient.get<CalendarItemDto[]>('/calendar', { params });
    return data;
  },

  async createEvent(request: CalendarEventRequest): Promise<CalendarItemDto> {
    const { data } = await apiClient.post<CalendarItemDto>('/calendar/events', request);
    return data;
  },

  async updateEvent(id: string, request: CalendarEventRequest): Promise<CalendarItemDto> {
    const { data } = await apiClient.put<CalendarItemDto>(`/calendar/events/${id}`, request);
    return data;
  },

  async deleteEvent(id: string): Promise<void> {
    await apiClient.delete(`/calendar/events/${id}`);
  },
};
