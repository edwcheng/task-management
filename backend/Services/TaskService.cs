using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.DTOs;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Services
{
    public interface ITaskService
    {
        Task<TaskListDto> GetTasksAsync(int forumId, int page, int pageSize, TaskItemStatus? status = null, bool? assignedToMe = null, bool? unassigned = null, int? currentUserId = null);
        Task<TaskDetailDto?> GetTaskByIdAsync(int id);
        Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, int userId);
        Task<TaskDto?> UpdateTaskAsync(int id, UpdateTaskRequest request, int userId, bool isAdmin);
        Task<bool> DeleteTaskAsync(int id, int userId, bool isAdmin);
    }

    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskListDto> GetTasksAsync(int forumId, int page, int pageSize, TaskItemStatus? status = null, bool? assignedToMe = null, bool? unassigned = null, int? currentUserId = null)
        {
            var query = _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Forum)
                .Include(t => t.Attachments)
                .Where(t => t.ForumId == forumId);

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            // Assignment filters (mutually exclusive)
            if (assignedToMe == true && currentUserId.HasValue)
            {
                query = query.Where(t => t.AssignedToUserId == currentUserId.Value);
            }
            else if (unassigned == true)
            {
                query = query.Where(t => t.AssignedToUserId == null);
            }

            var totalCount = await query.CountAsync();

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get reply counts for all tasks in one query for efficiency
            var taskIds = tasks.Select(t => t.Id).ToList();
            var replyCounts = await _context.Replies
                .Where(r => taskIds.Contains(r.TaskId))
                .GroupBy(r => r.TaskId)
                .Select(g => new { TaskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TaskId, x => x.Count);

            return new TaskListDto
            {
                Tasks = tasks.Select(t => MapToTaskDto(t, replyCounts.GetValueOrDefault(t.Id, 0))).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<TaskDetailDto?> GetTaskByIdAsync(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Forum)
                .Include(t => t.Attachments)
                .Include(t => t.Replies)
                    .ThenInclude(r => r.User)
                .Include(t => t.Replies)
                    .ThenInclude(r => r.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return null;

            return MapToTaskDetailDto(task);
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, int userId)
        {
            var task = new TaskItem
            {
                Title = request.Title,
                Body = request.Body,
                ForumId = request.ForumId,
                CreatedByUserId = userId,
                AssignedToUserId = request.AssignedToUserId,
                Status = request.Status,
                Priority = request.Priority,
                DueDate = request.DueDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            task = await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Forum)
                .Include(t => t.Attachments)
                .FirstAsync(t => t.Id == task.Id);

            return MapToTaskDto(task);
        }

        public async Task<TaskDto?> UpdateTaskAsync(int id, UpdateTaskRequest request, int userId, bool isAdmin)
        {
            var task = await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Forum)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return null;

            // Only creator or admin can update
            if (!isAdmin && task.CreatedByUserId != userId)
            {
                return null;
            }

            if (request.Title != null)
                task.Title = request.Title;
            if (request.Body != null)
                task.Body = request.Body;
            if (request.AssignedToUserId.HasValue)
                task.AssignedToUserId = request.AssignedToUserId;
            if (request.Status.HasValue)
                task.Status = request.Status.Value;
            if (request.Priority.HasValue)
                task.Priority = request.Priority.Value;
            if (request.DueDate.HasValue)
                task.DueDate = request.DueDate;

            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToTaskDto(task);
        }

        public async Task<bool> DeleteTaskAsync(int id, int userId, bool isAdmin)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return false;

            // Only creator or admin can delete
            if (!isAdmin && task.CreatedByUserId != userId)
            {
                return false;
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return true;
        }

        private static TaskDto MapToTaskDto(TaskItem task, int replyCount = 0)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Body = task.Body,
                ForumId = task.ForumId,
                ForumName = task.Forum?.Name ?? "",
                CreatedByUserId = task.CreatedByUserId,
                CreatedByUsername = task.CreatedBy?.DisplayName ?? task.CreatedBy?.Username ?? "",
                AssignedToUserId = task.AssignedToUserId,
                AssignedToUsername = task.AssignedTo?.DisplayName ?? task.AssignedTo?.Username,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                Attachments = task.Attachments?.Select(MapToAttachmentDto).ToList() ?? new List<AttachmentDto>(),
                ReplyCount = replyCount
            };
        }

        private static TaskDetailDto MapToTaskDetailDto(TaskItem task)
        {
            return new TaskDetailDto
            {
                Id = task.Id,
                Title = task.Title,
                Body = task.Body,
                ForumId = task.ForumId,
                ForumName = task.Forum?.Name ?? "",
                CreatedByUserId = task.CreatedByUserId,
                CreatedByUsername = task.CreatedBy?.DisplayName ?? task.CreatedBy?.Username ?? "",
                AssignedToUserId = task.AssignedToUserId,
                AssignedToUsername = task.AssignedTo?.DisplayName ?? task.AssignedTo?.Username,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                Attachments = task.Attachments?.Select(MapToAttachmentDto).ToList() ?? new List<AttachmentDto>(),
                Replies = task.Replies?.OrderBy(r => r.CreatedAt).Select(MapToReplyDto).ToList() ?? new List<ReplyDto>()
            };
        }

        private static AttachmentDto MapToAttachmentDto(Attachment attachment)
        {
            return new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                DownloadUrl = $"/api/attachments/{attachment.Id}/download"
            };
        }

        private static ReplyDto MapToReplyDto(Reply reply)
        {
            return new ReplyDto
            {
                Id = reply.Id,
                TaskId = reply.TaskId,
                UserId = reply.UserId,
                Username = reply.User?.Username ?? "",
                UserDisplayName = reply.User?.DisplayName ?? "",
                Content = reply.Content,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt,
                Attachments = reply.Attachments?.Select(MapToAttachmentDto).ToList() ?? new List<AttachmentDto>()
            };
        }
    }
}
