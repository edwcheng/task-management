using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerAPI.Models
{
    public class Reply
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [JsonIgnore]
        public TaskItem Task { get; set; } = null!;
        
        [JsonIgnore]
        public User User { get; set; } = null!;
        
        [JsonIgnore]
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
