using System.ComponentModel.DataAnnotations;
using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.Models
{
    public class AdjustmentRequest
    {
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        [Required]
        public int CategoryId { get; set; }
        public BudgetCategory Category { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Range(1, double.MaxValue)]
        public decimal Amount { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public string? ReviewerNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
    }
}
