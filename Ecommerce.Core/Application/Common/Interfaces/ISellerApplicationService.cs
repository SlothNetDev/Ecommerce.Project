using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface ISellerApplicationService
{
    Task<ResponseType<SellerApplicationDto>> SubmitApplication(SubmitApplicationRequest request);
    Task<ResponseType<List<SellerApplicationDto>>> GetPendingApplications();
    Task<ResponseType<bool>> ApproveApplication(Guid applicationId, string adminComments, Guid adminId);
    Task<ResponseType<bool>> RejectApplication(Guid applicationId, string adminComments, Guid adminId);
}