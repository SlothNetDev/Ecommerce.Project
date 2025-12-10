using Ecommerce.Shared;
using Ecommerce.Shared.SellerApplication;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface ISellerApplicationService
{
    Task<ResponseType<SellerApplicationResponse>> SubmitApplication(SubmitApplicationRequest request);
    Task<ResponseType<List<SellerApplicationResponse>>> GetPendingApplications();
    Task<ResponseType<bool>> ApproveApplication(Guid applicationId, string adminComments, Guid adminId);
    Task<ResponseType<bool>> RejectApplication(Guid applicationId, string adminComments, Guid adminId);
}