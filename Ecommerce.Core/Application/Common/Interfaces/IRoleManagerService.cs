using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IRoleManagementService  
{
    /// <summary>
    /// Crete a way to change the User Role
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="newRole"></param>
    /// <param name="changedBy"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    Task<ResponseType<bool>> ChangeUserRole(Guid userId, string newRole, Guid changedBy, string reason);
    
    /// <summary>
    /// Get the User Roles 
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<ResponseType<List<string>>> GetUserRoles(Guid userId);
}