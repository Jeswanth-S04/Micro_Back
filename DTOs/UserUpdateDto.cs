using System.ComponentModel.DataAnnotations;
using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.DTOs
{
    public class UserUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; } // Optional for updates

        [Required]
        [Range(1, 3, ErrorMessage = "Role must be 1 (Finance Admin), 2 (Department Head), or 3 (Management)")]
        public int Role { get; set; }

        public int? DepartmentId { get; set; } // Optional
    }
}
