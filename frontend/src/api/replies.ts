import apiClient from './client';
import type { Reply, CreateReplyRequest, UpdateReplyRequest } from '../types';

export const repliesApi = {
  getByTask: async (taskId: number): Promise<Reply[]> => {
    const response = await apiClient.get<Reply[]>(`/replies/task/${taskId}`);
    return response.data;
  },

  create: async (taskId: number, data: CreateReplyRequest): Promise<Reply> => {
    const response = await apiClient.post<Reply>(`/replies/task/${taskId}`, data);
    return response.data;
  },

  update: async (id: number, data: UpdateReplyRequest): Promise<Reply> => {
    const response = await apiClient.put<Reply>(`/replies/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/replies/${id}`);
  },
};
