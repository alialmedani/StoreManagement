using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoreManagement.Media;

public class UploadMediaDto
{
    [Required]
    [StringLength(MediaConsts.MaxEntityIdLength)]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    public MediaEntityType EntityType { get; set; }

    [Required]
    [StringLength(MediaConsts.MaxFilePlacementLength)]
    public string FilePlacement { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = default!;
}