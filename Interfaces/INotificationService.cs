namespace BudgetManagementSystem.Api.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
        Task NotifyUserAsync(int userId, string title, string message);
        Task NotifyRoleAsync(string role, string title, string message);
        Task SendThresholdAlertAsync(int departmentId, int categoryId, string details);
    }
}