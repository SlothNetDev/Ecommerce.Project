using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

public class RoleManagerService(
    UserManager<ApplicationUser> userManager,
    ILogger<RoleManagerService> logger,
    RoleManager<ApplicationRole> userRoles)  : IRoleManagementService
{
    public async Task<ResponseType<bool>> ChangeUserRole(Guid userId, string newRole, Guid changedBy, string reason)
    {
         //1. Validate input
        if (userId == Guid.Empty)
        {
            return ResponseType<bool>.Fail("Invalid user id");
        }

        if (string.IsNullOrWhiteSpace(newRole))
        {
            return ResponseType<bool>.Fail("Role name is required");
        }

        newRole = newRole.Trim();

        //2. Look for user by id
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            logger.LogError("User with id {UserId} not found", userId);
            return ResponseType<bool>.Fail("User not found");
        }

        //3. Ensure the target role exists
        var roleExists = await userRoles.RoleExistsAsync(newRole);
        if (!roleExists)
        {
            logger.LogWarning("Role {Role} does not exist. UserId: {UserId}", newRole, userId);
            return ResponseType<bool>.Fail($"Role '{newRole}' does not exist");
        }

        //4. Get current roles (async all the way; no .Result)
        var currentRoles = await userManager.GetRolesAsync(user);

        //5. If the user already has the requested role, do nothing
        if (currentRoles.Any(r => string.Equals(r, newRole, StringComparison.OrdinalIgnoreCase)))
        {
            return ResponseType<bool>.SuccessResult(true, "User already has the requested role");
        }

        //6. Remove current roles (if your business rules allow only ONE role at a time)
        if (currentRoles.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                logger.LogError("Failed removing roles from user {UserId}. Errors: {Errors}", userId, errors);
                return ResponseType<bool>.Fail("Failed to remove existing roles");
            }
        }

        //7. Add the new role
        var addResult = await userManager.AddToRoleAsync(user, newRole);
        if (!addResult.Succeeded)
        {
            var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
            logger.LogError("Failed adding role {Role} to user {UserId}. Errors: {Errors}", newRole, userId, errors);
            return ResponseType<bool>.Fail("Failed to add new role");
        }

        //8. (Optional) Audit / persist change reason (depends on your schema)
        // NOTE: Parameters changedBy/reason are currently not persisted anywhere.
        // If you have an Audit table, save an entry here using dbContext and call SaveChangesAsync().
        // await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "User role changed. UserId: {UserId}, NewRole: {NewRole}, ChangedBy: {ChangedBy}, Reason: {Reason}",
            userId, newRole, changedBy, reason);

        return ResponseType<bool>.SuccessResult(true, "Role updated successfully");
    }

    public async Task<ResponseType<List<string>>> GetUserRoles(Guid userId)
    {
        //1. Validate user id
        if (userId == Guid.Empty)
        {                                               
            logger.LogError("Invalid user id: {UserId}", userId);
            return ResponseType<List<string>>.Fail("Invalid user id");
        }
        
        //2. look for user Id
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            logger.LogError("User with id {UserId} not found", userId);
            return ResponseType<List<string>>.Fail("User not found");
        }
        
        //3. Return roles
        var roles = await userManager.GetRolesAsync(user);

        return ResponseType<List<string>>.SuccessResult(roles.ToList(),
            "Roles retrieved successfully");
    }
}