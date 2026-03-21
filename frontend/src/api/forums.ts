import apiClient from './client';
import type { Forum, ForumListResponse, CreateForumRequest, UpdateForumRequest } from '../types';

export const forumsApi = {
  getAll: async (page: number = 1, pageSize: number = 20, includeInactive: boolean = false): Promise<ForumListResponse> => {
    const response = await apiClient.get<ForumListResponse>('/forums', {
      params: { page, pageSize, includeInactive },
    });
    return response.data;
  },

  getById: async (id: number): Promise<Forum> => {
    const response = await apiClient.get<Forum>(`/forums/${id}`);
    return response.data;
  },

  create: async (data: CreateForumRequest): Promise<Forum> => {
    const response = await apiClient.post<Forum>('/forums', data);
    return response.data;
  },

  update: async (id: number, data: UpdateForumRequest): Promise<Forum> => {
    const response = await apiClient.put<Forum>(`/forums/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/forums/${id}`);
  },
};
