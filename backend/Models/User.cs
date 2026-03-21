using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [JsonIgnore]
        public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
        
        [JsonIgnore]
        public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
        
        [JsonIgnore]
        public ICollection<Reply> Replies { get; set; } = new List<Reply>();
    }

    public enum UserRole
    {
        User = 0,
        Admin = 1
    }
}
