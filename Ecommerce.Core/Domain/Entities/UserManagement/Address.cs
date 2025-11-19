using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Entities.UserManagement
{
    public class Address
    {
         public Guid Id { get; set; }
         public Guid UserId { get; set; }
         public string Street { get; set; } = string.Empty;
         public string City { get; set; } = string.Empty;
         public string State { get; set; } = string.Empty;
         public string Country { get; set; } = string.Empty;
         public string ZipCode { get; set; } = string.Empty;
         public bool IsPrimary { get; set; }
         public string AddressType { get; set; }
    }
}
