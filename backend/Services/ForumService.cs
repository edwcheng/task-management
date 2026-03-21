using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.DTOs;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Services
{
    public interface IForumService
    {
        Task<ForumListDto> GetForumsAsync(int page, int pageSize, bool includeInactive = false);
        Task<ForumDto?> GetForumByIdAsync(int id);
        Task<ForumDto> CreateForumAsync(CreateForumRequest request, int userId);
        Task<ForumDto?> UpdateForumAsync(int id, UpdateForumRequest request);
        Task<bool> DeleteForumAsync(int id);
    }

    public class ForumService : IForumService
    {
        private readonly AppDbContext _context;

        public ForumService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ForumListDto> GetForumsAsync(int page, int pageSize, bool includeInactive = false)
        {
            var query = _context.Forums
                .Include(f => f.CreatedBy)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }

            var totalCount = await query.CountAsync();

            var forums = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get task counts
            var forumIds = forums.Select(f => f.Id).ToList();
            var taskCounts = await _context.Tasks
                .Where(t => forumIds.Contains(t.ForumId))
                .GroupBy(t => t.ForumId)
                .Select(g => new { ForumId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ForumId, x => x.Count);

            return new ForumListDto
            {
                Forums = forums.Select(f => MapToForumDto(f, taskCounts.GetValueOrDefault(f.Id, 0))).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ForumDto?> GetForumByIdAsync(int id)
        {
            var forum = await _context.Forums
                .Include(f => f.CreatedBy)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (forum == null) return null;

            var taskCount = await _context.Tasks.CountAsync(t => t.ForumId == id);

            return MapToForumDto(forum, taskCount);
        }

        public async Task<ForumDto> CreateForumAsync(CreateForumRequest request, int userId)
        {
            var forum = new Forum
            {
                Name = request.Name,
                Description = request.Description,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Forums.Add(forum);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            forum = await _context.Forums
                .Include(f => f.CreatedBy)
                .FirstAsync(f => f.Id == forum.Id);

            return MapToForumDto(forum, 0);
        }

        public async Task<ForumDto?> UpdateForumAsync(int id, UpdateForumRequest request)
        {
            var forum = await _context.Forums
                .Include(f => f.CreatedBy)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (forum == null) return null;

            if (request.Name != null)
                forum.Name = request.Name;
            if (request.Description != null)
                forum.Description = request.Description;
            if (request.IsActive.HasValue)
                forum.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            var taskCount = await _context.Tasks.CountAsync(t => t.ForumId == id);
            return MapToForumDto(forum, taskCount);
        }

        public async Task<bool> DeleteForumAsync(int id)
        {
            var forum = await _context.Forums.FindAsync(id);
            if (forum == null) return false;

            _context.Forums.Remove(forum);
            await _context.SaveChangesAsync();

            return true;
        }

        private static ForumDto MapToForumDto(Forum forum, int taskCount)
        {
            return new ForumDto
            {
                Id = forum.Id,
                Name = forum.Name,
                Description = forum.Description,
                CreatedByUserId = forum.CreatedByUserId,
                CreatedByUsername = forum.CreatedBy?.DisplayName ?? forum.CreatedBy?.Username ?? "",
                CreatedAt = forum.CreatedAt,
                IsActive = forum.IsActive,
                TaskCount = taskCount
            };
        }
    }
}
