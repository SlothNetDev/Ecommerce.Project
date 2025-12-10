
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Identity.Entities;

public class RoleChangeHistory
{
    public Guid ChangeHistoryId { get; set; } =  Guid.NewGuid();

    // The user whose role was changed.
    public Guid UserId  { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public Guid? OldRoleId { get; set; }
    public IdentityRole? OldRole { get; set; }
    
    public Guid? NewRoleId { get; set; }
    public IdentityRole? NewRole { get; set; }

    //admin who change it
    public Guid? ChangeBy { get; set; } 
    public ApplicationUser? ChangeByUser { get; set; } = null!;

    public DateTime ChangeAt { get; set; } =  DateTime.Now;

    public string? Reason { get; set; }
}