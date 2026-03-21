using System.ComponentModel.DataAnnotations;

namespace TaskManagerAPI.DTOs
{
    public class CreateReplyRequest
    {
        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateReplyRequest
    {
        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public class ReplyDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
    }
}
