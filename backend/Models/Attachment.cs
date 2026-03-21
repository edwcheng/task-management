using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerAPI.Models
{
    public class Attachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        // File data stored in database (for Railway compatibility)
        public byte[]? FileData { get; set; }

        // File path (nullable - not used when FileData is populated)
        [StringLength(500)]
        public string? FilePath { get; set; }

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        public int? TaskId { get; set; }

        public int? ReplyId { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [JsonIgnore]
        public TaskItem? Task { get; set; }
        
        [JsonIgnore]
        public Reply? Reply { get; set; }
    }
}
