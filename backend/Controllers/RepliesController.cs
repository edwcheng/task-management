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
    public class RepliesController : ControllerBase
    {
        private readonly IReplyService _replyService;
        private readonly ILogger<RepliesController> _logger;

        public RepliesController(IReplyService replyService, ILogger<RepliesController> logger)
        {
            _replyService = replyService;
            _logger = logger;
        }

        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<List<ReplyDto>>> GetRepliesByTask(int taskId)
        {
            var result = await _replyService.GetRepliesByTaskIdAsync(taskId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReplyDto>> GetReply(int id)
        {
            var reply = await _replyService.GetReplyByIdAsync(id);
            if (reply == null)
            {
                return NotFound(new { message = "Reply not found" });
            }
            return Ok(reply);
        }

        [HttpPost("task/{taskId}")]
        [Authorize]
        public async Task<ActionResult<ReplyDto>> CreateReply(int taskId, [FromBody] CreateReplyRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _replyService.CreateReplyAsync(taskId, request, userId.Value);
            return CreatedAtAction(nameof(GetReply), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ReplyDto>> UpdateReply(int id, [FromBody] UpdateReplyRequest request)
        {
            var (userId, isAdmin) = GetCurrentUser();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _replyService.UpdateReplyAsync(id, request, userId.Value, isAdmin);
            if (result == null)
            {
                return NotFound(new { message = "Reply not found or you don't have permission to update it" });
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteReply(int id)
        {
            var (userId, isAdmin) = GetCurrentUser();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _replyService.DeleteReplyAsync(id, userId.Value, isAdmin);
            if (!result)
            {
                return NotFound(new { message = "Reply not found or you don't have permission to delete it" });
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
