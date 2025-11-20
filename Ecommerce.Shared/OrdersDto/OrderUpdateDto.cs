using Ecommerce.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.OrdersDto
{
    public class OrderUpdateDto
    {
        public Guid Id { get; init; }
        public Status? Status { get; init; } // Pending, Confirmed, Shipped, Delivered, Cancelled
        public decimal? TotalAmount { get; init; }
    }
}
