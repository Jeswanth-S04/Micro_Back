using System.ComponentModel.DataAnnotations;

namespace BudgetManagementSystem.Api.Models
{
    public class BudgetCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Limit { get; set; }

        [Required, MaxLength(40)]
        public string Timeframe { get; set; } = "Monthly"; // e.g., Monthly, Quarterly, Yearly

        [Range(0, 100)]
        public int ThresholdPercent { get; set; } = 80;

        public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    }
}