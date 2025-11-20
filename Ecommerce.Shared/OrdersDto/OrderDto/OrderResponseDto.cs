using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.OrdersDto.OrderDto
{
    internal record OrderResponseDto
    {
         public Guid Id { get; init; }
         public string Status { get; init; }
         public decimal TotalAmount { get; init; }
         public DateTime CreatedAt { get; init; } 
    }
}
