using System.Security.Claims;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Helpers;
using BudgetManagementSystem.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize] // Only authenticated users can access
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService service, ILogger<UsersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserResponseDto>>>> GetAll()
        {
            try
            {
                // Check if user has Finance Admin role (Role = 1)
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (userRole != "1") // Not Finance Admin
                {
                    return StatusCode(403, ApiResponse<IEnumerable<UserResponseDto>>.Fail("Access denied. Only Finance Admins can manage users."));
                }

                var users = await _service.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, ApiResponse<IEnumerable<UserResponseDto>>.Fail("Failed to retrieve users"));
            }
        }

        // GET: api/users/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetById([FromRoute] int id)
        {
            try
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (userRole != "1")
                {
                    return StatusCode(403, ApiResponse<UserResponseDto>.Fail("Access denied. Only Finance Admins can view user details."));
                }

                var user = await _service.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserResponseDto>.Fail("User not found"));
                }
                return Ok(ApiResponse<UserResponseDto>.Ok(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, ApiResponse<UserResponseDto>.Fail("Failed to retrieve user"));
            }
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> Create([FromBody] UserCreateDto dto)
        {
            try
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (userRole != "1")
                {
                    return StatusCode(403, ApiResponse<UserResponseDto>.Fail("Access denied. Only Finance Admins can create users."));
                }

                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<UserResponseDto>.Fail("Validation failed", ModelState));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _service.CreateAsync(dto, userId);
                return Ok(ApiResponse<UserResponseDto>.Ok(result, "User created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<UserResponseDto>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, ApiResponse<UserResponseDto>.Fail("Failed to create user"));
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> Update([FromRoute] int id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (userRole != "1")
                {
                    return StatusCode(403, ApiResponse<UserResponseDto>.Fail("Access denied. Only Finance Admins can update users."));
                }

                if (id != dto.Id)
                    return BadRequest(ApiResponse<UserResponseDto>.Fail("Mismatched user ID"));

                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<UserResponseDto>.Fail("Validation failed", ModelState));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _service.UpdateAsync(dto, userId);
                return Ok(ApiResponse<UserResponseDto>.Ok(result, "User updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<UserResponseDto>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, ApiResponse<UserResponseDto>.Fail("Failed to update user"));
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete([FromRoute] int id)
        {
            try
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (userRole != "1")
                {
                    return StatusCode(403, ApiResponse<object>.Fail("Access denied. Only Finance Admins can delete users."));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.DeleteAsync(id, userId);
                return Ok(ApiResponse<object>.Ok(new { id }, "User deleted successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Failed to delete user"));
            }
        }
    }
}
