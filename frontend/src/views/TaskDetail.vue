<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useTasksStore } from '../stores/tasks';
import { useAuthStore } from '../stores/auth';
import { useUsersStore } from '../stores/users';
import { repliesApi, attachmentsApi } from '../api';
import type { Reply, TaskStatus, TaskPriority, UpdateTaskRequest } from '../types';

const route = useRoute();
const router = useRouter();
const tasksStore = useTasksStore();
const authStore = useAuthStore();
const usersStore = useUsersStore();

const taskId = computed(() => Number(route.params.id));

const showEditModal = ref(false);
const replyContent = ref('');
const submitting = ref(false);
const editingReply = ref<Reply | null>(null);
const editReplyContent = ref('');
const uploadingFiles = ref(false);

const editForm = ref({
  title: '',
  body: '',
  status: '' as TaskStatus,
  priority: '' as TaskPriority,
  assignedToUserId: undefined as number | undefined,
  dueDate: '',
});

onMounted(() => {
  tasksStore.fetchTask(taskId.value);
  usersStore.fetchUsers();
});

function canEdit(): boolean {
  if (!tasksStore.currentTask || !authStore.user) return false;
  return (
    authStore.isAdmin ||
    tasksStore.currentTask.createdByUserId === authStore.user.id
  );
}

function openEditModal() {
  if (!tasksStore.currentTask) return;
  editForm.value = {
    title: tasksStore.currentTask.title,
    body: tasksStore.currentTask.body,
    status: tasksStore.currentTask.status,
    priority: tasksStore.currentTask.priority,
    assignedToUserId: tasksStore.currentTask.assignedToUserId || undefined,
    dueDate: tasksStore.currentTask.dueDate
      ? new Date(tasksStore.currentTask.dueDate).toISOString().split('T')[0]
      : '',
  };
  showEditModal.value = true;
}

async function updateTask() {
  const updateData: UpdateTaskRequest = {};
  if (editForm.value.title) updateData.title = editForm.value.title;
  if (editForm.value.body) updateData.body = editForm.value.body;
  if (editForm.value.status) updateData.status = editForm.value.status;
  if (editForm.value.priority) updateData.priority = editForm.value.priority;
  if (editForm.value.assignedToUserId !== undefined)
    updateData.assignedToUserId = editForm.value.assignedToUserId;
  if (editForm.value.dueDate) updateData.dueDate = editForm.value.dueDate;

  try {
    await tasksStore.updateTask(taskId.value, updateData);
    showEditModal.value = false;
  } catch (error) {
    console.error('Failed to update task:', error);
  }
}

async function deleteTask() {
  if (!confirm('Are you sure you want to delete this task?')) return;

  try {
    await tasksStore.deleteTask(taskId.value);
    router.back();
  } catch (error) {
    console.error('Failed to delete task:', error);
  }
}

async function submitReply() {
  if (!replyContent.value.trim()) return;

  submitting.value = true;
  try {
    await repliesApi.create(taskId.value, { content: replyContent.value });
    replyContent.value = '';
    await tasksStore.fetchTask(taskId.value);
  } catch (error) {
    console.error('Failed to submit reply:', error);
  } finally {
    submitting.value = false;
  }
}

function startEditReply(reply: Reply) {
  editingReply.value = reply;
  editReplyContent.value = reply.content;
}

async function saveEditReply() {
  if (!editingReply.value || !editReplyContent.value.trim()) return;

  try {
    await repliesApi.update(editingReply.value.id, { content: editReplyContent.value });
    editingReply.value = null;
    editReplyContent.value = '';
    await tasksStore.fetchTask(taskId.value);
  } catch (error) {
    console.error('Failed to update reply:', error);
  }
}

async function deleteReply(replyId: number) {
  if (!confirm('Are you sure you want to delete this reply?')) return;

  try {
    await repliesApi.delete(replyId);
    await tasksStore.fetchTask(taskId.value);
  } catch (error) {
    console.error('Failed to delete reply:', error);
  }
}

async function handleFileUpload(event: Event) {
  const target = event.target as HTMLInputElement;
  const files = target.files;
  if (!files || files.length === 0) return;

  uploadingFiles.value = true;
  try {
    for (const file of Array.from(files)) {
      await attachmentsApi.upload(file, taskId.value);
    }
    await tasksStore.fetchTask(taskId.value);
    // Reset the file input
    target.value = '';
  } catch (error) {
    console.error('Failed to upload file:', error);
    alert('Failed to upload file. Please try again.');
  } finally {
    uploadingFiles.value = false;
  }
}

async function deleteAttachment(attachmentId: number) {
  if (!confirm('Are you sure you want to delete this attachment?')) return;

  try {
    await attachmentsApi.delete(attachmentId);
    await tasksStore.fetchTask(taskId.value);
  } catch (error) {
    console.error('Failed to delete attachment:', error);
  }
}

function canEditReply(reply: Reply): boolean {
  if (!authStore.user) return false;
  return authStore.isAdmin || reply.userId === authStore.user.id;
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
    hour: '2-digit',
    minute: '2-digit',
  });
}
</script>

<template>
  <div class="task-detail">
    <div v-if="tasksStore.loading" class="loading">Loading...</div>

    <template v-else-if="tasksStore.currentTask">
      <div class="back-nav">
        <router-link
          :to="`/forums/${tasksStore.currentTask.forumId}`"
          class="back-link"
        >
          &larr; Back to {{ tasksStore.currentTask.forumName }}
        </router-link>
      </div>

      <div class="task-header">
        <div class="task-title-section">
          <h1>{{ tasksStore.currentTask.title }}</h1>
          <div class="task-badges">
            <span
              class="badge status"
              :style="{ backgroundColor: getStatusColor(tasksStore.currentTask.status) }"
            >
              {{ tasksStore.currentTask.status }}
            </span>
            <span
              class="badge priority"
              :style="{ backgroundColor: getPriorityColor(tasksStore.currentTask.priority) }"
            >
              {{ tasksStore.currentTask.priority }}
            </span>
          </div>
        </div>
        <div v-if="canEdit()" class="task-actions">
          <button class="btn btn-secondary" @click="openEditModal">Edit</button>
          <button class="btn btn-danger" @click="deleteTask">Delete</button>
        </div>
      </div>

      <div class="task-meta">
        <div class="meta-item">
          <span class="label">Created by:</span>
          <span class="value">{{ tasksStore.currentTask.createdByUsername }}</span>
        </div>
        <div class="meta-item">
          <span class="label">Created:</span>
          <span class="value">{{ formatDate(tasksStore.currentTask.createdAt) }}</span>
        </div>
        <div class="meta-item">
          <span class="label">Assigned to:</span>
          <span class="value">
            {{ tasksStore.currentTask.assignedToUsername || 'Unassigned' }}
          </span>
        </div>
        <div v-if="tasksStore.currentTask.dueDate" class="meta-item">
          <span class="label">Due Date:</span>
          <span class="value">{{ formatDate(tasksStore.currentTask.dueDate) }}</span>
        </div>
      </div>

      <div class="task-body">
        <h3>Description</h3>
        <div class="body-content">{{ tasksStore.currentTask.body }}</div>
      </div>

      <div class="task-attachments">
        <h3>Attachments</h3>
        
        <!-- Upload Section -->
        <div class="upload-section">
          <label class="upload-btn" :class="{ disabled: uploadingFiles }">
            <input 
              type="file" 
              multiple 
              accept="*/*" 
              @change="handleFileUpload" 
              :disabled="uploadingFiles"
              style="display: none"
            />
            {{ uploadingFiles ? 'Uploading...' : '+ Upload Files' }}
          </label>
        </div>

        <!-- Attachments List -->
        <div v-if="tasksStore.currentTask.attachments.length > 0" class="attachments-list">
          <div 
            v-for="attachment in tasksStore.currentTask.attachments" 
            :key="attachment.id" 
            class="attachment-item"
          >
            <a
              :href="`/api/attachments/${attachment.id}/download`"
              class="attachment-link"
              target="_blank"
            >
              <span class="file-icon">📄</span>
              <span class="file-name">{{ attachment.fileName }}</span>
              <span class="file-size">{{ (attachment.fileSize / 1024).toFixed(1) }} KB</span>
            </a>
            <button 
              v-if="canEdit()" 
              class="delete-attachment-btn" 
              @click.stop="deleteAttachment(attachment.id)"
              title="Delete attachment"
            >
              ✕
            </button>
          </div>
        </div>
        <div v-else class="no-attachments">No attachments yet</div>
      </div>

      <div class="replies-section">
        <h3>{{ tasksStore.currentTask.replies.length }} Replies</h3>

        <div v-if="tasksStore.currentTask.replies.length === 0" class="no-replies">
          No replies yet. Be the first to reply!
        </div>

        <div v-else class="replies-list">
          <div v-for="reply in tasksStore.currentTask.replies" :key="reply.id" class="reply-card">
            <div v-if="editingReply?.id === reply.id" class="reply-edit">
              <textarea v-model="editReplyContent" rows="3"></textarea>
              <div class="edit-actions">
                <button class="btn btn-secondary btn-sm" @click="editingReply = null">
                  Cancel
                </button>
                <button class="btn btn-primary btn-sm" @click="saveEditReply">Save</button>
              </div>
            </div>
            <template v-else>
              <div class="reply-header">
                <span class="reply-author">{{ reply.userDisplayName }}</span>
                <span class="reply-date">{{ formatDate(reply.createdAt) }}</span>
              </div>
              <div class="reply-content">{{ reply.content }}</div>
              <div v-if="canEditReply(reply)" class="reply-actions">
                <button class="btn-link" @click="startEditReply(reply)">Edit</button>
                <button class="btn-link danger" @click="deleteReply(reply.id)">Delete</button>
              </div>
            </template>
          </div>
        </div>

        <div class="reply-form">
          <h4>Add a Reply</h4>
          <textarea
            v-model="replyContent"
            placeholder="Write your reply..."
            rows="3"
          ></textarea>
          <button
            class="btn btn-primary"
            :disabled="!replyContent.trim() || submitting"
            @click="submitReply"
          >
            {{ submitting ? 'Submitting...' : 'Submit Reply' }}
          </button>
        </div>
      </div>
    </template>

    <div v-else class="error">Task not found</div>

    <!-- Edit Task Modal -->
    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal">
        <h2>Edit Task</h2>
        <form @submit.prevent="updateTask">
          <div class="form-group">
            <label for="editTitle">Title</label>
            <input id="editTitle" v-model="editForm.title" type="text" required />
          </div>
          <div class="form-group">
            <label for="editBody">Description</label>
            <textarea id="editBody" v-model="editForm.body" required rows="5"></textarea>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label for="editStatus">Status</label>
              <select id="editStatus" v-model="editForm.status">
                <option value="Open">Open</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
                <option value="Closed">Closed</option>
                <option value="OnHold">On Hold</option>
              </select>
            </div>
            <div class="form-group">
              <label for="editPriority">Priority</label>
              <select id="editPriority" v-model="editForm.priority">
                <option value="Low">Low</option>
                <option value="Normal">Normal</option>
                <option value="High">High</option>
                <option value="Urgent">Urgent</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label for="editDueDate">Due Date</label>
            <input id="editDueDate" v-model="editForm.dueDate" type="date" />
          </div>
          <div class="form-group">
            <label for="editAssignedTo">Assign To</label>
            <select id="editAssignedTo" v-model="editForm.assignedToUserId">
              <option :value="undefined">-- Unassigned --</option>
              <option v-for="user in usersStore.users" :key="user.id" :value="user.id">
                {{ user.username }}
              </option>
            </select>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showEditModal = false">
              Cancel
            </button>
            <button type="submit" class="btn btn-primary">Save Changes</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.task-detail {
  padding: 1rem;
  max-width: 900px;
  margin: 0 auto;
}

.back-nav {
  margin-bottom: 1rem;
}

.back-link {
  color: #667eea;
  text-decoration: none;
  font-size: 0.9rem;
}

.back-link:hover {
  text-decoration: underline;
}

.loading,
.error,
.no-replies {
  text-align: center;
  padding: 3rem;
  color: #666;
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
}

.task-title-section h1 {
  color: #333;
  margin: 0 0 0.5rem;
}

.task-badges {
  display: flex;
  gap: 0.5rem;
}

.badge {
  padding: 0.35rem 0.75rem;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 500;
  color: white;
}

.task-actions {
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

.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
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

.btn-danger {
  background: #dc3545;
  color: white;
}

.btn-danger:hover {
  background: #c82333;
}

.task-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  padding: 1rem;
  background: #f8f9fa;
  border-radius: 8px;
  margin-bottom: 1.5rem;
}

.meta-item {
  display: flex;
  gap: 0.5rem;
}

.meta-item .label {
  color: #666;
}

.meta-item .value {
  font-weight: 500;
  color: #333;
}

.task-body {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  border: 1px solid #eee;
}

.task-body h3 {
  color: #333;
  margin: 0 0 1rem;
  font-size: 1.1rem;
}

.body-content {
  color: #444;
  line-height: 1.7;
  white-space: pre-wrap;
}

.task-attachments {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  border: 1px solid #eee;
}

.task-attachments h3 {
  color: #333;
  margin: 0 0 1rem;
  font-size: 1.1rem;
}

.upload-section {
  margin-bottom: 1rem;
}

.upload-btn {
  display: inline-block;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 500;
  transition: all 0.2s;
}

.upload-btn:hover:not(.disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.upload-btn.disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.no-attachments {
  color: #666;
  font-style: italic;
}

.attachments-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.attachment-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: #f8f9fa;
  border-radius: 6px;
}

.attachment-link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
  color: #333;
  flex: 1;
  transition: color 0.2s;
}

.attachment-link:hover {
  color: #667eea;
}

.delete-attachment-btn {
  background: none;
  border: none;
  color: #dc3545;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  transition: background 0.2s;
}

.delete-attachment-btn:hover {
  background: #fee2e2;
}

.file-icon {
  font-size: 1.2rem;
}

.file-name {
  font-weight: 500;
}

.file-size {
  color: #666;
  font-size: 0.8rem;
}

.replies-section {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  border: 1px solid #eee;
}

.replies-section h3 {
  color: #333;
  margin: 0 0 1rem;
  font-size: 1.1rem;
}

.replies-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.reply-card {
  padding: 1rem;
  background: #f8f9fa;
  border-radius: 8px;
}

.reply-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.reply-author {
  font-weight: 500;
  color: #333;
}

.reply-date {
  color: #999;
  font-size: 0.85rem;
}

.reply-content {
  color: #444;
  line-height: 1.6;
  white-space: pre-wrap;
}

.reply-actions {
  display: flex;
  gap: 1rem;
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid #e9ecef;
}

.btn-link {
  background: none;
  border: none;
  color: #667eea;
  cursor: pointer;
  padding: 0;
  font-size: 0.85rem;
}

.btn-link:hover {
  text-decoration: underline;
}

.btn-link.danger {
  color: #dc3545;
}

.reply-edit textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
  box-sizing: border-box;
  margin-bottom: 0.5rem;
}

.edit-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}

.reply-form {
  border-top: 1px solid #eee;
  padding-top: 1.5rem;
}

.reply-form h4 {
  color: #333;
  margin: 0 0 0.75rem;
  font-size: 1rem;
}

.reply-form textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
  box-sizing: border-box;
  margin-bottom: 0.75rem;
  resize: vertical;
}

.reply-form textarea:focus {
  outline: none;
  border-color: #667eea;
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
