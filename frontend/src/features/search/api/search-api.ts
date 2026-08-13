import { apiClient } from '@/lib/api-client';
import type { SearchResultsDto } from '@/types/api';

export const searchApi = {
  async search(query: string, limitPerType = 5): Promise<SearchResultsDto> {
    const { data } = await apiClient.get<SearchResultsDto>('/search', {
      params: { query, limitPerType },
    });
    return data;
  },
};
