namespace BudgetManagementSystem.Api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty; // e.g., "AllocationCreated", "RequestApproved"
        public string Details { get; set; } = string.Empty;
    }
}
