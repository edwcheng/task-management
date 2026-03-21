<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { computed } from 'vue';

const router = useRouter();
const authStore = useAuthStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);
const isAdmin = computed(() => authStore.isAdmin);
const user = computed(() => authStore.user);

function handleLogout() {
  authStore.logout();
  router.push('/login');
}
</script>

<template>
  <nav class="navbar">
    <div class="navbar-brand">
      <router-link to="/" class="brand-link">Task Manager</router-link>
    </div>
    <div class="navbar-menu">
      <template v-if="isAuthenticated">
        <router-link to="/forums" class="nav-link">Forums</router-link>
        <router-link v-if="isAdmin" to="/admin" class="nav-link">Admin</router-link>
        <router-link to="/profile" class="nav-link">Profile</router-link>
        <div class="user-info">
          <span class="user-name">{{ user?.displayName }}</span>
          <span v-if="isAdmin" class="badge">Admin</span>
        </div>
        <button @click="handleLogout" class="btn btn-logout">Logout</button>
      </template>
      <template v-else>
        <router-link to="/login" class="nav-link">Login</router-link>
        <router-link to="/register" class="nav-link">Register</router-link>
      </template>
    </div>
  </nav>
</template>

<style scoped>
.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.navbar-brand {
  font-size: 1.5rem;
  font-weight: bold;
}

.brand-link {
  color: white;
  text-decoration: none;
}

.navbar-menu {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.nav-link {
  color: rgba(255, 255, 255, 0.9);
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  transition: all 0.2s;
}

.nav-link:hover {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: white;
  margin-left: 1rem;
}

.user-name {
  font-weight: 500;
}

.badge {
  background: rgba(255, 255, 255, 0.2);
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
}

.btn {
  padding: 0.5rem 1rem;
  border-radius: 6px;
  border: none;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.btn-logout {
  background: rgba(255, 255, 255, 0.2);
  color: white;
}

.btn-logout:hover {
  background: rgba(255, 255, 255, 0.3);
}
</style>
