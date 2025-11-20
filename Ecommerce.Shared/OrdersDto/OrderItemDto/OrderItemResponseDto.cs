using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.OrdersDto.OrderItemDto
{
    public record OrderItemResponseDto
    {
        public Guid Id { get; init; }
        public int Quantity { get; init; }
        public string Status { get; set; }
        public decimal UnitPrice { get; init; }
    }
}
