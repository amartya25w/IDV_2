using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserAuthAPI.Data;
using UserAuthAPI.DTOs;

namespace UserAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all users (Admin functionality)
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        CreatedAt = u.CreatedAt
                    })
                    .OrderBy(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<UserDto>>.SuccessResponse(users, "Users retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<UserDto>>.ErrorResponse($"Failed to retrieve users: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        CreatedAt = u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
                }

                return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse($"Failed to retrieve user: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        /// <param name="request">Updated user information</param>
        /// <returns>Updated user details</returns>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 401)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Validation failed", errors));
                }

                // Get user ID from JWT token
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
                }

                // Check if email is already taken by another user
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != userId);

                    if (existingUser != null)
                    {
                        return BadRequest(ApiResponse<UserDto>.ErrorResponse("Email is already taken"));
                    }

                    user.Email = request.Email.ToLower().Trim();
                }

                // Update user information
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName.Trim();

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName.Trim();

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                };

                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse($"Failed to update profile: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete current user account (soft delete)
        /// </summary>
        /// <returns>Success message</returns>
        [HttpDelete("profile")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 401)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<ActionResult<ApiResponse<string>>> DeleteAccount()
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<string>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<string>.ErrorResponse("User not found"));
                }

                // Soft delete - just mark as inactive
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                // Revoke all refresh tokens
                var refreshTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.SuccessResponse("Success", "Account deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse($"Failed to delete account: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get user statistics (for admin or analytics)
        /// </summary>
        /// <returns>User statistics</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse<object>>> GetUserStats()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync(u => u.IsActive);
                var totalInactiveUsers = await _context.Users.CountAsync(u => !u.IsActive);
                var usersThisMonth = await _context.Users
                    .CountAsync(u => u.CreatedAt.Month == DateTime.UtcNow.Month &&
                                   u.CreatedAt.Year == DateTime.UtcNow.Year);

                var stats = new
                {
                    TotalActiveUsers = totalUsers,
                    TotalInactiveUsers = totalInactiveUsers,
                    UsersCreatedThisMonth = usersThisMonth,
                    TotalUsers = totalUsers + totalInactiveUsers
                };

                return Ok(ApiResponse<object>.SuccessResponse(stats, "User statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Failed to retrieve statistics: {ex.Message}"));
            }
        }
    }
}