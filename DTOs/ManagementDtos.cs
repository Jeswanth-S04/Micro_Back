using BudgetManagementSystem.Api.Enums;

namespace BudgetManagementSystem.Api.DTOs
{
    public class ManagementDashboardDto
    {
        public ManagementSummaryDto Summary { get; set; } = new();
        public int PendingRequestsCount { get; set; }
        public IEnumerable<RecentRequestDto> RecentRequests { get; set; } = new List<RecentRequestDto>();
        public IEnumerable<BudgetAlertDto> BudgetAlerts { get; set; } = new List<BudgetAlertDto>();
        public IEnumerable<UtilizationTrendDto> UtilizationTrends { get; set; } = new List<UtilizationTrendDto>();
        public int TotalDepartments { get; set; }
        public int HighUtilizationDepartments { get; set; }
    }

    public class RecentRequestDto
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class BudgetAlertDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int ThresholdPercentage { get; set; }
        public string AlertType { get; set; } = string.Empty; // "NearingLimit" or "Exceeded"
        public string Severity { get; set; } = string.Empty; // "Low", "Medium", "High"
    }

    public class UtilizationTrendDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal UtilizationPercentage { get; set; }
        public decimal TotalAllocation { get; set; }
        public decimal TotalSpent { get; set; }
        public bool IsHighUtilization { get; set; }
    }

    public class DepartmentPerformanceDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalAllocation { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int CategoriesCount { get; set; }
        public int CategoriesNearingLimit { get; set; }
        public int CategoriesExceeded { get; set; }
        public decimal PerformanceScore { get; set; }
    }

    public class AllocationTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalAllocated { get; set; }
        public decimal TotalSpent { get; set; }
        public int AllocationCount { get; set; }
    }

    public class CategoryAnalysisDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal CategoryLimit { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalSpent { get; set; }
        public int DepartmentCount { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal RemainingLimit { get; set; }
    }

    public class RequestAnalyticsDto
    {
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int PendingRequests { get; set; }
        public decimal TotalAmountRequested { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public double ApprovalRate { get; set; }
        public decimal AverageRequestAmount { get; set; }
        public IEnumerable<DepartmentRequestSummaryDto> TopRequestingDepartments { get; set; } = new List<DepartmentRequestSummaryDto>();
    }

    public class DepartmentRequestSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public decimal TotalAmountRequested { get; set; }
        public int ApprovedCount { get; set; }
    }
}
