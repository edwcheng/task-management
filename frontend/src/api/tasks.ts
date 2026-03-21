import apiClient from './client';
import type { Task, TaskDetail, TaskListResponse, CreateTaskRequest, UpdateTaskRequest, TaskStatus } from '../types';

export const tasksApi = {
  getByForum: async (
    forumId: number,
    page: number = 1,
    pageSize: number = 20,
    status?: TaskStatus,
    assignedToMe?: boolean,
    unassigned?: boolean
  ): Promise<TaskListResponse> => {
    const response = await apiClient.get<TaskListResponse>(`/tasks/forum/${forumId}`, {
      params: { page, pageSize, status, assignedToMe, unassigned },
    });
    return response.data;
  },

  getById: async (id: number): Promise<TaskDetail> => {
    const response = await apiClient.get<TaskDetail>(`/tasks/${id}`);
    return response.data;
  },

  create: async (data: CreateTaskRequest): Promise<Task> => {
    const response = await apiClient.post<Task>('/tasks', data);
    return response.data;
  },

  update: async (id: number, data: UpdateTaskRequest): Promise<Task> => {
    const response = await apiClient.put<Task>(`/tasks/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/tasks/${id}`);
  },
};
