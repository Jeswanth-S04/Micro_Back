using System.ComponentModel.DataAnnotations;
using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.DTOs
{
    public class AdjustmentRequestCreateDto
    {
        [Required] public int DepartmentId { get; set; }
        [Required] public int CategoryId { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
        [Range(1, double.MaxValue)] public decimal Amount { get; set; }
    }

    public class AdjustmentReviewDto
    {
        [Required] public int RequestId { get; set; }
        [Required] public bool Approve { get; set; }
        public string? ReviewerNote { get; set; }
    }

    public class AdjustmentRequestResponseDto
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int CategoryId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequestStatus Status { get; set; }
        public string? ReviewerNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
