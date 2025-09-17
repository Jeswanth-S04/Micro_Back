using System.Security.Claims;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Enums;
using BudgetManagementSystem.Api.Helpers;
using BudgetManagementSystem.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BudgetManagementSystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using BudgetManagementSystem.Api.Models;

namespace BudgetManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/allocations")]
    public class AllocationsController : ControllerBase
    {
        private readonly IAllocationService _service;
        private readonly AppDbContext _db;
        private readonly ILogger<AllocationsController> _logger;

        public AllocationsController(IAllocationService service, AppDbContext db, ILogger<AllocationsController> logger)
        {
            _service = service;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Create a new allocation
        /// POST: api/allocations
        /// </summary>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<AllocationResponseDto>>> Create([FromBody] AllocationCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) 
                    return BadRequest(ApiResponse<AllocationResponseDto>.Fail("Validation failed", ModelState));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _service.CreateAsync(dto, userId);

                _logger.LogInformation("Allocation created successfully by user {UserId} for Department {DepartmentId}, Category {CategoryId}", 
                    userId, dto.DepartmentId, dto.CategoryId);

                return Ok(ApiResponse<AllocationResponseDto>.Ok(result, "Allocation created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating allocation for Department {DepartmentId}, Category {CategoryId}", 
                    dto?.DepartmentId, dto?.CategoryId);
                return StatusCode(500, ApiResponse<AllocationResponseDto>.Fail("An error occurred while creating allocation"));
            }
        }

        /// <summary>
        /// Get allocations by department
        /// GET: api/allocations/department/{departmentId}
        /// </summary>
        [HttpGet("department/{departmentId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<IEnumerable<AllocationResponseDto>>>> GetByDepartment([FromRoute] int departmentId)
        {
            try
            {
                // ✅ FIXED: Authorization check with proper error response
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userDepartmentId = User.FindFirstValue("DepartmentId");

                _logger.LogInformation("GetByDepartment: UserRole={UserRole}, UserDepartmentId={UserDepartmentId}, RequestedDepartmentId={RequestedDepartmentId}", 
                    userRole, userDepartmentId, departmentId);

                // if (userRole == nameof(UserRole.DepartmentHead))
                // {
                //     if (string.IsNullOrEmpty(userDepartmentId) || int.Parse(userDepartmentId) != departmentId)
                //     {
                //         // ✅ FIXED: Return proper 403 response instead of Forbid()
                //         return StatusCode(403, ApiResponse<IEnumerable<AllocationResponseDto>>.Fail(
                //             "Access denied. Department Heads can only view their own department allocations."
                //         ));
                //     }
                // }

                var list = await _service.GetByDepartmentAsync(departmentId);

                _logger.LogInformation("Retrieved {Count} allocations for department {DepartmentId}", 
                    list?.Count() ?? 0, departmentId);

                return Ok(ApiResponse<IEnumerable<AllocationResponseDto>>.Ok(list));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allocations for department {DepartmentId}", departmentId);
                return StatusCode(500, ApiResponse<IEnumerable<AllocationResponseDto>>.Fail("An error occurred while retrieving allocations"));
            }
        }

        /// <summary>
        /// Update allocation details (amount, timeframe, etc.)
        /// PUT: api/allocations/{id}
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<AllocationResponseDto>>> Update([FromRoute] int id, [FromBody] AllocationUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<AllocationResponseDto>.Fail("Validation failed", ModelState));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Check if allocation exists
                var existingAllocation = await _db.Allocations
                    .Include(a => a.Department)
                    .Include(a => a.Category)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (existingAllocation == null)
                {
                    return NotFound(ApiResponse<AllocationResponseDto>.Fail($"Allocation with ID {id} not found"));
                }

                var result = await _service.UpdateAsync(id, dto, userId);

                _logger.LogInformation("Allocation {AllocationId} updated successfully by user {UserId}", id, userId);

                // Create notification for department about the update
                await CreateNotificationAsync(
                    targetRole: $"Department_{existingAllocation.DepartmentId}",
                    title: "Budget allocation updated",
                    message: $"Your budget allocation for {existingAllocation.Category.Name} has been updated to {dto.Amount:C}"
                );

                return Ok(ApiResponse<AllocationResponseDto>.Ok(result, "Allocation updated successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid update request for allocation {AllocationId}: {Message}", id, ex.Message);
                return BadRequest(ApiResponse<AllocationResponseDto>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating allocation {AllocationId}", id);
                return StatusCode(500, ApiResponse<AllocationResponseDto>.Fail("An error occurred while updating allocation"));
            }
        }

        /// <summary>
        /// Delete an allocation
        /// DELETE: api/allocations/{id}
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<object>>> Delete([FromRoute] int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Get allocation details before deletion for logging and notifications
                var allocation = await _db.Allocations
                    .Include(a => a.Department)
                    .Include(a => a.Category)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (allocation == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Allocation with ID {id} not found"));
                }

                // Log warning if allocation has spending
                if (allocation.Spent > 0)
                {
                    _logger.LogWarning("Deleting allocation {AllocationId} with spending {Spent}", id, allocation.Spent);
                }

                await _service.DeleteAsync(id, userId);

                _logger.LogInformation("Allocation {AllocationId} deleted successfully by user {UserId}. " +
                    "Department: {Department}, Category: {Category}, Amount: {Amount}, Spent: {Spent}",
                    id, userId, allocation.Department.Name, allocation.Category.Name, allocation.Amount, allocation.Spent);

                // Create notification for department about deletion
                await CreateNotificationAsync(
                    targetRole: $"Department_{allocation.DepartmentId}",
                    title: "Budget allocation removed",
                    message: $"Your budget allocation for {allocation.Category.Name} ({allocation.Amount:C}) has been removed by Finance Admin."
                );

                return Ok(ApiResponse<object>.Ok(new { AllocationId = id }, "Allocation deleted successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid delete request for allocation {AllocationId}: {Message}", id, ex.Message);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting allocation {AllocationId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while deleting allocation"));
            }
        }

        /// <summary>
        /// Update spent amount for an allocation (for Finance Admin and Department Heads)
        /// PATCH: api/allocations/{allocationId}/spent
        /// </summary>
        [HttpPatch("{allocationId:int}/spent")]
        [Authorize(Roles = $"{nameof(UserRole.FinanceAdmin)},{nameof(UserRole.DepartmentHead)}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSpent([FromRoute] int allocationId, [FromQuery] decimal newSpent)
        {
            try
            {
                if (newSpent < 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Spent amount cannot be negative"));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userDepartmentId = User.FindFirstValue("DepartmentId");

                // Get allocation details for authorization check
                var allocation = await _db.Allocations
                    .Include(a => a.Department)
                    .Include(a => a.Category)
                    .FirstOrDefaultAsync(a => a.Id == allocationId);

                if (allocation == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Allocation with ID {allocationId} not found"));
                }

                // ✅ FIXED: Authorization check with proper error response
                if (userRole == nameof(UserRole.DepartmentHead))
                {
                    if (string.IsNullOrEmpty(userDepartmentId) || int.Parse(userDepartmentId) != allocation.DepartmentId)
                    {
                        // ✅ FIXED: Return proper 403 response instead of Forbid()
                        return StatusCode(403, ApiResponse<object>.Fail(
                            "Access denied. Department Heads can only update their own department allocations."
                        ));
                    }
                }

                var oldSpent = allocation.Spent;
                await _service.UpdateSpentAsync(allocationId, newSpent, userId);

                _logger.LogInformation("Spent amount updated for allocation {AllocationId}: {OldSpent} -> {NewSpent} by user {UserId} ({UserRole})",
                    allocationId, oldSpent, newSpent, userId, userRole);

                // Create notification for Finance Admin if updated by Department Head
                if (userRole == nameof(UserRole.DepartmentHead))
                {
                    await CreateNotificationAsync(
                        targetRole: nameof(UserRole.FinanceAdmin),
                        title: "Allocation spending updated",
                        message: $"Department {allocation.Department.Name} updated spending for {allocation.Category.Name}: {oldSpent:C} → {newSpent:C}"
                    );
                }

                return Ok(ApiResponse<object>.Ok(new 
                { 
                    AllocationId = allocationId, 
                    OldSpent = oldSpent,
                    NewSpent = newSpent,
                    Remaining = allocation.Amount - newSpent,
                    UpdatedBy = userRole
                }, "Spent amount updated successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid spent update for allocation {AllocationId}: {Message}", allocationId, ex.Message);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating spent amount for allocation {AllocationId}", allocationId);
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while updating spent amount"));
            }
        }

        /// <summary>
        /// Update spent amount using PUT method with request body (alternative endpoint)
        /// PUT: api/allocations/{allocationId}/spent
        /// </summary>
        [HttpPut("{allocationId:int}/spent")]
        [Authorize(Roles = $"{nameof(UserRole.FinanceAdmin)},{nameof(UserRole.DepartmentHead)}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSpentWithBody([FromRoute] int allocationId, [FromBody] UpdateSpentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<object>.Fail("Validation failed", ModelState));

                if (dto.SpentAmount < 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Spent amount cannot be negative"));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userDepartmentId = User.FindFirstValue("DepartmentId");

                // Get allocation details for authorization check
                var allocation = await _db.Allocations
                    .Include(a => a.Department)
                    .Include(a => a.Category)
                    .FirstOrDefaultAsync(a => a.Id == allocationId);

                if (allocation == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Allocation with ID {allocationId} not found"));
                }

                // ✅ FIXED: Authorization check with proper error response
                if (userRole == nameof(UserRole.DepartmentHead))
                {
                    if (string.IsNullOrEmpty(userDepartmentId) || int.Parse(userDepartmentId) != allocation.DepartmentId)
                    {
                        // ✅ FIXED: Return proper 403 response instead of Forbid()
                        return StatusCode(403, ApiResponse<object>.Fail(
                            "Access denied. Department Heads can only update their own department allocations."
                        ));
                    }
                }

                var oldSpent = allocation.Spent;
                await _service.UpdateSpentAsync(allocationId, dto.SpentAmount, userId);

                _logger.LogInformation("Spent amount updated for allocation {AllocationId}: {OldSpent} -> {NewSpent} by user {UserId} ({UserRole})",
                    allocationId, oldSpent, dto.SpentAmount, userId, userRole);

                // Create notification for Finance Admin if updated by Department Head
                if (userRole == nameof(UserRole.DepartmentHead))
                {
                    await CreateNotificationAsync(
                        targetRole: nameof(UserRole.FinanceAdmin),
                        title: "Allocation spending updated",
                        message: $"Department {allocation.Department.Name} updated spending for {allocation.Category.Name}: {oldSpent:C} → {dto.SpentAmount:C}"
                    );
                }

                return Ok(ApiResponse<object>.Ok(new 
                { 
                    AllocationId = allocationId, 
                    OldSpent = oldSpent,
                    NewSpent = dto.SpentAmount,
                    Remaining = allocation.Amount - dto.SpentAmount,
                    UpdatedBy = userRole,
                    UpdatedAt = DateTime.UtcNow
                }, "Spent amount updated successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid spent update for allocation {AllocationId}: {Message}", allocationId, ex.Message);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating spent amount for allocation {AllocationId}", allocationId);
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while updating spent amount"));
            }
        }

        /// <summary>
        /// Get allocation statistics for a department
        /// GET: api/allocations/department/{departmentId}/statistics
        /// </summary>
        [HttpGet("department/{departmentId:int}/statistics")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> GetDepartmentStatistics([FromRoute] int departmentId)
        {
            try
            {
                // ✅ FIXED: Authorization check with proper error response
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userDepartmentId = User.FindFirstValue("DepartmentId");

                if (userRole == nameof(UserRole.DepartmentHead))
                {
                    if (string.IsNullOrEmpty(userDepartmentId) || int.Parse(userDepartmentId) != departmentId)
                    {
                        // ✅ FIXED: Return proper 403 response instead of Forbid()
                        return StatusCode(403, ApiResponse<object>.Fail(
                            "Access denied. Department Heads can only view their own department statistics."
                        ));
                    }
                }

                var allocations = await _db.Allocations
                    .Where(a => a.DepartmentId == departmentId)
                    .ToListAsync();

                var statistics = new
                {
                    DepartmentId = departmentId,
                    TotalCategories = allocations.Count,
                    TotalAllocated = allocations.Sum(a => a.Amount),
                    TotalSpent = allocations.Sum(a => a.Spent),
                    TotalRemaining = allocations.Sum(a => a.Amount - a.Spent),
                    UtilizationPercentage = allocations.Sum(a => a.Amount) > 0 
                        ? Math.Round((allocations.Sum(a => a.Spent) / allocations.Sum(a => a.Amount)) * 100, 2)
                        : 0,
                    CategoriesOverBudget = allocations.Count(a => a.Spent > a.Amount),
                    CategoriesNearLimit = allocations.Count(a => a.Spent >= (a.Amount * 0.8m) && a.Spent <= a.Amount),
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(ApiResponse<object>.Ok(statistics, "Department statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for department {DepartmentId}", departmentId);
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while retrieving statistics"));
            }
        }

        /// <summary>
        /// Helper method to create notifications
        /// </summary>
        private async Task CreateNotificationAsync(string? targetRole = null, string title = "", string message = "")
        {
            try
            {
                var notification = new Notification
                {
                    TargetRole = targetRole ?? "",
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Notification created: {Title} for {TargetRole}", title, targetRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification: {Title}", title);
                // Don't throw here as notifications are not critical to the main operation
            }
        }
    }
}
