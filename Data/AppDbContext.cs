using BudgetManagementSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManagementSystem.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users => Set<User>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
        public DbSet<Allocation> Allocations => Set<Allocation>();
        public DbSet<AdjustmentRequest> AdjustmentRequests => Set<AdjustmentRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name).IsUnique();

            modelBuilder.Entity<BudgetCategory>()
                .HasIndex(c => new { c.Name, c.Timeframe }).IsUnique();

            modelBuilder.Entity<Allocation>()
                .HasOne(a => a.Department)
                .WithMany(d => d.Allocations)
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Allocation>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Allocations)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdjustmentRequest>()
                .HasOne(r => r.Department)
                .WithMany()
                .HasForeignKey(r => r.DepartmentId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
