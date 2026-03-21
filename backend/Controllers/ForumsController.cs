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
    public class ForumsController : ControllerBase
    {
        private readonly IForumService _forumService;
        private readonly ILogger<ForumsController> _logger;

        public ForumsController(IForumService forumService, ILogger<ForumsController> logger)
        {
            _forumService = forumService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ForumListDto>> GetForums(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeInactive = false)
        {
            var result = await _forumService.GetForumsAsync(page, pageSize, includeInactive);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ForumDto>> GetForum(int id)
        {
            var forum = await _forumService.GetForumByIdAsync(id);
            if (forum == null)
            {
                return NotFound(new { message = "Forum not found" });
            }
            return Ok(forum);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ForumDto>> CreateForum([FromBody] CreateForumRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _forumService.CreateForumAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetForum), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ForumDto>> UpdateForum(int id, [FromBody] UpdateForumRequest request)
        {
            var result = await _forumService.UpdateForumAsync(id, request);
            if (result == null)
            {
                return NotFound(new { message = "Forum not found" });
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteForum(int id)
        {
            var result = await _forumService.DeleteForumAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Forum not found" });
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
    }
}
