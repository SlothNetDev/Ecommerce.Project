using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.CatalogDto.Category
{
    public record CategoryResponseDto
    {
        public Guid? Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
    }
}
