using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.DTOs;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Services
{
    public interface IAttachmentService
    {
        Task<AttachmentDto?> UploadAttachmentAsync(IFormFile file, int? taskId, int? replyId, int userId, bool isAdmin);
        Task<(byte[]? Data, string? ContentType, string? FileName)?> DownloadAttachmentAsync(int id);
        Task<bool> DeleteAttachmentAsync(int id, int userId, bool isAdmin);
    }

    public class AttachmentService : IAttachmentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<AttachmentService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<AttachmentDto?> UploadAttachmentAsync(IFormFile file, int? taskId, int? replyId, int userId, bool isAdmin)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate ownership
            if (taskId.HasValue)
            {
                var task = await _context.Tasks.FindAsync(taskId.Value);
                if (task == null || (!isAdmin && task.CreatedByUserId != userId))
                    return null;
            }

            if (replyId.HasValue)
            {
                var reply = await _context.Replies.FindAsync(replyId.Value);
                if (reply == null || (!isAdmin && reply.UserId != userId))
                    return null;
            }

            // Read file data into memory
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            // Create database record with file data stored directly
            var attachment = new Attachment
            {
                FileName = file.FileName,
                FileData = fileData,
                FilePath = null, // No longer using filesystem
                ContentType = file.ContentType,
                FileSize = file.Length,
                TaskId = taskId,
                ReplyId = replyId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {Id} uploaded successfully ({Size} bytes)", attachment.Id, attachment.FileSize);

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

        public async Task<(byte[]? Data, string? ContentType, string? FileName)?> DownloadAttachmentAsync(int id)
        {
            try
            {
                var attachment = await _context.Attachments.FindAsync(id);
                if (attachment == null)
                {
                    _logger.LogWarning("Attachment {Id} not found in database", id);
                    return null;
                }

                // First check if file data is stored in database (new method)
                if (attachment.FileData != null && attachment.FileData.Length > 0)
                {
                    _logger.LogInformation("Serving attachment {Id} from database ({Size} bytes)", id, attachment.FileData.Length);
                    return (attachment.FileData, attachment.ContentType, attachment.FileName);
                }

                // Fallback to file system (legacy support)
                if (!string.IsNullOrEmpty(attachment.FilePath) && File.Exists(attachment.FilePath))
                {
                    _logger.LogInformation("Serving attachment {Id} from filesystem: {Path}", id, attachment.FilePath);
                    var data = await File.ReadAllBytesAsync(attachment.FilePath);
                    return (data, attachment.ContentType, attachment.FileName);
                }

                _logger.LogWarning("Attachment {Id} has no data (FileData is null and file not found on disk)", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {Id}", id);
                return null;
            }
        }

        public async Task<bool> DeleteAttachmentAsync(int id, int userId, bool isAdmin)
        {
            var attachment = await _context.Attachments
                .Include(a => a.Task)
                .Include(a => a.Reply)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attachment == null) return false;

            // Check ownership
            bool isOwner = false;
            if (attachment.TaskId.HasValue && attachment.Task?.CreatedByUserId == userId)
                isOwner = true;
            if (attachment.ReplyId.HasValue && attachment.Reply?.UserId == userId)
                isOwner = true;

            if (!isAdmin && !isOwner)
                return false;

            // Delete file from filesystem if exists (legacy support)
            if (!string.IsNullOrEmpty(attachment.FilePath) && File.Exists(attachment.FilePath))
            {
                File.Delete(attachment.FilePath);
            }

            // Remove database record (FileData is deleted automatically)
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {Id} deleted", id);

            return true;
        }
    }
}
