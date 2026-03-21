import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { User } from '../types';
import { usersApi } from '../api';

export const useUsersStore = defineStore('users', () => {
  const users = ref<User[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function fetchUsers() {
    loading.value = true;
    error.value = null;
    try {
      users.value = await usersApi.getAll();
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch users';
    } finally {
      loading.value = false;
    }
  }

  async function updateUser(id: number, data: Partial<User> & { password?: string }) {
    loading.value = true;
    error.value = null;
    try {
      const user = await usersApi.update(id, data);
      const index = users.value.findIndex(u => u.id === id);
      if (index !== -1) {
        users.value[index] = user;
      }
      return user;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to update user';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function deleteUser(id: number) {
    loading.value = true;
    error.value = null;
    try {
      await usersApi.delete(id);
      users.value = users.value.filter(u => u.id !== id);
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete user';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function setRole(id: number, role: string) {
    loading.value = true;
    error.value = null;
    try {
      await usersApi.setRole(id, role);
      const user = users.value.find(u => u.id === id);
      if (user) {
        user.role = role;
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to set user role';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function setActiveStatus(id: number, isActive: boolean) {
    loading.value = true;
    error.value = null;
    try {
      await usersApi.setActiveStatus(id, isActive);
      const user = users.value.find(u => u.id === id);
      if (user) {
        user.isActive = isActive;
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to set user active status';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  return {
    users,
    loading,
    error,
    fetchUsers,
    updateUser,
    deleteUser,
    setRole,
    setActiveStatus,
  };
});
