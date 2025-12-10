namespace Ecommerce.Shared.SellerApplication;

public record SubmitApplicationRequest()
{
    public string BusinessName { get; init; } = string.Empty;
 
    public string ApplicationReason{get;init;} = string.Empty;
};