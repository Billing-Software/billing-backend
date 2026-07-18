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
                Type = dto.Type,
                ParentId = dto.ParentId
            };

            var added = await _categoryRepository.AddAsync(category);
            return MapToDto(added);
        }

        public async Task<CategoryDto?> UpdateAsync(int businessId, int id, CategoryDto dto)
        {
            var existing = await _categoryRepository.GetByIdAsync(businessId, id);
            if (existing == null)
            {
                return null;
            }

            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId.Value == id)
                {
                    throw new InvalidOperationException("A category cannot be its own parent.");
                }

                // Check for cyclic dependency
                var allCategories = (await _categoryRepository.GetByBusinessIdAsync(businessId)).ToList();
                if (IsDescendant(id, dto.ParentId.Value, allCategories))
                {
                    throw new InvalidOperationException("A category cannot have a descendant as its parent.");
                }
            }

            existing.Name = dto.Name;
            existing.Type = dto.Type;
            existing.ParentId = dto.ParentId;

            var updated = await _categoryRepository.UpdateAsync(existing);
            return updated == null ? null : MapToDto(updated);
        }

        private bool IsDescendant(int parentId, int potentialDescendantId, List<Category> allCategories)
        {
            var children = allCategories.Where(c => c.ParentId == parentId).Select(c => c.Id).ToList();
            if (children.Contains(potentialDescendantId))
            {
                return true;
            }

            foreach (var childId in children)
            {
                if (IsDescendant(childId, potentialDescendantId, allCategories))
                {
                    return true;
                }
            }

            return false;
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
                ParentId = c.ParentId,
                CreatedAt = c.CreatedAt
            };
        }
    }
}
