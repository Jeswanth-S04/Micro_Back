using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetMy()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var items = await _db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync();
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpPost("{id:int}/read")]
        public async Task<ActionResult<ApiResponse<object>>> MarkRead([FromRoute] int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var n = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (n == null) return NotFound(ApiResponse<object>.Fail("Notification not found"));
            n.IsRead = true;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { id }, "Marked as read"));
        }
    }
}
