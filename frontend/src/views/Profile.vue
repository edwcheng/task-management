<script setup lang="ts">
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useUsersStore } from '../stores/users';

const authStore = useAuthStore();
const usersStore = useUsersStore();

const user = computed(() => authStore.user);

const editing = ref(false);
const form = ref({
  displayName: '',
  email: '',
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
});
const saving = ref(false);
const message = ref('');
const error = ref('');

function startEdit() {
  if (user.value) {
    form.value = {
      displayName: user.value.displayName,
      email: user.value.email,
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    };
    editing.value = true;
    message.value = '';
    error.value = '';
  }
}

async function saveProfile() {
  if (!user.value) return;

  if (form.value.newPassword && form.value.newPassword !== form.value.confirmPassword) {
    error.value = 'New passwords do not match';
    return;
  }

  saving.value = true;
  error.value = '';
  message.value = '';

  try {
    const updateData: any = {
      displayName: form.value.displayName,
      email: form.value.email,
    };

    if (form.value.newPassword) {
      updateData.password = form.value.newPassword;
    }

    const updatedUser = await usersStore.updateUser(user.value.id, updateData);
    authStore.updateUser(updatedUser);
    message.value = 'Profile updated successfully';
    editing.value = false;
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to update profile';
  } finally {
    saving.value = false;
  }
}

function cancelEdit() {
  editing.value = false;
  message.value = '';
  error.value = '';
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
</script>

<template>
  <div class="profile-page">
    <div class="profile-card">
      <h1>Profile</h1>

      <div v-if="message" class="alert success">{{ message }}</div>
      <div v-if="error" class="alert error">{{ error }}</div>

      <div v-if="!editing" class="profile-info">
        <div class="info-row">
          <span class="label">Username:</span>
          <span class="value">{{ user?.username }}</span>
        </div>
        <div class="info-row">
          <span class="label">Display Name:</span>
          <span class="value">{{ user?.displayName }}</span>
        </div>
        <div class="info-row">
          <span class="label">Email:</span>
          <span class="value">{{ user?.email }}</span>
        </div>
        <div class="info-row">
          <span class="label">Role:</span>
          <span :class="['role-badge', user?.role?.toLowerCase()]">{{ user?.role }}</span>
        </div>
        <div class="info-row">
          <span class="label">Member Since:</span>
          <span class="value">{{ user?.createdAt ? formatDate(user.createdAt) : '-' }}</span>
        </div>
        <div class="info-row">
          <span class="label">Last Login:</span>
          <span class="value">{{ user?.lastLoginAt ? formatDate(user.lastLoginAt) : '-' }}</span>
        </div>

        <button class="btn btn-primary" @click="startEdit">Edit Profile</button>
      </div>

      <form v-else @submit.prevent="saveProfile" class="edit-form">
        <div class="form-group">
          <label for="displayName">Display Name</label>
          <input id="displayName" v-model="form.displayName" type="text" required />
        </div>
        <div class="form-group">
          <label for="email">Email</label>
          <input id="email" v-model="form.email" type="email" required />
        </div>

        <div class="password-section">
          <h3>Change Password (optional)</h3>
          <div class="form-group">
            <label for="newPassword">New Password</label>
            <input
              id="newPassword"
              v-model="form.newPassword"
              type="password"
              placeholder="Enter new password"
              minlength="6"
            />
          </div>
          <div class="form-group">
            <label for="confirmPassword">Confirm New Password</label>
            <input
              id="confirmPassword"
              v-model="form.confirmPassword"
              type="password"
              placeholder="Confirm new password"
            />
          </div>
        </div>

        <div class="form-actions">
          <button type="button" class="btn btn-secondary" @click="cancelEdit">Cancel</button>
          <button type="submit" class="btn btn-primary" :disabled="saving">
            {{ saving ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.profile-page {
  display: flex;
  justify-content: center;
  padding: 2rem;
}

.profile-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 500px;
}

h1 {
  color: #333;
  margin: 0 0 1.5rem;
  text-align: center;
}

.alert {
  padding: 0.75rem 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
}

.alert.success {
  background: #d4edda;
  color: #155724;
}

.alert.error {
  background: #f8d7da;
  color: #721c24;
}

.profile-info {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.info-row {
  display: flex;
  justify-content: space-between;
  padding: 0.75rem 0;
  border-bottom: 1px solid #eee;
}

.info-row:last-of-type {
  border-bottom: none;
}

.info-row .label {
  color: #666;
}

.info-row .value {
  font-weight: 500;
  color: #333;
}

.role-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
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

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  margin-top: 1rem;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  background: #f0f0f0;
  color: #333;
}

.btn-secondary:hover {
  background: #e0e0e0;
}

.edit-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  color: #555;
  font-weight: 500;
}

.form-group input {
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
}

.form-group input:focus {
  outline: none;
  border-color: #667eea;
}

.password-section {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #eee;
}

.password-section h3 {
  color: #333;
  font-size: 1rem;
  margin: 0 0 1rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1rem;
}
</style>
