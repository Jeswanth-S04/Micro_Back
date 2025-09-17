using System.ComponentModel.DataAnnotations;

namespace BudgetManagementSystem.Api.DTOs
{
    /// <summary>
    /// DTO for creating a new allocation
    /// </summary>
    public class AllocationCreateDto
    {
        [Required(ErrorMessage = "Department ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive number")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Category ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(50, ErrorMessage = "Timeframe cannot exceed 50 characters")]
        public string Timeframe { get; set; } = "Monthly";
    }

    /// <summary>
    /// DTO for updating an existing allocation
    /// </summary>
    public class AllocationUpdateDto
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(50, ErrorMessage = "Timeframe cannot exceed 50 characters")]
        public string? Timeframe { get; set; } = "Monthly";
    }

    /// <summary>
    /// DTO for updating spent amount only
    /// </summary>
    public class UpdateSpentDto
    {
        [Required(ErrorMessage = "Spent amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Spent amount must be greater than or equal to 0")]
        public decimal SpentAmount { get; set; }
    }

    /// <summary>
    /// Response DTO for allocation data
    /// </summary>
    public class AllocationResponseDto
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Spent { get; set; }
        public decimal Balance => Amount - Spent;
        public decimal UtilizationPercentage => Amount > 0 ? Math.Round((Spent / Amount) * 100, 2) : 0;
        public bool IsOverBudget => Spent > Amount;
        public bool IsNearLimit => UtilizationPercentage >= 80 && !IsOverBudget;
        public string Timeframe { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status => IsOverBudget ? "Over Budget" : 
                               IsNearLimit ? "Near Limit" : 
                               "On Track";
    }

    /// <summary>
    /// DTO for allocation summary/statistics
    /// </summary>
    public class AllocationSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalCategories { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int CategoriesOverBudget { get; set; }
        public int CategoriesNearLimit { get; set; }
        public int CategoriesOnTrack { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<AllocationResponseDto> Categories { get; set; } = new();
    }

    /// <summary>
    /// DTO for bulk allocation updates
    /// </summary>
    public class BulkAllocationUpdateDto
    {
        public List<AllocationBulkItem> Allocations { get; set; } = new();
    }

    /// <summary>
    /// Individual allocation item for bulk operations
    /// </summary>
    public class AllocationBulkItem
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Allocation ID must be a positive number")]
        public int AllocationId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Spent amount must be greater than or equal to 0")]
        public decimal? Spent { get; set; }
    }

    /// <summary>
    /// DTO for allocation history/audit trail
    /// </summary>
    public class AllocationHistoryDto
    {
        public int Id { get; set; }
        public int AllocationId { get; set; }
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, SpentUpdated
        public decimal? PreviousAmount { get; set; }
        public decimal? NewAmount { get; set; }
        public decimal? PreviousSpent { get; set; }
        public decimal? NewSpent { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for allocation filters and search
    /// </summary>
    public class AllocationFilterDto
    {
        public int? DepartmentId { get; set; }
        public int? CategoryId { get; set; }
        public string? Timeframe { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool? IsOverBudget { get; set; }
        public bool? IsNearLimit { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? SortBy { get; set; } = "CreatedAt"; // CreatedAt, Amount, Spent, Department, Category
        public string? SortDirection { get; set; } = "desc"; // asc, desc
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Paginated response for allocations
    /// </summary>
    public class PaginatedAllocationResponseDto
    {
        public List<AllocationResponseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// DTO for copying allocations from one department to another
    /// </summary>
    public class CopyAllocationDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Source Department ID must be a positive number")]
        public int SourceDepartmentId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Target Department ID must be a positive number")]
        public int TargetDepartmentId { get; set; }

        public List<int>? CategoryIds { get; set; } // If null, copy all categories

        [Range(0.1, 2.0, ErrorMessage = "Amount multiplier must be between 0.1 and 2.0")]
        public decimal AmountMultiplier { get; set; } = 1.0m; // For scaling amounts up or down

        public bool CopySpentAmounts { get; set; } = false;
    }

    /// <summary>
    /// DTO for allocation comparison between departments
    /// </summary>
    public class AllocationComparisonDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<DepartmentAllocationCompare> Departments { get; set; } = new();
    }

    /// <summary>
    /// Department allocation data for comparison
    /// </summary>
    public class DepartmentAllocationCompare
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Spent { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for allocation threshold settings
    /// </summary>
    public class AllocationThresholdDto
    {
        public int AllocationId { get; set; }

        [Range(50, 95, ErrorMessage = "Warning threshold must be between 50% and 95%")]
        public decimal WarningThresholdPercent { get; set; } = 80;

        [Range(95, 100, ErrorMessage = "Critical threshold must be between 95% and 100%")]
        public decimal CriticalThresholdPercent { get; set; } = 95;

        public bool EnableEmailAlerts { get; set; } = true;
        public bool EnableSystemNotifications { get; set; } = true;
    }

    /// <summary>
    /// DTO for allocation export
    /// </summary>
    public class AllocationExportDto
    {
        public int? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ExportFormat { get; set; } = "Excel"; // Excel, CSV, PDF
        public bool IncludeSpentDetails { get; set; } = true;
        public bool IncludeHistory { get; set; } = false;
    }
}
