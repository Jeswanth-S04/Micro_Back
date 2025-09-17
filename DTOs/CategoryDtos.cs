using System.ComponentModel.DataAnnotations;

namespace BudgetManagementSystem.Api.DTOs
{
    public class CategoryCreateDto
    {
        [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
        [Range(0, double.MaxValue)] public decimal Limit { get; set; }
        [Required, MaxLength(40)] public string Timeframe { get; set; } = "Monthly";
        [Range(0, 100)] public int ThresholdPercent { get; set; } = 80;
    }

    public class CategoryUpdateDto : CategoryCreateDto
    {
        [Required] public int Id { get; set; }
    }

    public class CategoryResponseDto : CategoryUpdateDto {}
}
