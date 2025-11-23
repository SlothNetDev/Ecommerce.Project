using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Identity.Entities
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        //provide roles
        public string Description { get; set; } = string.Empty;

        //used for time stamp
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
