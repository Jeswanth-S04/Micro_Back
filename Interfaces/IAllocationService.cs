using BudgetManagementSystem.Api.DTOs;

namespace BudgetManagementSystem.Api.Interfaces
{
    public interface IAllocationService
    {
        Task<AllocationResponseDto> CreateAsync(AllocationCreateDto dto, int userId);
        Task<IEnumerable<AllocationResponseDto>> GetByDepartmentAsync(int departmentId);
        Task<DepartmentSummaryDto> GetDepartmentSummaryAsync(int departmentId);
        Task<ManagementSummaryDto> GetManagementSummaryAsync(int? categoryId = null, int? departmentId = null);
        Task UpdateSpentAsync(int allocationId, decimal newSpent, int userId); // hook for expense integrations
        Task<AllocationResponseDto> UpdateAsync(int id, AllocationUpdateDto dto, int userId);
         Task DeleteAsync(int id, int userId);
    }
}