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

            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create database record
            var attachment = new Attachment
            {
                FileName = file.FileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                TaskId = taskId,
                ReplyId = replyId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

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
            var attachment = await _context.Attachments.FindAsync(id);
            if (attachment == null || !File.Exists(attachment.FilePath))
                return null;

            var data = await File.ReadAllBytesAsync(attachment.FilePath);
            return (data, attachment.ContentType, attachment.FileName);
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

            // Delete file
            if (File.Exists(attachment.FilePath))
            {
                File.Delete(attachment.FilePath);
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
