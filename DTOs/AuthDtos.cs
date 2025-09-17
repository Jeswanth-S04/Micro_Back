using System.ComponentModel.DataAnnotations;
using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.DTOs
{
    public class LoginRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int? DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
