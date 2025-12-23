using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.SellerApplication;
using Ecommerce.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services;

public class SellerApplicationService(
    ApplicationDbContext dbContext,
    ILogger<SellerApplicationService> logger,
    IRoleManagementService roleManagementService
    ) : ISellerApplicationService
{
    public async Task<ResponseType<SellerApplicationResponse>> SubmitApplication(SubmitApplicationRequest request)
    {
        //1. check if application has pending application or approved application
        var existingApplication = await dbContext.SellerApplicationDb
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SellerId == request.SellerId &&
                                      x.Status == nameof(ApplicationStatus.Pending) &&
                                      x.Status == nameof(ApplicationStatus.Approved));
        
        //this should be null if no pending or approved application
        if (existingApplication is not null)
        {
            if (existingApplication.Status == nameof(ApplicationStatus.Pending))
            {
                logger.LogWarning("User {UserId} already has pending application. Skipping application submission.", request.SellerId);
                return ResponseType<SellerApplicationResponse>.Fail("User already has pending application",
                    FailureType.Conflict);
            }

            if (existingApplication.Status == nameof(ApplicationStatus.Approved))
            {
                logger.LogWarning("User {UserId} already has approved application. Skipping application submission.", request.SellerId);
                return ResponseType<SellerApplicationResponse>.Fail("User already has approved application",
                    FailureType.Conflict);
            }
        }
        
        //2. map the applicationSeller if application was not exist
        var newApplication = new SellerApplication()
        {
            SellerId = request.SellerId,
            BusinessName = request.BusinessName,
            ApplicationReason = request.ApplicationReason,

            Status = nameof(ApplicationStatus.Pending),
            SubmittedAt = DateTime.UtcNow
        };
        
        //4. save to db
        await dbContext.SellerApplicationDb.AddAsync(newApplication);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("New application submitted for user: {UserId}", request.SellerId);
        
        //map response dto
        var response = new SellerApplicationResponse()
        {
            SellerId = newApplication.SellerId,
            BusinessName = newApplication.BusinessName,
            ApplicationReason = newApplication.ApplicationReason,
            Status = newApplication.Status,
            SubmittedAt = newApplication.SubmittedAt

        };
        return ResponseType<SellerApplicationResponse>.SuccessResult(response, "Application submitted successfully");
    }

    public async Task<ResponseType<List<SellerApplicationResponse>>> GetPendingApplications()
    {
        //1. Load pending application
        var pending = await dbContext.SellerApplicationDb
            .AsNoTracking()
            .Where(x => x.Status == nameof(ApplicationStatus.Pending))
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new SellerApplicationResponse()
            {
                SellerId = x.SellerId,
                BusinessName = x.BusinessName,
                ApplicationReason = x.ApplicationReason,
                SubmittedAt = x.SubmittedAt
            })
            .ToListAsync();
        
        return ResponseType<List<SellerApplicationResponse>>.SuccessResult(pending, "Pending applications retrieved");

    }

    public async Task<ResponseType<bool>> ApproveApplication(AdminActionCommand request)
    {
        
        //2. load application using sellerId as Pk 
        var application = await dbContext.SellerApplicationDb
            .FirstOrDefaultAsync(x => x.SellerId == request.ApplicationId);
        
        if(application is null)
        {
            logger.LogError("Application not found for sellerId: {SellerId}", request.ApplicationId);
            return ResponseType<bool>.Fail("Application not found",
                FailureType.NotFound);
        }
        
        //3. ensure the application was pending before approval
        if(application.Status != nameof(ApplicationStatus.Pending))
        {
            logger.LogError("Application status is not pending: {Status}", application.Status);
            return ResponseType<bool>.Fail("Application status is not pending",
                FailureType.Conflict);
        }
        
        //4. approve the application of admin
        application.Status = nameof(ApplicationStatus.Approved); // approved
        application.ReviewAt = DateTime.UtcNow;
        application.ReviewBy = request.AdminId;
        application.ReviewComments = string.IsNullOrWhiteSpace(request.AdminComments) ? null : request.AdminComments.Trim();
        
        //save to db
        await dbContext.SaveChangesAsync();
        
        
        //5. check user's role current role using GetUserRoleAsync method
        var currentRoleResult = await roleManagementService.GetUserRoles(application.SellerId);
        if (currentRoleResult.Success)
        {
            var currentRoles = currentRoleResult.Data;
            
            // Check if the user already has the target role
            if (currentRoles!.Contains(RoleSeeder.Seller))
            {
                logger.LogInformation("User {UserId} already has the {Role} role. Skipping role assignment.", application.SellerId, RoleSeeder.Seller);
        
                // Path 2: Role exists -> Set flag to false and we can return success immediately
                return ResponseType<bool>.SuccessResult(true, "Application approved successfully. User role was already Seller.");
            }
        }
        else
        {
            // Path 1: Failed to retrieve roles -> Log warning, keep shouldChangeRole = true 
            logger.LogWarning("Failed to retrieve current roles for user {UserId}. Proceeding with role change attempt.", application.SellerId);
            logger.LogWarning($"> {currentRoleResult.Message} <");
        }
        var roleResult = await roleManagementService
            .ChangeUserRole(
                userId: application.SellerId,
                newRole: RoleSeeder.Seller,
                changedBy: request.AdminId,
                reason: "Seller Application Approved by admin"
                );

        if (!roleResult.Success)
        {
            logger.LogError("Failed to assign role {Role} to user {UserId}", RoleSeeder.Seller, application.SellerId);
            return ResponseType<bool>.Fail("Application approved, but failed to update user role",
                FailureType.Internal);
        }
        return ResponseType<bool>.SuccessResult(true, "Application approved successfully");
    }

    public async Task<ResponseType<bool>> RejectApplication(AdminActionCommand request)
    {
        //2. load application using sellerId as Pk 
        var application = await dbContext.SellerApplicationDb
            .FirstOrDefaultAsync(x => x.SellerId == request.ApplicationId);
        
        if(application is null)
        {
            logger.LogError("Application not found for sellerId: {SellerId}", request.ApplicationId);
            return ResponseType<bool>.Fail("Application not found",
                FailureType.NotFound);
        }
        
        //3. ensure the application was pending before approval
        if(application.Status != nameof(ApplicationStatus.Pending))
        {
            logger.LogError("Application status is not pending: {Status}", application.Status);
            return ResponseType<bool>.Fail("Application status is not pending",
                FailureType.Conflict);
        }
        
        //4. rejected the application of admin
        application.Status = nameof(ApplicationStatus.Rejected); // rejected
        application.ReviewAt = DateTime.UtcNow;
        application.ReviewBy = request.AdminId;
        application.ReviewComments = string.IsNullOrWhiteSpace(request.AdminComments) ? null : request.AdminComments.Trim();
        
        //save to db
        await dbContext.SaveChangesAsync();
        
        var roleResult = await roleManagementService
            .ChangeUserRole(
                userId: application.SellerId,
                newRole: RoleSeeder.Customer,
                changedBy: request.AdminId,
                reason: "Seller Application Rejected by admin"
            );

        if (!roleResult.Success)
        {
            logger.LogError("Failed to assign role {Role} to user {UserId}", RoleSeeder.Seller, application.SellerId);
            return ResponseType<bool>.Fail("Application Rejected, but failed to update user role",
                FailureType.Internal);
        }
        return ResponseType<bool>.SuccessResult(true, "Application Rejected successfully");
    }
    
}