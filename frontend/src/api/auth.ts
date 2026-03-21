import apiClient from './client';
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '../types';

export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/auth/register', data);
    return response.data;
  },
};

export const usersApi = {
  getAll: async (): Promise<User[]> => {
    const response = await apiClient.get<User[]>('/users');
    return response.data;
  },

  getById: async (id: number): Promise<User> => {
    const response = await apiClient.get<User>(`/users/${id}`);
    return response.data;
  },

  update: async (id: number, data: Partial<User> & { password?: string }): Promise<User> => {
    const response = await apiClient.put<User>(`/users/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },

  setRole: async (id: number, role: string): Promise<void> => {
    await apiClient.put(`/users/${id}/role`, { role });
  },

  setActiveStatus: async (id: number, isActive: boolean): Promise<void> => {
    await apiClient.put(`/users/${id}/active`, { isActive });
  },

  changePassword: async (id: number, newPassword: string): Promise<void> => {
    await apiClient.put(`/users/${id}/password`, { newPassword });
  },
};
