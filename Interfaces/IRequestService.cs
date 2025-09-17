using BudgetManagementSystem.Api.DTOs;

namespace BudgetManagementSystem.Api.Interfaces
{
    public interface IRequestService
    {
        Task<AdjustmentRequestResponseDto> CreateAsync(AdjustmentRequestCreateDto dto, int userId);
        Task<AdjustmentRequestResponseDto> ReviewAsync(AdjustmentReviewDto dto, int reviewerUserId);
        Task<IEnumerable<AdjustmentRequestResponseDto>> GetPendingAsync();
        Task<IEnumerable<AdjustmentRequestResponseDto>> GetByDepartmentAsync(int departmentId);
    }
}