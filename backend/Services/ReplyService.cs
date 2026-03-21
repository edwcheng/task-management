using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.DTOs;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Services
{
    public interface IReplyService
    {
        Task<List<ReplyDto>> GetRepliesByTaskIdAsync(int taskId);
        Task<ReplyDto?> GetReplyByIdAsync(int id);
        Task<ReplyDto> CreateReplyAsync(int taskId, CreateReplyRequest request, int userId);
        Task<ReplyDto?> UpdateReplyAsync(int id, UpdateReplyRequest request, int userId, bool isAdmin);
        Task<bool> DeleteReplyAsync(int id, int userId, bool isAdmin);
    }

    public class ReplyService : IReplyService
    {
        private readonly AppDbContext _context;

        public ReplyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReplyDto>> GetRepliesByTaskIdAsync(int taskId)
        {
            var replies = await _context.Replies
                .Include(r => r.User)
                .Include(r => r.Attachments)
                .Where(r => r.TaskId == taskId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return replies.Select(MapToReplyDto).ToList();
        }

        public async Task<ReplyDto?> GetReplyByIdAsync(int id)
        {
            var reply = await _context.Replies
                .Include(r => r.User)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reply == null) return null;

            return MapToReplyDto(reply);
        }

        public async Task<ReplyDto> CreateReplyAsync(int taskId, CreateReplyRequest request, int userId)
        {
            var reply = new Reply
            {
                TaskId = taskId,
                UserId = userId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Replies.Add(reply);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            reply = await _context.Replies
                .Include(r => r.User)
                .Include(r => r.Attachments)
                .FirstAsync(r => r.Id == reply.Id);

            return MapToReplyDto(reply);
        }

        public async Task<ReplyDto?> UpdateReplyAsync(int id, UpdateReplyRequest request, int userId, bool isAdmin)
        {
            var reply = await _context.Replies
                .Include(r => r.User)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reply == null) return null;

            // Only creator or admin can update
            if (!isAdmin && reply.UserId != userId)
            {
                return null;
            }

            reply.Content = request.Content;
            reply.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToReplyDto(reply);
        }

        public async Task<bool> DeleteReplyAsync(int id, int userId, bool isAdmin)
        {
            var reply = await _context.Replies.FindAsync(id);
            if (reply == null) return false;

            // Only creator or admin can delete
            if (!isAdmin && reply.UserId != userId)
            {
                return false;
            }

            _context.Replies.Remove(reply);
            await _context.SaveChangesAsync();

            return true;
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
    }
}
