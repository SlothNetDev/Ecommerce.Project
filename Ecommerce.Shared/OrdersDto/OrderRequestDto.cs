
using Ecommerce.Shared.Entities;

namespace Ecommerce.Shared.OrdersDto
{
    public record OrderRequestDto
    {
         public Guid Id { get; init; }
         public Status? Status { get; init; } // Pending, Confirmed, Shipped, Delivered, Cancelled
         public decimal TotalAmount { get; init; }
    }
}
