using System;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Media;

public class MediaDto : FullAuditedEntityDto<Guid>
{
    public string EntityId { get; set; } = string.Empty;

    public MediaEntityType EntityType { get; set; }

    public string FilePlacement { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }
}