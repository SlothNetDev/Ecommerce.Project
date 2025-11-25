using Ecommerce.Core.Domain.Entities.UserManagement;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Identity.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public DateTime? AccountCreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AccountUpdatedAt { get; set; }
        public DateTime? AccountDeletedAt { get; set; }
        
        
        //link to domain user entities "User"
        public Guid DomainUserId { get; set; }
        public User DomainUser { get; set; } = null!;

        //Add refresh token to track all active/Inactive refresh tokens
        public ICollection<ApplicationToken> RefreshTokens { get; set; } = new List<ApplicationToken>();
    }
}
