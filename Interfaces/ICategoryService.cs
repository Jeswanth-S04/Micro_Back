using BudgetManagementSystem.Api.DTOs;

namespace BudgetManagementSystem.Api.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto, int userId);
        Task<CategoryResponseDto> UpdateAsync(CategoryUpdateDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
    }
}