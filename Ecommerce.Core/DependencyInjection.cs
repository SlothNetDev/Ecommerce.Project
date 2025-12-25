using Ecommerce.Core.Domain.Validations.AuthenticationValidation;
using Ecommerce.Core.Domain.Validations.CatalogValidation.CategoryValidation;
using Ecommerce.Core.Domain.Validations.CatalogValidation.ProductValidation;
using Ecommerce.Core.Domain.Validations.OrderValidation.OrderItemValidation;
using Ecommerce.Core.Domain.Validations.OrderValidation.OrdersValidation;
using Ecommerce.Core.Domain.Validations.RefreshTokenValidation;
using Ecommerce.Core.Domain.Validations.ReviewsValidation;
using Ecommerce.Core.Domain.Validations.SellerApplicationValidation;
using Ecommerce.Shared.SellerApplication;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;


namespace Ecommerce.Core
{
    /// <summary>
    /// Creates extension methods for registering services in the Infrastructure layer.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            #region Category Validation
            services.AddValidatorsFromAssemblyContaining<CategoryRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<CategoryUpdateValidation>();

            services.AddValidatorsFromAssemblyContaining<ProductRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<ProductUpdateValidation>();
            #endregion

            #region Order Validation
            services.AddValidatorsFromAssemblyContaining<StatusUpdateBySellerValidation>(); 

            services.AddValidatorsFromAssemblyContaining<OrderRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<OrderUpdateValidation>();
            #endregion

            #region Reviews Validation
            services.AddValidatorsFromAssemblyContaining<ReviewRequestValidation>();
            #endregion
            
            #region Refresh token validation
            services.AddValidatorsFromAssemblyContaining<RefreshTokenRequestValidation>();
            #endregion
            
            #region  Authentication

            services.AddValidatorsFromAssemblyContaining<LoginRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<RegistrationRequestValidation>();
            #endregion

            #region Application Seller Validation
            services.AddValidatorsFromAssemblyContaining<SubmitApplicationRequestValidation>();
            #endregion
            return services;
        }
    }
}
