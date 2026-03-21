import apiClient from './client';
import type { Attachment } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export const attachmentsApi = {
  upload: async (file: File, taskId?: number, replyId?: number): Promise<Attachment> => {
    const formData = new FormData();
    formData.append('file', file);

    const params = new URLSearchParams();
    if (taskId) params.append('taskId', taskId.toString());
    if (replyId) params.append('replyId', replyId.toString());

    const response = await apiClient.post<Attachment>(
      `/attachments/upload?${params.toString()}`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data;
  },

  download: async (id: number, fileName: string): Promise<void> => {
    const response = await apiClient.get(`/attachments/${id}/download`, {
      responseType: 'blob',
    });
    
    // Create blob URL and trigger download
    const blob = new Blob([response.data]);
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  },

  getDownloadUrl: (id: number): string => {
    return `${API_BASE_URL}/attachments/${id}/download`;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/attachments/${id}`);
  },
};
