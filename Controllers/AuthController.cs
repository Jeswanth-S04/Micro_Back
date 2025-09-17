using System.Security.Claims;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Helpers;
using BudgetManagementSystem.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<LoginResponse>.Fail("Validation failed", ModelState));
            var result = await _auth.LoginAsync(request);
            return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful"));
        }

        [HttpGet("me")]
        [Authorize]
        public ActionResult<ApiResponse<object>> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var dep = User.FindFirst("dep")?.Value;
            return Ok(ApiResponse<object>.Ok(new { userId, role, departmentId = dep }));
        }
    }
}
