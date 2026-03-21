import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { User, LoginRequest, RegisterRequest } from '../types';
import { authApi } from '../api';

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'));
  const user = ref<User | null>(JSON.parse(localStorage.getItem('user') || 'null'));

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const isAdmin = computed(() => user.value?.role === 'Admin');

  async function login(credentials: LoginRequest) {
    const response = await authApi.login(credentials);
    if (response.success && response.token && response.user) {
      token.value = response.token;
      user.value = response.user;
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));
    }
    return response;
  }

  async function register(data: RegisterRequest) {
    const response = await authApi.register(data);
    if (response.success && response.token && response.user) {
      token.value = response.token;
      user.value = response.user;
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));
    }
    return response;
  }

  function logout() {
    token.value = null;
    user.value = null;
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  function updateUser(updatedUser: User) {
    user.value = updatedUser;
    localStorage.setItem('user', JSON.stringify(updatedUser));
  }

  return {
    token,
    user,
    isAuthenticated,
    isAdmin,
    login,
    register,
    logout,
    updateUser,
  };
});
