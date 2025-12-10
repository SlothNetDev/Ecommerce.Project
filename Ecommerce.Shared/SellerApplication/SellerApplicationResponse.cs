using Ecommerce.Shared.Enums;

namespace Ecommerce.Shared.SellerApplication;

public record SellerApplicationResponse()
{
    public Guid SellerId { get; set; } 
    public string BusinessName { get; set; } = string.Empty;
 
    public string ApplicationReason{get;set;} = string.Empty;
    public string? Status { get; set; }
    public DateTime SubmittedAt { get; set; }
};