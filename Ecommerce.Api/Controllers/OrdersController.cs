using Ecommerce.Core.Domain.Entities.Orders;
using Ecommerce.Core.Domain.Entities.UserManagement;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Shared.OrdersDto.OrderDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
public class OrdersController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto request)
    {
        // UserId is required by the entity but not in DTO. 
        // We need a valid User (Domain Entity) to link the order to.
        var user = await dbContext.Set<User>().FirstOrDefaultAsync();
        if (user == null)
        {
            user = new User
            {
                UserId = Guid.NewGuid(),
                IsActive = true
            };
            dbContext.Set<User>().Add(user);
            await dbContext.SaveChangesAsync();
        }

        var order = new Order
        {
            Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
            Status = request.Status?.ToString() ?? "Pending",
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow,
            UserId = user.UserId
        };

        dbContext.OrderDb.Add(order);
        await dbContext.SaveChangesAsync();

        var response = new OrderResponseDto
        {
            Id = order.Id,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        };

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await dbContext.OrderDb.FindAsync(id);
        if (order == null) return NotFound();

        var response = new OrderResponseDto
        {
            Id = order.Id,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        };

        return Ok(response);
    }
}