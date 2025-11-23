using Ecommerce.Core.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.IRepository
{
    /// <summary>
    /// Category Repository Interface for CRUD operations and pagination
    /// </summary>
    public interface ICategoryRepository
    {
        Task<Category> CreateAsync(Category category);
        Task<Category> DeleteAsync(Category categoryId);
        Task<Category> UpdateAsync(Category category);
        Task<(List<Category>, int totalCount)> GetPaginatedCategoryAsync(Guid userId, int pageNumber, int pageSize);
        Task<Category> GetByIdAsync(Guid id);    
    }
}
