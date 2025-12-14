namespace Ecommerce.Shared.SellerApplication;

public record AdminActionCommand(
    Guid ApplicationId,
    string AdminComments,
    Guid AdminId);