using Ecommerce.Shared.Enums;

namespace Ecommerce.Shared.SellerApplication;

public record SellerApplicationResponse()
{
    public Guid SellerId { get; init; } 
    public string BusinessName { get; init; } = string.Empty;
 
    public string ApplicationReason{get;init;} = string.Empty;
    public string? Status { get; init; }
    public DateTime SubmittedAt { get; init; }
};