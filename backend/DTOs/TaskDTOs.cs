using System.ComponentModel.DataAnnotations;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.DTOs
{
    public class CreateTaskRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public int ForumId { get; set; }

        public int? AssignedToUserId { get; set; }

        public TaskItemStatus Status { get; set; } = TaskItemStatus.Open;

        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        public DateTime? DueDate { get; set; }
    }

    public class UpdateTaskRequest
    {
        [StringLength(200, MinimumLength = 2)]
        public string? Title { get; set; }

        public string? Body { get; set; }

        public int? AssignedToUserId { get; set; }

        public TaskItemStatus? Status { get; set; }

        public TaskPriority? Priority { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUsername { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
        public int ReplyCount { get; set; }
    }

    public class TaskListDto
    {
        public List<TaskDto> Tasks { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class TaskDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUsername { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
        public List<ReplyDto> Replies { get; set; } = new();
    }
}
