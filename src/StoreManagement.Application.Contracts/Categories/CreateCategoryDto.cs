using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Categories;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(CategoryConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(CategoryConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public CategorySizeType SizeType { get; set; } = CategorySizeType.None;

    public bool IsActive { get; set; } = true;
}