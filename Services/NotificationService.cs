using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace BudgetManagementSystem.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public NotificationService(IConfiguration config, AppDbContext db)
        {
            _config = config;
            _db = db;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_config["Smtp:FromName"], _config["Smtp:FromEmail"]));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var body = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = body.ToMessageBody();

                using var client = new SmtpClient();
                var host = _config["Smtp:Host"];
                var port = int.Parse(_config["Smtp:Port"] ?? "587");
                var useStartTls = bool.Parse(_config["Smtp:UseStartTls"] ?? "true");

                await client.ConnectAsync(host, port, useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                await client.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent breaking the main operation
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

        public async Task NotifyUserAsync(int userId, string title, string message)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            _db.Notifications.Add(new Models.Notification
            {
                UserId = userId,
                Title = title,
                Message = message
            });
            await _db.SaveChangesAsync();

            try
            {
                await SendEmailAsync(user.Email, title, $"<p>{message}</p>");
            }
            catch 
            { 
                // Ignore email failure - notification is still saved to database
            }
        }

        public async Task NotifyRoleAsync(string role, string title, string message)
        {
            // FIX: Convert string role to enum for proper comparison
            UserRole roleEnum;
            if (!Enum.TryParse<UserRole>(role, out roleEnum))
            {
                return; // Invalid role, skip notification
            }

            // FIX: Compare enum values directly, not string conversion
            var users = await _db.Users.Where(u => u.Role == roleEnum).ToListAsync();
            
            foreach (var user in users)
            {
                await NotifyUserAsync(user.Id, title, message);
            }
        }

        public async Task SendThresholdAlertAsync(int departmentId, int categoryId, string details)
        {
            // FIX: Use enum directly instead of string comparison
            var depHeads = await _db.Users
                .Where(u => u.DepartmentId == departmentId && u.Role == UserRole.DepartmentHead)
                .ToListAsync();
            
            foreach (var head in depHeads)
            {
                await NotifyUserAsync(head.Id, "Budget threshold alert", details);
            }

            var admins = await _db.Users
                .Where(u => u.Role == UserRole.FinanceAdmin)
                .ToListAsync();
            
            foreach (var admin in admins)
            {
                await NotifyUserAsync(admin.Id, "Budget threshold alert ( new Admin)", details);
            }
        }
    }
}


