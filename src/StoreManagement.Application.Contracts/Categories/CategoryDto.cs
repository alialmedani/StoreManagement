using System;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Categories;

public class CategoryDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public LookupDto SizeType { get; set; } = new();

    public string? ImageUrl { get; set; }

    public string? ImageFullUrl => !string.IsNullOrEmpty(ImageUrl) 
        ? $"/api/app/file/by-file-name/{ImageUrl}" 
        : null;

    public bool IsActive { get; set; }
}