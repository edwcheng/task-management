export interface User {
  id: number;
  username: string;
  email: string;
  displayName: string;
  role: string;
  createdAt: string;
  lastLoginAt: string | null;
  isActive: boolean;
}

export interface Forum {
  id: number;
  name: string;
  description: string | null;
  createdByUserId: number;
  createdByUsername: string;
  createdAt: string;
  isActive: boolean;
  taskCount: number;
}

export interface Task {
  id: number;
  title: string;
  body: string;
  forumId: number;
  forumName: string;
  createdByUserId: number;
  createdByUsername: string;
  assignedToUserId: number | null;
  assignedToUsername: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  createdAt: string;
  updatedAt: string | null;
  dueDate: string | null;
  attachments: Attachment[];
  replyCount: number;
}

export interface TaskDetail extends Task {
  replies: Reply[];
}

export interface Reply {
  id: number;
  taskId: number;
  userId: number;
  username: string;
  userDisplayName: string;
  content: string;
  createdAt: string;
  updatedAt: string | null;
  attachments: Attachment[];
}

export interface Attachment {
  id: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  downloadUrl: string;
}

export type TaskStatus = 'Open' | 'InProgress' | 'Completed' | 'Closed' | 'OnHold';
export type TaskPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token: string | null;
  user: User | null;
}

export interface CreateForumRequest {
  name: string;
  description?: string;
}

export interface UpdateForumRequest {
  name?: string;
  description?: string;
  isActive?: boolean;
}

export interface CreateTaskRequest {
  title: string;
  body: string;
  forumId: number;
  assignedToUserId?: number;
  status?: TaskStatus;
  priority?: TaskPriority;
  dueDate?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  body?: string;
  assignedToUserId?: number;
  status?: TaskStatus;
  priority?: TaskPriority;
  dueDate?: string;
}

export interface CreateReplyRequest {
  content: string;
}

export interface UpdateReplyRequest {
  content: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ForumListResponse {
  forums: Forum[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TaskListResponse {
  tasks: Task[];
  totalCount: number;
  page: number;
  pageSize: number;
}
