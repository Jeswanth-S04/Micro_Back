using System.Security.Claims;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Enums;
using BudgetManagementSystem.Api.Helpers;
using BudgetManagementSystem.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardsController : ControllerBase
    {
        private readonly IAllocationService _allocations;

        public DashboardsController(IAllocationService allocations) => _allocations = allocations;

        // Department head dashboard summary
        [HttpGet("department")]
        [Authorize(Roles = nameof(UserRole.DepartmentHead))]
        public async Task<ActionResult<ApiResponse<DepartmentSummaryDto>>> DepartmentSummary()
        {
            var depIdStr = User.FindFirst("dep")?.Value;
            if (string.IsNullOrWhiteSpace(depIdStr)) return BadRequest(ApiResponse<DepartmentSummaryDto>.Fail("No department assigned"));
            var depId = int.Parse(depIdStr);
            var summary = await _allocations.GetDepartmentSummaryAsync(depId);
            return Ok(ApiResponse<DepartmentSummaryDto>.Ok(summary));
        }

        // Management summary (filters)
        [HttpGet("management")]
        [Authorize(Roles = nameof(UserRole.Management) + "," + nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<ManagementSummaryDto>>> ManagementSummary([FromQuery] int? categoryId, [FromQuery] int? departmentId)
        {
            var summary = await _allocations.GetManagementSummaryAsync(categoryId, departmentId);
            return Ok(ApiResponse<ManagementSummaryDto>.Ok(summary));
        }
    }
}
