using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<CategoryDto>> GetByBusinessIdAsync(int businessId)
        {
            var list = await _categoryRepository.GetByBusinessIdAsync(businessId);
            return list.Select(MapToDto);
        }

        public async Task<CategoryDto> AddAsync(int businessId, CategoryDto dto)
        {
            var category = new Category
            {
                BusinessId = businessId,
                Name = dto.Name,
                Type = dto.Type
            };

            var added = await _categoryRepository.AddAsync(category);
            return MapToDto(added);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _categoryRepository.DeleteAsync(businessId, id);
        }

        private CategoryDto MapToDto(Category c)
        {
            return new CategoryDto
            {
                Id = c.Id,
                BusinessId = c.BusinessId,
                Name = c.Name,
                Type = c.Type,
                CreatedAt = c.CreatedAt
            };
        }
    }
}
