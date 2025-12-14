using Ecommerce.Shared;
using Ecommerce.Shared.SellerApplication;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface ISellerApplicationService
{
    Task<ResponseType<SellerApplicationResponse>> SubmitApplication(SubmitApplicationRequest request);
    Task<ResponseType<List<SellerApplicationResponse>>> GetPendingApplications();
    Task<ResponseType<bool>> ApproveApplication(AdminActionCommand request);
    Task<ResponseType<bool>> RejectApplication(AdminActionCommand request);
}