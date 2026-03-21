using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerAPI.Models
{
    public class Forum
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [JsonIgnore]
        public User CreatedBy { get; set; } = null!;
        
        [JsonIgnore]
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
