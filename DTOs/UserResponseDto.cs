using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.DTOs
{public class UserResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Role { get; set; } // 1,2,3
        public int? DepartmentId { get; set; } // Optional
        public string? DepartmentName { get; set; } // From join
    }
}
