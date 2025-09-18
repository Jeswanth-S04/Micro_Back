using BudgetManagementSystem.Api.DTOs;

namespace BudgetManagementSystem.Api.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllAsync();
        Task<UserResponseDto?> GetByIdAsync(int id);
        Task<UserResponseDto> CreateAsync(UserCreateDto dto, int createdByUserId);
        Task<UserResponseDto> UpdateAsync(UserUpdateDto dto, int updatedByUserId);
        Task DeleteAsync(int id, int deletedByUserId);
        Task<bool> ActivateUserAsync(int id, int activatedByUserId);
    }
}
