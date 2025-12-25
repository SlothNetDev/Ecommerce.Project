using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Common.Interfaces.Security;
using Ecommerce.Core.Domain.Utilities;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Security;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services.Notification;

/// <summary>
/// Service for generating, storing, and validating One-Time Passwords (OTP).
/// Uses in-memory storage for development; can be easily extended to use Redis or database.
/// </summary>
public class OtpService(ILogger<OtpService> logger,
    ApplicationDbContext dbContext,
    IHashService hashService) : IOtpService
{
    private const int MaxAttemp = 5;
    private static readonly DateTime ResendCooldown = DateTime.UtcNow.AddSeconds(60);
    private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(5); //expire after 5 minutes
    public async Task<ResponseType<string>> CreateEmailOtpAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            logger.LogWarning("User Id Cannot be empty");
            return ResponseType<string>.Fail("User Id is Empty",
                FailureType.Validation);
        }

        var generateOtp = OtpGenerator.GenerateOtp();
        var hashOtp = hashService.Hash(generateOtp); //hashing the generated Otp

        var entity = new EmailOtp()
        {
            UserId = userId,
            CodeHash = hashOtp,
            ExpiresAt = DateTime.UtcNow.Add(OtpExpiration),
        };
        
        await dbContext.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Otp Generated Successfully for user: {UserId}", userId);
        
        return ResponseType<string>.SuccessResult(generateOtp, "Otp Generated Successfully");
    }

    public async Task<OtpValidationResult> VerifyEmailOtpAsync(Guid userId, string otpCode)
    {
        if (userId == Guid.Empty)
        {
            logger.LogWarning("User Id Cannot be empty");
            return OtpValidationResult.Invalid;
        }

        if (string.IsNullOrWhiteSpace(otpCode))
        {
            logger.LogWarning("Otp Code Cannot be empty");
            return OtpValidationResult.Invalid;
        }

        var otp = await dbContext.EmailOtpDb
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        
        if(otp is null)
            return OtpValidationResult.NotFound;

        if (otp.IsExpired || !otp.IsActive)
        {
            dbContext.Remove(otp);
            await dbContext.SaveChangesAsync();
            return OtpValidationResult.Expired;
        }
            

        if (otp.Attempts >= MaxAttemp)
            return OtpValidationResult.TooManyAttempts;
        
        otp.Attempts++; // increment otp Attemps per verification

        if (!hashService.Verify(otpCode, otp.CodeHash)) //check if otp is has
        {
            if(otp.IsLocked) //check if it's lock
                dbContext.Remove(otp);
            
            await dbContext.SaveChangesAsync();
            return OtpValidationResult.Invalid;
        }
        otp.VerifiedAt = DateTime.UtcNow;
        return OtpValidationResult.Valid;
    }

    public async Task<ResponseType<bool>> HasValidOtpAsync(Guid userId)
    {
        var response =  await dbContext.EmailOtpDb
            .AnyAsync(x => x.UserId == userId &&
                           x.VerifiedAt == null &&
                           x.IsExpired);
        return ResponseType<bool>.SuccessResult(response, "Otp Valid");
    }

    public async Task<ResponseType<string>> ResendOtpAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            logger.LogWarning("User Id Cannot be empty");
            return ResponseType<string>.Fail("User Id is Empty",
                FailureType.Validation);
        }
        
        var exisingOtp = await dbContext.EmailOtpDb
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        
        //cooldown
        if (exisingOtp is null && exisingOtp?.LastSentAt < ResendCooldown)
        {
            dbContext.Remove(exisingOtp);
            await dbContext.SaveChangesAsync();
            
            logger.LogWarning("Existing otp is null");
            return ResponseType<string>.Fail("Please wait for 60 second for you to resend otp",
                FailureType.Expired);
        } 
        
        //reset limit
        var generateOtp = OtpGenerator.GenerateOtp();
        var hashOtp = hashService.Hash(generateOtp); //hashing the generated Otp
        
        var entity = new EmailOtp()
        {
            UserId = userId,
            CodeHash = hashOtp,
            ExpiresAt = DateTime.UtcNow.Add(OtpExpiration),
        };
        
        await dbContext.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Otp Generated Successfully for user: {UserId}", userId);
        
        return ResponseType<string>.SuccessResult(generateOtp, "Otp generated again Successfully");
        
    }
}
