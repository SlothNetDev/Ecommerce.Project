using Ecommerce.Core.Domain.Validations.CatalogValidation.CategoryValidation;
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
            #endregion

            return services;
        }
    }
}
