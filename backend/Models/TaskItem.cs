using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskManagerAPI.Models
{
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public int ForumId { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        public int? AssignedToUserId { get; set; }

        [Required]
        public TaskItemStatus Status { get; set; } = TaskItemStatus.Open;

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DueDate { get; set; }

        // Navigation properties
        [JsonIgnore]
        public Forum Forum { get; set; } = null!;
        
        [JsonIgnore]
        public User CreatedBy { get; set; } = null!;
        
        [JsonIgnore]
        public User? AssignedTo { get; set; }
        
        [JsonIgnore]
        public ICollection<Reply> Replies { get; set; } = new List<Reply>();
        
        [JsonIgnore]
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }

    public enum TaskItemStatus
    {
        Open = 0,
        InProgress = 1,
        Completed = 2,
        Closed = 3,
        OnHold = 4
    }

    public enum TaskPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }
}
