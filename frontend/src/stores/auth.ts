import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { User, LoginRequest, RegisterRequest } from '../types';
import { authApi } from '../api';

/**
 * Parse JWT token and return its payload
 * Returns null if token is invalid or not parseable
 */
function parseJwt(token: string): { exp?: number; [key: string]: unknown } | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const payload = parts[1];
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded);
  } catch {
    return null;
  }
}

/**
 * Check if JWT token is expired
 * Returns true if token is expired or invalid
 */
function isTokenExpired(token: string): boolean {
  const payload = parseJwt(token);
  if (!payload || !payload.exp) return true;

  // exp is in seconds, Date.now() is in milliseconds
  const expirationTime = payload.exp * 1000;
  const currentTime = Date.now();

  // Add 30 seconds buffer to handle clock skew
  return currentTime >= expirationTime - 30000;
}

/**
 * Check token on initialization and clear if expired
 * Returns the valid token or null
 */
function initializeToken(): string | null {
  const storedToken = localStorage.getItem('token');
  if (!storedToken) return null;

  if (isTokenExpired(storedToken)) {
    // Token expired, clear storage
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    return null;
  }

  return storedToken;
}

/**
 * Initialize user from localStorage only if token is valid
 */
function initializeUser(): User | null {
  const storedToken = localStorage.getItem('token');
  if (!storedToken) return null;

  // If token was cleared by initializeToken, user should be null too
  if (isTokenExpired(storedToken)) return null;

  try {
    const storedUser = localStorage.getItem('user');
    return storedUser ? JSON.parse(storedUser) : null;
  } catch {
    return null;
  }
}

export const useAuthStore = defineStore('auth', () => {
  // Initialize with token validation
  const token = ref<string | null>(initializeToken());
  const user = ref<User | null>(initializeUser());

  /**
   * Check if token is valid (not expired)
   * This is a pure computed property
   */
  const isTokenValid = computed(() => {
    if (!token.value) return false;
    return !isTokenExpired(token.value);
  });

  const isAuthenticated = computed(() => isTokenValid.value && !!user.value);
  const isAdmin = computed(() => user.value?.role === 'Admin');

  /**
   * Check token validity and logout if expired
   * Call this before API requests or route navigation
   */
  function checkTokenExpiry(): boolean {
    if (token.value && isTokenExpired(token.value)) {
      logout();
      return false;
    }
    return true;
  }

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
    isTokenValid,
    checkTokenExpiry,
    login,
    register,
    logout,
    updateUser,
  };
});
