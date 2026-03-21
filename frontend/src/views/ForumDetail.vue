<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useForumsStore } from '../stores/forums';
import { useTasksStore } from '../stores/tasks';
import { useAuthStore } from '../stores/auth';
import type { TaskStatus, TaskPriority } from '../types';

const route = useRoute();
const router = useRouter();
const forumsStore = useForumsStore();
const tasksStore = useTasksStore();
const authStore = useAuthStore();

const forumId = computed(() => Number(route.params.id));

const showCreateModal = ref(false);
const selectedStatus = ref<TaskStatus | ''>('');
const assignmentFilter = ref<'all' | 'me' | 'unassigned'>('all');
const newTask = ref({
  title: '',
  body: '',
  assignedToUserId: undefined as number | undefined,
  status: 'Open' as TaskStatus,
  priority: 'Normal' as TaskPriority,
  dueDate: '',
});
const creating = ref(false);

onMounted(() => {
  forumsStore.fetchForum(forumId.value);
  tasksStore.fetchTasksByForum(forumId.value);
});

watch(forumId, (newId) => {
  forumsStore.fetchForum(newId);
  // Reset filters when changing forum
  selectedStatus.value = '';
  assignmentFilter.value = 'all';
  tasksStore.fetchTasksByForum(newId);
});

function filterByStatus(status: TaskStatus | '') {
  selectedStatus.value = status;
  fetchTasks();
}

function setAssignmentFilter(filter: 'all' | 'me' | 'unassigned') {
  assignmentFilter.value = filter;
  fetchTasks();
}

function fetchTasks() {
  const assignedToMe = assignmentFilter.value === 'me' ? true : undefined;
  const unassigned = assignmentFilter.value === 'unassigned' ? true : undefined;
  tasksStore.fetchTasksByForum(
    forumId.value,
    1,
    selectedStatus.value || undefined,
    assignedToMe,
    unassigned
  );
}

function openTask(taskId: number) {
  router.push(`/tasks/${taskId}`);
}

async function createTask() {
  if (!newTask.value.title.trim() || !newTask.value.body.trim()) return;

  creating.value = true;
  try {
    await tasksStore.createTask({
      title: newTask.value.title,
      body: newTask.value.body,
      forumId: forumId.value,
      assignedToUserId: newTask.value.assignedToUserId,
      status: newTask.value.status,
      priority: newTask.value.priority,
      dueDate: newTask.value.dueDate || undefined,
    });
    showCreateModal.value = false;
    newTask.value = {
      title: '',
      body: '',
      assignedToUserId: undefined,
      status: 'Open',
      priority: 'Normal',
      dueDate: '',
    };
  } catch (error) {
    console.error('Failed to create task:', error);
  } finally {
    creating.value = false;
  }
}

function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    Open: '#28a745',
    InProgress: '#007bff',
    Completed: '#6f42c1',
    Closed: '#6c757d',
    OnHold: '#ffc107',
  };
  return colors[status] || '#6c757d';
}

function getPriorityColor(priority: string): string {
  const colors: Record<string, string> = {
    Low: '#6c757d',
    Normal: '#28a745',
    High: '#fd7e14',
    Urgent: '#dc3545',
  };
  return colors[priority] || '#6c757d';
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
  <div class="forum-detail">
    <div v-if="forumsStore.loading" class="loading">Loading...</div>

    <template v-else-if="forumsStore.currentForum">
      <div class="page-header">
        <div>
          <router-link to="/forums" class="back-link">&larr; Back to Forums</router-link>
          <h1>{{ forumsStore.currentForum.name }}</h1>
          <p v-if="forumsStore.currentForum.description" class="forum-description">
            {{ forumsStore.currentForum.description }}
          </p>
        </div>
        <button class="btn btn-primary" @click="showCreateModal = true">+ New Task</button>
      </div>

      <div class="filters">
        <span class="filter-label">Filter by status:</span>
        <button
          :class="['filter-btn', { active: selectedStatus === '' }]"
          @click="filterByStatus('')"
        >
          All
        </button>
        <button
          v-for="status in ['Open', 'InProgress', 'Completed', 'Closed', 'OnHold']"
          :key="status"
          :class="['filter-btn', { active: selectedStatus === status }]"
          @click="filterByStatus(status as TaskStatus)"
        >
          {{ status }}
        </button>
      </div>

      <div class="filters" v-if="authStore.isAuthenticated">
        <span class="filter-label">Assignment:</span>
        <button
          :class="['filter-btn', { active: assignmentFilter === 'all' }]"
          @click="setAssignmentFilter('all')"
        >
          All Tasks
        </button>
        <button
          :class="['filter-btn', { active: assignmentFilter === 'me' }]"
          @click="setAssignmentFilter('me')"
        >
          Assigned to Me
        </button>
        <button
          :class="['filter-btn', { active: assignmentFilter === 'unassigned' }]"
          @click="setAssignmentFilter('unassigned')"
        >
          Unassigned
        </button>
      </div>

      <div v-if="tasksStore.loading" class="loading">Loading tasks...</div>

      <div v-else-if="tasksStore.tasks.length === 0" class="empty-state">
        <p>No tasks yet. Create the first one!</p>
      </div>

      <div v-else class="tasks-list">
        <div
          v-for="task in tasksStore.tasks"
          :key="task.id"
          class="task-card"
          @click="openTask(task.id)"
        >
          <div class="task-header">
            <h3>{{ task.title }}</h3>
            <div class="task-badges">
              <span
                class="badge status"
                :style="{ backgroundColor: getStatusColor(task.status) }"
              >
                {{ task.status }}
              </span>
              <span
                class="badge priority"
                :style="{ backgroundColor: getPriorityColor(task.priority) }"
              >
                {{ task.priority }}
              </span>
            </div>
          </div>
          <p class="task-body">{{ task.body.substring(0, 150) }}{{ task.body.length > 150 ? '...' : '' }}</p>
          <div class="task-meta">
            <div class="meta-left">
              <span v-if="task.assignedToUsername" class="assigned">
                Assigned to: <strong>{{ task.assignedToUsername }}</strong>
              </span>
              <span v-else class="unassigned">Unassigned</span>
            </div>
            <div class="meta-right">
              <span>{{ task.replyCount }} replies</span>
              <span>{{ formatDate(task.createdAt) }}</span>
            </div>
          </div>
        </div>
      </div>
    </template>

    <div v-else class="error">Forum not found</div>

    <!-- Create Task Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <h2>Create New Task</h2>
        <form @submit.prevent="createTask">
          <div class="form-group">
            <label for="taskTitle">Title</label>
            <input
              id="taskTitle"
              v-model="newTask.title"
              type="text"
              required
              placeholder="Enter task title"
            />
          </div>
          <div class="form-group">
            <label for="taskBody">Description</label>
            <textarea
              id="taskBody"
              v-model="newTask.body"
              required
              placeholder="Enter task description"
              rows="5"
            ></textarea>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label for="taskStatus">Status</label>
              <select id="taskStatus" v-model="newTask.status">
                <option value="Open">Open</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
                <option value="Closed">Closed</option>
                <option value="OnHold">On Hold</option>
              </select>
            </div>
            <div class="form-group">
              <label for="taskPriority">Priority</label>
              <select id="taskPriority" v-model="newTask.priority">
                <option value="Low">Low</option>
                <option value="Normal">Normal</option>
                <option value="High">High</option>
                <option value="Urgent">Urgent</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label for="taskDueDate">Due Date (Optional)</label>
            <input id="taskDueDate" v-model="newTask.dueDate" type="date" />
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
.forum-detail {
  padding: 1rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
}

.back-link {
  color: #667eea;
  text-decoration: none;
  font-size: 0.9rem;
}

.back-link:hover {
  text-decoration: underline;
}

h1 {
  color: #333;
  margin: 0.5rem 0;
}

.forum-description {
  color: #666;
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

.filters {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
}

.filter-label {
  color: #666;
  font-size: 0.9rem;
}

.filter-btn {
  padding: 0.5rem 1rem;
  border: 1px solid #ddd;
  background: white;
  border-radius: 20px;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.85rem;
}

.filter-btn:hover {
  border-color: #667eea;
}

.filter-btn.active {
  background: #667eea;
  color: white;
  border-color: #667eea;
}

.loading,
.empty-state,
.error {
  text-align: center;
  padding: 3rem;
  color: #666;
}

.tasks-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.task-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid #eee;
}

.task-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.1);
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.task-header h3 {
  margin: 0;
  color: #333;
}

.task-badges {
  display: flex;
  gap: 0.5rem;
}

.badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 500;
  color: white;
}

.task-body {
  color: #666;
  margin: 0 0 1rem;
  line-height: 1.5;
}

.task-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  color: #999;
  font-size: 0.85rem;
  padding-top: 1rem;
  border-top: 1px solid #eee;
}

.meta-right {
  display: flex;
  gap: 1rem;
}

.assigned {
  color: #333;
}

.unassigned {
  color: #999;
  font-style: italic;
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
  max-width: 600px;
  max-height: 90vh;
  overflow-y: auto;
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
.form-group textarea,
.form-group select {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
  box-sizing: border-box;
}

.form-group input:focus,
.form-group textarea:focus,
.form-group select:focus {
  outline: none;
  border-color: #667eea;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1.5rem;
}
</style>
