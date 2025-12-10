using Ecommerce.Shared.Enums;

namespace Ecommerce.Infrastructure.Identity.Entities;

public class SellerApplication
{
    public Guid SellerId { get; set; } = Guid.NewGuid();

    public string BusinessName { get; set; } = string.Empty;
    
    public string ApplicationReason{get;set;} = string.Empty;

    public string Status { get; set; } = nameof(ApplicationStatus.Pending);

    public DateTime SubmittedAt { get; set; } =  DateTime.UtcNow;

    public DateTime? ReviewAt { get; set; } //when it reviews and approved

    //who reviews
    public Guid? ReviewBy { get; set; }
    public ApplicationUser? ReviewByUser { get; set; }
    
    public string? ReviewComments { get; set; } 
    
    public bool IsPending => Status == nameof(ApplicationStatus.Pending); //true if pending

    public bool IsApproved => Status == nameof(ApplicationStatus.Approved); //true if approved

    public bool IsRejected => Status == nameof(ApplicationStatus.Rejected); //true if rejected

}