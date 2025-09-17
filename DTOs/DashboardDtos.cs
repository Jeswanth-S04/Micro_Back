namespace BudgetManagementSystem.Api.DTOs
{
    public class DepartmentSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalAllocation { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal Balance => TotalAllocation - TotalSpent;
        public IEnumerable<CategoryBreakdownDto> Categories { get; set; } = new List<CategoryBreakdownDto>();
    }

    public class CategoryBreakdownDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Allocation { get; set; }
        public decimal Spent { get; set; }
        public decimal Balance => Allocation - Spent;
        public int ThresholdPercent { get; set; }
        public bool NearingLimit { get; set; }
        public bool Exceeded { get; set; }
    }

    public class ManagementSummaryDto
    {
        public IEnumerable<DepartmentSummaryDto> Departments { get; set; } = new List<DepartmentSummaryDto>();
        public decimal GrandTotalAllocation { get; set; }
        public decimal GrandTotalSpent { get; set; }
        public decimal GrandBalance => GrandTotalAllocation - GrandTotalSpent;
    }
}
