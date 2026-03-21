import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Task, TaskDetail, TaskListResponse, CreateTaskRequest, UpdateTaskRequest, TaskStatus } from '../types';
import { tasksApi } from '../api';

export const useTasksStore = defineStore('tasks', () => {
  const tasks = ref<Task[]>([]);
  const currentTask = ref<TaskDetail | null>(null);
  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(20);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function fetchTasksByForum(
    forumId: number,
    page: number = 1,
    status?: TaskStatus,
    assignedToMe?: boolean,
    unassigned?: boolean
  ) {
    loading.value = true;
    error.value = null;
    try {
      const response: TaskListResponse = await tasksApi.getByForum(forumId, page, pageSize.value, status, assignedToMe, unassigned);
      tasks.value = response.tasks;
      totalCount.value = response.totalCount;
      currentPage.value = response.page;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch tasks';
    } finally {
      loading.value = false;
    }
  }

  async function fetchTask(id: number) {
    loading.value = true;
    error.value = null;
    try {
      currentTask.value = await tasksApi.getById(id);
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch task';
    } finally {
      loading.value = false;
    }
  }

  async function createTask(data: CreateTaskRequest) {
    loading.value = true;
    error.value = null;
    try {
      const task = await tasksApi.create(data);
      tasks.value.unshift(task);
      totalCount.value++;
      return task;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to create task';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function updateTask(id: number, data: UpdateTaskRequest) {
    loading.value = true;
    error.value = null;
    try {
      const task = await tasksApi.update(id, data);
      const index = tasks.value.findIndex(t => t.id === id);
      if (index !== -1) {
        tasks.value[index] = task;
      }
      if (currentTask.value?.id === id) {
        currentTask.value = { ...currentTask.value, ...task };
      }
      return task;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to update task';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function deleteTask(id: number) {
    loading.value = true;
    error.value = null;
    try {
      await tasksApi.delete(id);
      tasks.value = tasks.value.filter(t => t.id !== id);
      totalCount.value--;
      if (currentTask.value?.id === id) {
        currentTask.value = null;
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete task';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  function clearCurrentTask() {
    currentTask.value = null;
  }

  return {
    tasks,
    currentTask,
    totalCount,
    currentPage,
    pageSize,
    loading,
    error,
    fetchTasksByForum,
    fetchTask,
    createTask,
    updateTask,
    deleteTask,
    clearCurrentTask,
  };
});
