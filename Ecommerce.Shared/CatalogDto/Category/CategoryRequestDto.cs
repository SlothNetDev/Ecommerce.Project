using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.CatalogDto.Category
{
    public record CategoryRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
