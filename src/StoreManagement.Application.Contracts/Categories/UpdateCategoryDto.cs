using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Categories;

public class UpdateCategoryDto
{
    [Required]
    [MaxLength(CategoryConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(CategoryConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public CategorySizeType SizeType { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }
}