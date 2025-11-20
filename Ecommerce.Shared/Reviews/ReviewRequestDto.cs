using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.Reviews
{
    public record ReviewRequestDto
    {
         public Guid Id { get; init; }
         public int Rating { get; init; } // 1-5
         public string Comment { get; init; } = string.Empty;
    }
}
