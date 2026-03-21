<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useUsersStore } from '../stores/users';
import { useForumsStore } from '../stores/forums';
import { useAuthStore } from '../stores/auth';
import type { User, Forum } from '../types';

const usersStore = useUsersStore();
const forumsStore = useForumsStore();
const authStore = useAuthStore();

const activeTab = ref<'users' | 'forums'>('users');

const showUserModal = ref(false);
const editingUser = ref<User | null>(null);
const userForm = ref({
  displayName: '',
  email: '',
  role: '',
  isActive: true,
});

const showForumModal = ref(false);
const editingForum = ref<Forum | null>(null);
const forumForm = ref({
  name: '',
  description: '',
  isActive: true,
});

onMounted(() => {
  usersStore.fetchUsers();
  forumsStore.fetchForums(1, true);
});

function openUserModal(user: User) {
  editingUser.value = user;
  userForm.value = {
    displayName: user.displayName,
    email: user.email,
    role: user.role,
    isActive: user.isActive,
  };
  showUserModal.value = true;
}

async function saveUser() {
  if (!editingUser.value) return;

  try {
    await usersStore.updateUser(editingUser.value.id, {
      displayName: userForm.value.displayName,
      email: userForm.value.email,
    });

    if (userForm.value.role !== editingUser.value.role) {
      await usersStore.setRole(editingUser.value.id, userForm.value.role);
    }

    if (userForm.value.isActive !== editingUser.value.isActive) {
      await usersStore.setActiveStatus(editingUser.value.id, userForm.value.isActive);
    }

    showUserModal.value = false;
    editingUser.value = null;
  } catch (error) {
    console.error('Failed to save user:', error);
  }
}

async function deleteUser(user: User) {
  if (!confirm(`Are you sure you want to delete user "${user.displayName}"?`)) return;

  try {
    await usersStore.deleteUser(user.id);
  } catch (error) {
    console.error('Failed to delete user:', error);
  }
}

function openForumModal(forum: Forum) {
  editingForum.value = forum;
  forumForm.value = {
    name: forum.name,
    description: forum.description || '',
    isActive: forum.isActive,
  };
  showForumModal.value = true;
}

async function saveForum() {
  if (!editingForum.value) return;

  try {
    await forumsStore.updateForum(editingForum.value.id, {
      name: forumForm.value.name,
      description: forumForm.value.description || undefined,
      isActive: forumForm.value.isActive,
    });

    showForumModal.value = false;
    editingForum.value = null;
  } catch (error) {
    console.error('Failed to save forum:', error);
  }
}

async function deleteForum(forum: Forum) {
  if (!confirm(`Are you sure you want to delete forum "${forum.name}"?`)) return;

  try {
    await forumsStore.deleteForum(forum.id);
  } catch (error) {
    console.error('Failed to delete forum:', error);
  }
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}
</script>

<template>
  <div class="admin-page">
    <h1>Admin Dashboard</h1>

    <div class="tabs">
      <button
        :class="['tab', { active: activeTab === 'users' }]"
        @click="activeTab = 'users'"
      >
        Users ({{ usersStore.users.length }})
      </button>
      <button
        :class="['tab', { active: activeTab === 'forums' }]"
        @click="activeTab = 'forums'"
      >
        Forums ({{ forumsStore.forums.length }})
      </button>
    </div>

    <!-- Users Tab -->
    <div v-if="activeTab === 'users'" class="tab-content">
      <div v-if="usersStore.loading" class="loading">Loading users...</div>

      <table v-else class="admin-table">
        <thead>
          <tr>
            <th>Username</th>
            <th>Display Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in usersStore.users" :key="user.id">
            <td>{{ user.username }}</td>
            <td>{{ user.displayName }}</td>
            <td>{{ user.email }}</td>
            <td>
              <span :class="['role-badge', user.role.toLowerCase()]">{{ user.role }}</span>
            </td>
            <td>
              <span :class="['status-badge', user.isActive ? 'active' : 'inactive']">
                {{ user.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>{{ formatDate(user.createdAt) }}</td>
            <td class="actions">
              <button class="btn btn-sm btn-secondary" @click="openUserModal(user)">
                Edit
              </button>
              <button
                v-if="user.id !== authStore.user?.id"
                class="btn btn-sm btn-danger"
                @click="deleteUser(user)"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Forums Tab -->
    <div v-if="activeTab === 'forums'" class="tab-content">
      <div v-if="forumsStore.loading" class="loading">Loading forums...</div>

      <table v-else class="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Description</th>
            <th>Tasks</th>
            <th>Status</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="forum in forumsStore.forums" :key="forum.id">
            <td>{{ forum.name }}</td>
            <td>{{ forum.description || '-' }}</td>
            <td>{{ forum.taskCount }}</td>
            <td>
              <span :class="['status-badge', forum.isActive ? 'active' : 'inactive']">
                {{ forum.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>{{ formatDate(forum.createdAt) }}</td>
            <td class="actions">
              <button class="btn btn-sm btn-secondary" @click="openForumModal(forum)">
                Edit
              </button>
              <button class="btn btn-sm btn-danger" @click="deleteForum(forum)">
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- User Edit Modal -->
    <div v-if="showUserModal" class="modal-overlay" @click.self="showUserModal = false">
      <div class="modal">
        <h2>Edit User</h2>
        <form @submit.prevent="saveUser">
          <div class="form-group">
            <label>Username</label>
            <input :value="editingUser?.username" disabled />
          </div>
          <div class="form-group">
            <label for="displayName">Display Name</label>
            <input id="displayName" v-model="userForm.displayName" type="text" required />
          </div>
          <div class="form-group">
            <label for="email">Email</label>
            <input id="email" v-model="userForm.email" type="email" required />
          </div>
          <div class="form-group">
            <label for="role">Role</label>
            <select id="role" v-model="userForm.role">
              <option value="User">User</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
          <div class="form-group">
            <label class="checkbox-label">
              <input v-model="userForm.isActive" type="checkbox" />
              Active
            </label>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showUserModal = false">
              Cancel
            </button>
            <button type="submit" class="btn btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Forum Edit Modal -->
    <div v-if="showForumModal" class="modal-overlay" @click.self="showForumModal = false">
      <div class="modal">
        <h2>Edit Forum</h2>
        <form @submit.prevent="saveForum">
          <div class="form-group">
            <label for="forumName">Name</label>
            <input id="forumName" v-model="forumForm.name" type="text" required />
          </div>
          <div class="form-group">
            <label for="forumDesc">Description</label>
            <textarea id="forumDesc" v-model="forumForm.description" rows="3"></textarea>
          </div>
          <div class="form-group">
            <label class="checkbox-label">
              <input v-model="forumForm.isActive" type="checkbox" />
              Active
            </label>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showForumModal = false">
              Cancel
            </button>
            <button type="submit" class="btn btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-page {
  padding: 1rem;
}

h1 {
  color: #333;
  margin-bottom: 1.5rem;
}

.tabs {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  border-bottom: 2px solid #eee;
  padding-bottom: 0.5rem;
}

.tab {
  padding: 0.75rem 1.5rem;
  border: none;
  background: none;
  cursor: pointer;
  font-size: 1rem;
  color: #666;
  border-radius: 6px 6px 0 0;
  transition: all 0.2s;
}

.tab:hover {
  background: #f0f0f0;
}

.tab.active {
  color: #667eea;
  background: #f8f9ff;
  font-weight: 500;
}

.loading {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.admin-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
}

.admin-table th,
.admin-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #eee;
}

.admin-table th {
  background: #f8f9fa;
  font-weight: 600;
  color: #333;
}

.admin-table tbody tr:hover {
  background: #f8f9fa;
}

.actions {
  display: flex;
  gap: 0.5rem;
}

.btn {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-sm {
  padding: 0.35rem 0.75rem;
  font-size: 0.85rem;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn-secondary {
  background: #f0f0f0;
  color: #333;
}

.btn-secondary:hover {
  background: #e0e0e0;
}

.btn-danger {
  background: #dc3545;
  color: white;
}

.btn-danger:hover {
  background: #c82333;
}

.role-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 500;
}

.role-badge.admin {
  background: #fff3cd;
  color: #856404;
}

.role-badge.user {
  background: #d4edda;
  color: #155724;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 500;
}

.status-badge.active {
  background: #d4edda;
  color: #155724;
}

.status-badge.inactive {
  background: #f8d7da;
  color: #721c24;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal {
  background: white;
  padding: 2rem;
  border-radius: 12px;
  width: 100%;
  max-width: 500px;
}

.modal h2 {
  margin: 0 0 1.5rem;
  color: #333;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #555;
  font-weight: 500;
}

.form-group input,
.form-group textarea,
.form-group select {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
  box-sizing: border-box;
}

.form-group input:disabled {
  background: #f0f0f0;
  cursor: not-allowed;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.checkbox-label input[type="checkbox"] {
  width: auto;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1.5rem;
}
</style>
