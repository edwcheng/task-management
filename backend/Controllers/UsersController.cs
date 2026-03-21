using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerAPI.DTOs;
using TaskManagerAPI.Models;
using TaskManagerAPI.Services;

namespace TaskManagerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var (currentUserId, isAdmin) = GetCurrentUser();

            // Users can only view their own profile unless they're admin
            if (!isAdmin && id != currentUserId)
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var (currentUserId, isAdmin) = GetCurrentUser();

            var result = await _userService.UpdateUserAsync(id, request, currentUserId!.Value, isAdmin);
            if (result == null)
            {
                return NotFound(new { message = "User not found or you don't have permission to update it" });
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _userService.DeleteUserAsync(id, currentUserId!.Value, true);
            if (!result)
            {
                return BadRequest(new { message = "Cannot delete this user" });
            }
            return NoContent();
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SetUserRole(int id, [FromBody] SetRoleRequest request)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _userService.SetUserRoleAsync(id, request.Role, currentUserId!.Value);
            if (!result)
            {
                return BadRequest(new { message = "Cannot change this user's role" });
            }
            return NoContent();
        }

        [HttpPut("{id}/active")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SetUserActiveStatus(int id, [FromBody] SetActiveRequest request)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _userService.SetUserActiveStatusAsync(id, request.IsActive, currentUserId!.Value);
            if (!result)
            {
                return BadRequest(new { message = "Cannot change this user's active status" });
            }
            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        private (int? userId, bool isAdmin) GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            int? userId = null;
            if (int.TryParse(userIdClaim, out var parsedId))
            {
                userId = parsedId;
            }

            var isAdmin = roleClaim == UserRole.Admin.ToString();

            return (userId, isAdmin);
        }
    }

    public class SetRoleRequest
    {
        public UserRole Role { get; set; }
    }

    public class SetActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
