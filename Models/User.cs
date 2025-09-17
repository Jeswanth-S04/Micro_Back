using System.ComponentModel.DataAnnotations;
using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(160)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
    }
}
