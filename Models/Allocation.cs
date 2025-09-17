using System.ComponentModel.DataAnnotations;

namespace BudgetManagementSystem.Api.Models
{
    public class Allocation
    {
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        [Required]
        public int CategoryId { get; set; }
        public BudgetCategory Category { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Spent { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Timeframe => Category.Timeframe;
    }
}
