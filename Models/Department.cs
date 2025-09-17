using System.ComponentModel.DataAnnotations;

namespace BudgetManagementSystem.Api.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    }
}