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
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet("forum/{forumId}")]
        public async Task<ActionResult<TaskListDto>> GetTasksByForum(
            int forumId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] TaskItemStatus? status = null,
            [FromQuery] bool? assignedToMe = null,
            [FromQuery] bool? unassigned = null)
        {
            int? currentUserId = null;
            if (assignedToMe == true)
            {
                currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { message = "You must be logged in to filter by 'assigned to me'" });
                }
            }

            var result = await _taskService.GetTasksAsync(forumId, page, pageSize, status, assignedToMe, unassigned, currentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDetailDto>> GetTask(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }
            return Ok(task);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _taskService.CreateTaskAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetTask), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<TaskDto>> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            var (userId, isAdmin) = GetCurrentUser();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _taskService.UpdateTaskAsync(id, request, userId.Value, isAdmin);
            if (result == null)
            {
                return NotFound(new { message = "Task not found or you don't have permission to update it" });
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteTask(int id)
        {
            var (userId, isAdmin) = GetCurrentUser();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _taskService.DeleteTaskAsync(id, userId.Value, isAdmin);
            if (!result)
            {
                return NotFound(new { message = "Task not found or you don't have permission to delete it" });
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
}
