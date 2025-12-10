using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IRoleManagementService  
{
    Task<ResponseType<bool>> ChangeUserRole(Guid userId, string newRole, Guid changedBy, string reason);
    Task<ResponseType<List<string>>> GetUserRoles(Guid userId);
}