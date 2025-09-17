using BudgetManagementSystem.Api.DTOs;

namespace BudgetManagementSystem.Api.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
