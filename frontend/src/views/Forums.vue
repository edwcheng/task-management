<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useForumsStore } from '../stores/forums';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';

const forumsStore = useForumsStore();
const authStore = useAuthStore();
const router = useRouter();

const showCreateModal = ref(false);
const newForumName = ref('');
const newForumDescription = ref('');
const creating = ref(false);

onMounted(() => {
  forumsStore.fetchForums();
});

function openForum(forumId: number) {
  router.push(`/forums/${forumId}`);
}

async function createForum() {
  if (!newForumName.value.trim()) return;

  creating.value = true;
  try {
    await forumsStore.createForum({
      name: newForumName.value,
      description: newForumDescription.value || undefined,
    });
    showCreateModal.value = false;
    newForumName.value = '';
    newForumDescription.value = '';
  } catch (error) {
    console.error('Failed to create forum:', error);
  } finally {
    creating.value = false;
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
  <div class="forums-page">
    <div class="page-header">
      <h1>Forums</h1>
      <button v-if="authStore.isAuthenticated" class="btn btn-primary" @click="showCreateModal = true">
        + New Forum
      </button>
    </div>

    <div v-if="forumsStore.loading" class="loading">Loading forums...</div>

    <div v-else-if="forumsStore.forums.length === 0" class="empty-state">
      <p>No forums yet. Create the first one!</p>
    </div>

    <div v-else class="forums-grid">
      <div
        v-for="forum in forumsStore.forums"
        :key="forum.id"
        class="forum-card"
        @click="openForum(forum.id)"
      >
        <div class="forum-header">
          <h3>{{ forum.name }}</h3>
          <span class="task-count">{{ forum.taskCount }} tasks</span>
        </div>
        <p v-if="forum.description" class="forum-description">{{ forum.description }}</p>
        <div class="forum-meta">
          <span>Created by {{ forum.createdByUsername }}</span>
          <span>{{ formatDate(forum.createdAt) }}</span>
        </div>
      </div>
    </div>

    <!-- Create Forum Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <h2>Create New Forum</h2>
        <form @submit.prevent="createForum">
          <div class="form-group">
            <label for="forumName">Forum Name</label>
            <input
              id="forumName"
              v-model="newForumName"
              type="text"
              required
              placeholder="Enter forum name"
            />
          </div>
          <div class="form-group">
            <label for="forumDescription">Description (Optional)</label>
            <textarea
              id="forumDescription"
              v-model="newForumDescription"
              placeholder="Enter forum description"
              rows="3"
            ></textarea>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showCreateModal = false">
              Cancel
            </button>
            <button type="submit" class="btn btn-primary" :disabled="creating">
              {{ creating ? 'Creating...' : 'Create' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.forums-page {
  padding: 1rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

h1 {
  color: #333;
  margin: 0;
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
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn-secondary {
  background: #f0f0f0;
  color: #333;
}

.btn-secondary:hover {
  background: #e0e0e0;
}

.loading {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: #666;
}

.forums-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.forum-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid #eee;
}

.forum-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
}

.forum-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.forum-header h3 {
  margin: 0;
  color: #333;
}

.task-count {
  background: #e8f4fd;
  color: #667eea;
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 500;
}

.forum-description {
  color: #666;
  margin: 0.5rem 0;
  line-height: 1.5;
}

.forum-meta {
  display: flex;
  justify-content: space-between;
  color: #999;
  font-size: 0.85rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #eee;
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
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
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
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
  box-sizing: border-box;
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #667eea;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1.5rem;
}
</style>
