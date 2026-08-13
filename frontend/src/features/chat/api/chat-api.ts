import { apiClient } from '@/lib/api-client';
import type {
  ChatRoomDto,
  MessageDto,
  MessagePageDto,
  MessageReadReceiptDto,
} from '@/types/api';

export interface MessageHistoryParams {
  /** Bu zamandan önceki mesajlar getirilir (imleç tabanlı sayfalama). */
  before?: string;
  pageSize?: number;
}

export const chatApi = {
  async rooms(): Promise<ChatRoomDto[]> {
    const { data } = await apiClient.get<ChatRoomDto[]>('/chat/rooms');
    return data;
  },

  async leadersRoom(): Promise<ChatRoomDto> {
    const { data } = await apiClient.get<ChatRoomDto>('/chat/rooms/leaders');
    return data;
  },

  async messages(roomId: string, params: MessageHistoryParams = {}): Promise<MessagePageDto> {
    const { data } = await apiClient.get<MessagePageDto>(`/chat/rooms/${roomId}/messages`, {
      params,
    });
    return data;
  },

  async send(roomId: string, content: string, replyToMessageId?: string): Promise<MessageDto> {
    const { data } = await apiClient.post<MessageDto>(`/chat/rooms/${roomId}/messages`, {
      content,
      replyToMessageId: replyToMessageId ?? null,
    });
    return data;
  },

  async edit(roomId: string, messageId: string, content: string): Promise<MessageDto> {
    const { data } = await apiClient.put<MessageDto>(
      `/chat/rooms/${roomId}/messages/${messageId}`,
      { content },
    );
    return data;
  },

  async remove(roomId: string, messageId: string): Promise<void> {
    await apiClient.delete(`/chat/rooms/${roomId}/messages/${messageId}`);
  },

  /** Okundu işaretler ve odanın kalan okunmamış sayısını döner. */
  async markAsRead(roomId: string, messageIds: string[] = []): Promise<number> {
    const { data } = await apiClient.put<number>(`/chat/rooms/${roomId}/read`, { messageIds });
    return data;
  },

  async readReceipts(roomId: string, messageId: string): Promise<MessageReadReceiptDto[]> {
    const { data } = await apiClient.get<MessageReadReceiptDto[]>(
      `/chat/rooms/${roomId}/messages/${messageId}/reads`,
    );
    return data;
  },

  async sendAttachment(roomId: string, file: File, caption?: string): Promise<MessageDto> {
    const formData = new FormData();
    formData.append('file', file);
    if (caption) formData.append('caption', caption);

    const { data } = await apiClient.post<MessageDto>(
      `/chat/rooms/${roomId}/attachments`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    return data;
  },
};
