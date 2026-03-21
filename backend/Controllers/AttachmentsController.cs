using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerAPI.Models;
using TaskManagerAPI.Services;

namespace TaskManagerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<AttachmentsController> _logger;

        public AttachmentsController(IAttachmentService attachmentService, ILogger<AttachmentsController> logger)
        {
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<DTOs.AttachmentDto>> UploadAttachment(
            IFormFile file,
            [FromQuery] int? taskId,
            [FromQuery] int? replyId)
        {
            var (userId, isAdmin) = GetCurrentUser();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _attachmentService.UploadAttachmentAsync(file, taskId, replyId, userId.Value, isAdmin);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to upload attachment" });
            }
            return Ok(result);
        }

        [HttpGet("{id}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var result = await _attachmentService.DownloadAttachmentAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Attachment not found" });
            }

            var (data, contentType, fileName) = result.Value;
            return File(data!, contentType, fileName);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAttachment(int id)
        {
            var (userId, isAdmin) = GetCurrentUser();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _attachmentService.DeleteAttachmentAsync(id, userId.Value, isAdmin);
            if (!result)
            {
                return NotFound(new { message = "Attachment not found or you don't have permission to delete it" });
            }
            return NoContent();
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
