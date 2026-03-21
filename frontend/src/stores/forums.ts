import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Forum, ForumListResponse, CreateForumRequest, UpdateForumRequest } from '../types';
import { forumsApi } from '../api';

export const useForumsStore = defineStore('forums', () => {
  const forums = ref<Forum[]>([]);
  const currentForum = ref<Forum | null>(null);
  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(20);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function fetchForums(page: number = 1, includeInactive: boolean = false) {
    loading.value = true;
    error.value = null;
    try {
      const response: ForumListResponse = await forumsApi.getAll(page, pageSize.value, includeInactive);
      forums.value = response.forums;
      totalCount.value = response.totalCount;
      currentPage.value = response.page;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch forums';
    } finally {
      loading.value = false;
    }
  }

  async function fetchForum(id: number) {
    loading.value = true;
    error.value = null;
    try {
      currentForum.value = await forumsApi.getById(id);
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch forum';
    } finally {
      loading.value = false;
    }
  }

  async function createForum(data: CreateForumRequest) {
    loading.value = true;
    error.value = null;
    try {
      const forum = await forumsApi.create(data);
      forums.value.unshift(forum);
      totalCount.value++;
      return forum;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to create forum';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function updateForum(id: number, data: UpdateForumRequest) {
    loading.value = true;
    error.value = null;
    try {
      const forum = await forumsApi.update(id, data);
      const index = forums.value.findIndex(f => f.id === id);
      if (index !== -1) {
        forums.value[index] = forum;
      }
      if (currentForum.value?.id === id) {
        currentForum.value = forum;
      }
      return forum;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to update forum';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function deleteForum(id: number) {
    loading.value = true;
    error.value = null;
    try {
      await forumsApi.delete(id);
      forums.value = forums.value.filter(f => f.id !== id);
      totalCount.value--;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete forum';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  return {
    forums,
    currentForum,
    totalCount,
    currentPage,
    pageSize,
    loading,
    error,
    fetchForums,
    fetchForum,
    createForum,
    updateForum,
    deleteForum,
  };
});
