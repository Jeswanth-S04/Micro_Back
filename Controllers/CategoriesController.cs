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
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service) => _service = service;

        [HttpGet]

        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponseDto>>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>.Ok(list));
        }

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> Create([FromBody] CategoryCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<CategoryResponseDto>.Fail("Validation failed", ModelState));
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.CreateAsync(dto, userId);
            return Ok(ApiResponse<CategoryResponseDto>.Ok(result, "Category saved"));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> Update([FromRoute] int id, [FromBody] CategoryUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest(ApiResponse<CategoryResponseDto>.Fail("Mismatched id"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse<CategoryResponseDto>.Fail("Validation failed", ModelState));
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.UpdateAsync(dto, userId);
            return Ok(ApiResponse<CategoryResponseDto>.Ok(result, "Category updated"));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = nameof(UserRole.FinanceAdmin))]
        public async Task<ActionResult<ApiResponse<object>>> Delete([FromRoute] int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _service.DeleteAsync(id, userId);
            return Ok(ApiResponse<object>.Ok(new { id }, "Category deleted"));
        }

       

    }
}