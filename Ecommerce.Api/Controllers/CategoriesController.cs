using Ecommerce.Infrastructure.Data;
using Ecommerce.Shared.CatalogDto.Category;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Core.Domain.Entities.Catalog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ecommerce.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ApplicationDbContext dbContext) : ControllerBase
    {
        // GET: api/<CategoryController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = await dbContext.CategoriesDb
                .Select(c => new CategoryResponseDto
                {
                    Id = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(response);
        }

        // GET api/<CategoryController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var category = await dbContext.CategoriesDb.FindAsync(id);
            if (category == null) return NotFound();

            var response = new CategoryResponseDto
            {
                Id = category.CategoryId,
                Name = category.Name,
                Description = category.Description
            };
            return Ok(response);
        }

        // POST api/<CategoryController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CategoryRequestDto request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            dbContext.CategoriesDb.Add(category);
            await dbContext.SaveChangesAsync();

            var response = new CategoryResponseDto
            {
                Id = category.CategoryId,
                Name = category.Name,
                Description = category.Description
            };

            return CreatedAtAction(nameof(Get), new { id = category.CategoryId }, response);
        }

        // PUT api/<CategoryController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] CategoryUpdateRequestDto request)
        {
            if (id != request.Id) return BadRequest("ID mismatch");

            var category = await dbContext.CategoriesDb.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = request.Name;
            category.Description = request.Description;

            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/<CategoryController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var category = await dbContext.CategoriesDb.FindAsync(id);
            if (category == null) return NotFound();

            dbContext.CategoriesDb.Remove(category);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
