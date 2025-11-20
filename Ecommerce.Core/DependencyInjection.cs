using Ecommerce.Core.Domain.Validations.CatalogValidation.CategoryValidation;
using Ecommerce.Core.Domain.Validations.CatalogValidation.ProductValidation;
using Ecommerce.Core.Domain.Validations.OrderValidation.OrderItem;
using Ecommerce.Core.Domain.Validations.OrderValidation.OrderItemValidation;
using Ecommerce.Core.Domain.Validations.OrderValidation.OrdersValidation;
using Ecommerce.Core.Domain.Validations.Reviews;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Core
{
    /// <summary>
    /// Creates extension methods for registering services in the Infrastructure layer.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            #region
            //Category Validation
            services.AddValidatorsFromAssemblyContaining<CategoryRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<CategoryUpdateValidation>();

            services.AddValidatorsFromAssemblyContaining<ProductRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<ProductUpdateValidation>();
            #endregion

            #region
            //Order Validation
            services.AddValidatorsFromAssemblyContaining<StatusUpdateBySellerValidation>();

            services.AddValidatorsFromAssemblyContaining<OrderRequestValidation>();
            services.AddValidatorsFromAssemblyContaining<OrderUpdateValidation>();
            #endregion

            #region
            //Reviews Validation
            services.AddValidatorsFromAssemblyContaining<ReviewRequestValidation>();
            #endregion
            return services;
        }
    }
}
