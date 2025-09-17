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
    [Route("api/requests")]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestService _service;

        public RequestsController(IRequestService service) => _service = service;

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.DepartmentHead))]
        public async Task<ActionResult<ApiResponse<AdjustmentRequestResponseDto>>> Create([FromBody] AdjustmentRequestCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<AdjustmentRequestResponseDto>.Fail("Validation failed", ModelState));
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.CreateAsync(dto, userId);
            return Ok(ApiResponse<AdjustmentRequestResponseDto>.Ok(result, "Request submitted"));
        }

        [HttpPost("review")]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<AdjustmentRequestResponseDto>>> Review([FromBody] AdjustmentReviewDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.ReviewAsync(dto, userId);
            var message = result.Status == RequestStatus.Approved ? "Request approved" : "Request rejected";
            return Ok(ApiResponse<AdjustmentRequestResponseDto>.Ok(result, message));
        }

        [HttpGet("pending")]
        [Authorize(Roles = $"{nameof(UserRole.FinanceAdmin)},{nameof(UserRole.Management)}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AdjustmentRequestResponseDto>>>> Pending()
        {
            var list = await _service.GetPendingAsync();
            return Ok(ApiResponse<IEnumerable<AdjustmentRequestResponseDto>>.Ok(list));
        }


        [HttpGet("department/{departmentId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<IEnumerable<AdjustmentRequestResponseDto>>>> ByDepartment([FromRoute] int departmentId)
        {
            var list = await _service.GetByDepartmentAsync(departmentId);
            return Ok(ApiResponse<IEnumerable<AdjustmentRequestResponseDto>>.Ok(list));
        }
    }
}
