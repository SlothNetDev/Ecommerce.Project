using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.OrdersDto.OrderItemDto
{
    public record StatusUpdateBySellerDto
    {
        public Guid OrderItemId { get; init; } // Unique identifier for the order item
        public string Status { get; init; } = null!; // e.g., Shipped, Delivered
    }
}
