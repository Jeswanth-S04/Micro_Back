using Microsoft.EntityFrameworkCore;
using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace BudgetManagementSystem.Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Hash password using SHA256 with salt
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + "YourSecretSalt2024!";
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Department)
                    .OrderBy(u => u.Name)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Role = (int)u.Role,
                        DepartmentId = u.DepartmentId,
                        DepartmentName = u.Department != null ? u.Department.Name : null
                    })
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<UserResponseDto?> GetByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.Id == id)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Role = (int)u.Role,
                        DepartmentId = u.DepartmentId,
                        DepartmentName = u.Department != null ? u.Department.Name : null
                    })
                    .FirstOrDefaultAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                throw;
            }
        }

        public async Task<UserResponseDto> CreateAsync(UserCreateDto dto, int createdByUserId)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .Where(u => u.Email.ToLower() == dto.Email.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    throw new InvalidOperationException($"A user with email '{dto.Email}' already exists");
                }

                // Validate department if provided
                if (dto.DepartmentId.HasValue)
                {
                    var departmentExists = await _context.Departments
                        .AnyAsync(d => d.Id == dto.DepartmentId.Value);
                    
                    if (!departmentExists)
                    {
                        throw new InvalidOperationException($"Department with ID {dto.DepartmentId} not found");
                    }
                }

                // Validate role is valid (1, 2, or 3)
                if (dto.Role < 1 || dto.Role > 3)
                {
                    throw new InvalidOperationException("Role must be 1 (Finance Admin), 2 (Department Head), or 3 (Management)");
                }

                // Hash password
                var hashedPassword = HashPassword(dto.Password);

                var user = new User
                {
                    Name = dto.Name.Trim(),
                    Email = dto.Email.Trim().ToLower(),
                    PasswordHash = hashedPassword,
                    Role = (Enums.UserRole)dto.Role,
                    DepartmentId = dto.DepartmentId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Reload with department for response
                var createdUser = await GetByIdAsync(user.Id);

                _logger.LogInformation("User created: ID={UserId}, Name={UserName}, Email={UserEmail}, Role={Role}, DepartmentId={DepartmentId} by user {CreatedByUserId}", 
                    user.Id, user.Name, user.Email, user.Role, user.DepartmentId, createdByUserId);

                return createdUser!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {UserEmail}", dto.Email);
                throw;
            }
        }

        public async Task<UserResponseDto> UpdateAsync(UserUpdateDto dto, int updatedByUserId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == dto.Id)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {dto.Id} not found");
                }

                // Check if new email already exists (excluding current user)
                var existingUser = await _context.Users
                    .Where(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != dto.Id)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    throw new InvalidOperationException($"A user with email '{dto.Email}' already exists");
                }

                // Validate department if provided
                if (dto.DepartmentId.HasValue)
                {
                    var departmentExists = await _context.Departments
                        .AnyAsync(d => d.Id == dto.DepartmentId.Value);
                    
                    if (!departmentExists)
                    {
                        throw new InvalidOperationException($"Department with ID {dto.DepartmentId} not found");
                    }
                }

                // Validate role is valid (1, 2, or 3)
                if (dto.Role < 1 || dto.Role > 3)
                {
                    throw new InvalidOperationException("Role must be 1 (Finance Admin), 2 (Department Head), or 3 (Management)");
                }

                // Update properties
                user.Name = dto.Name.Trim();
                user.Email = dto.Email.Trim().ToLower();
                user.Role = (Enums.UserRole)dto.Role;
                user.DepartmentId = dto.DepartmentId;

                // Update password if provided
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    user.PasswordHash = HashPassword(dto.Password);
                }

                await _context.SaveChangesAsync();

                // Reload with department for response
                var updatedUser = await GetByIdAsync(user.Id);

                _logger.LogInformation("User updated: ID={UserId}, Name={UserName}, Email={UserEmail}, Role={Role}, DepartmentId={DepartmentId} by user {UpdatedByUserId}", 
                    user.Id, user.Name, user.Email, user.Role, user.DepartmentId, updatedByUserId);

                return updatedUser!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", dto.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id, int deletedByUserId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {id} not found");
                }

                // Check if user has any budget requests (if AdjustmentRequests table exists)
                bool hasRelatedData = false;
                try
                {
                    hasRelatedData = await _context.AdjustmentRequests
                        .AnyAsync(r => r.ReviewedByUserId == id);
                }
                catch
                {
                    // If AdjustmentRequests table doesn't exist, proceed with delete
                }

                if (hasRelatedData)
                {
                    throw new InvalidOperationException("Cannot delete user with existing budget requests. Please contact system administrator.");
                }

                // Hard delete since no IsActive field
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User deleted: ID={UserId}, Name={UserName} by user {DeletedByUserId}", 
                    user.Id, user.Name, deletedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                throw;
            }
        }

        public Task<bool> ActivateUserAsync(int id, int activatedByUserId)
        {
            throw new NotImplementedException();
        }
    }
}
