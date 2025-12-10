namespace Ecommerce.Shared;

public record SellerApplicationDto()
{
    public string BusinessName { get; init; } = string.Empty;
 
    public string ApplicationReason{get;init;} = string.Empty;
};