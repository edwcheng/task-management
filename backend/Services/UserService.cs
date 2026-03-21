using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.DTOs;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request, int currentUserId, bool isAdmin);
        Task<bool> DeleteUserAsync(int id, int currentUserId, bool isAdmin);
        Task<bool> SetUserRoleAsync(int id, UserRole role, int currentUserId);
        Task<bool> SetUserActiveStatusAsync(int id, bool isActive, int currentUserId);
        Task<bool> ChangeUserPasswordAsync(int id, string newPassword, int currentUserId);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .OrderBy(u => u.Username)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            return MapToUserDto(user);
        }

        public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request, int currentUserId, bool isAdmin)
        {
            // Only admin or the user themselves can update
            if (!isAdmin && id != currentUserId)
            {
                return null;
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            if (request.DisplayName != null)
                user.DisplayName = request.DisplayName;
            if (request.Email != null)
                user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<bool> DeleteUserAsync(int id, int currentUserId, bool isAdmin)
        {
            // Only admin can delete users, and cannot delete themselves
            if (!isAdmin || id == currentUserId)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetUserRoleAsync(int id, UserRole role, int currentUserId)
        {
            // Cannot change own role
            if (id == currentUserId)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Role = role;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetUserActiveStatusAsync(int id, bool isActive, int currentUserId)
        {
            // Cannot deactivate self
            if (id == currentUserId)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = isActive;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangeUserPasswordAsync(int id, string newPassword, int currentUserId)
        {
            // Only admin can change another user's password
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || currentUser.Role != UserRole.Admin)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };
        }
    }
}
