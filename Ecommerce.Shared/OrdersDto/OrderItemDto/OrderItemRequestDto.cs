using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.OrdersDto.OrderItemDto
{
    public record OrderItemRequestDto
    {
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}
